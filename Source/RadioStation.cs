using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace Wasteland_Waves.Source;

public class RadioStation : MonoBehaviour
{
    private readonly Queue<string> _songQueue = new();
    private readonly Dictionary<string, AudioClip> _stationSongs = new();
    private string _currentSong = string.Empty;
    private AudioSource _internalAudioSource;

    private bool _isFinishedLoading;
    private int _loadedSongs;

    private void Update()
    {
        var isServerOrSinglePlayer =
            SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ||
            SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (!isServerOrSinglePlayer)
            return;

        if (!_isFinishedLoading)
            return;

        //We only get to this point if we are in single player, or are the server. And we finished loading our songs.

        //play next song if one is not playing
        if (_internalAudioSource.isPlaying && _currentSong != string.Empty) return;
        SetCurrentSong();
        PlayCurrentSong();
    }

    public void Init(string stationName)
    {
        name = stationName;

        var songNames = SingletonMonoBehaviour<AudioFileManager>.Instance.GetStationSongs(name);
        foreach (var songName in songNames) StartCoroutine(LoadAudioClip(songName));
        _internalAudioSource = gameObject.AddComponent<AudioSource>();
        _internalAudioSource.volume = 0f;
        _internalAudioSource.playOnAwake = false;
    }

    public float GetStationTime()
    {
        return _internalAudioSource.time;
    }

    public string GetCurrentSongName()
    {
        return _currentSong;
    }

    public AudioClip GetSongClip(string songName)
    {
        return _stationSongs[songName];
    }

    public AudioClip GetCurrentSongClip()
    {
        return _stationSongs[_currentSong];
    }

    private void SetCurrentSong()
    {
        if (_songQueue.Count == 0)
            ShuffleQueue();

        _currentSong = _songQueue.Dequeue();
    }

    private void PlayCurrentSong()
    {
        var songToPlay = _stationSongs[_currentSong];

        _internalAudioSource.clip = songToPlay;
        _internalAudioSource.Play();
    }

    private void ShuffleQueue()
    {
        var numberOfSongs = _stationSongs.Count;
        var songIndexList = new List<string>();
        _songQueue.Clear();

        for (var i = 0; i < numberOfSongs; i++) songIndexList.Add(_stationSongs.Keys.ToList()[i]);

        var random = new Random();
        var n = numberOfSongs;

        while (n > 1)
        {
            n--;
            var k = random.Next(n + 1);
            (songIndexList[k], songIndexList[n]) = (songIndexList[n], songIndexList[k]);
        }

        foreach (var songIndex in songIndexList) _songQueue.Enqueue(songIndex);
    }

    private IEnumerator LoadAudioClip(string songName)
    {
        Debug.Log($"Loading Song: {songName}");
        var audioFileManager = SingletonMonoBehaviour<AudioFileManager>.Instance;

        var filePath = $"{audioFileManager.dataDirectory}\\{name}\\{songName}";

        var dh = new DownloadHandlerAudioClip($"file://{filePath}", AudioType.MPEG);
        dh.compressed = true;

        using var wr = new UnityWebRequest($"file://{filePath}", "GET", dh, null);
        yield return wr.SendWebRequest();

        if (wr.responseCode == 200)
        {
            Debug.Log($"Loaded AudioClip: {songName}");
            dh.audioClip.LoadAudioData();
            _stationSongs.Add(songName, dh.audioClip);
        }
        else
        {
            Debug.LogError($"Unable to Load AudioClip: {songName}");
        }

        _loadedSongs += 1;
        var songCount = SingletonMonoBehaviour<AudioFileManager>.Instance.GetStationSongs(name).Count;

        if (_loadedSongs < songCount) yield break;

        _isFinishedLoading = true;
        Debug.LogWarning($"Station {name} finished loading all songs");
    }
}