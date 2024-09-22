using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UniLinq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;
using Random = System.Random;

namespace Wasteland_Waves.Source;

public class RadioStation : MonoBehaviour
{
    private AudioSource _internalAudioSource;
    
    private readonly Queue<string> _songQueue = new();
    private List<string> _stationSongs = new();
    
    private string _currentSong = string.Empty;
    private AudioClip _currentSongClip = null;
    private AudioClip _nextSongClip = null;
    private bool _isValidated = false;
    private bool _isLoading = false;

    private void Update()
    {
        if (!_isValidated)
            return;
        
        var isServerOrSinglePlayer =
            SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ||
            SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (!isServerOrSinglePlayer)
            return;
        
        if(Input.GetKeyDown(KeyCode.Alpha9))
            PrintCurrentState();
        
        //We only get to this point if we are in single player, or are the server.
        CheckAndLoadSongs();

        //play next song if one is not playing
        if (_internalAudioSource.isPlaying) return;
        ReadyNextSong();
    }

    public void Init(string stationName)
    {
        name = stationName;
        
        _internalAudioSource = gameObject.AddComponent<AudioSource>();
        _internalAudioSource.volume = 0f;
        _internalAudioSource.playOnAwake = false;
        _internalAudioSource.loop = false;

        //get a list of songs to validate
        _stationSongs = SingletonMonoBehaviour<SongsFileManager>.Instance.GetStationSongs(name);
        //validate the songs
        StartCoroutine(ValidateSongs());
        
    }

    public float GetStationTime() => _internalAudioSource.time;

    public string GetCurrentSongName()=> _currentSong;
    
    public AudioClip GetCurrentSongClip()=> _currentSongClip;

    public string GetNextSongName() => _songQueue.Count == 0 ? string.Empty : _songQueue.Peek();

    private void ReadyNextSong()
    {
        Debug.Log($"Station: {name} - Current Song Finished, Starting New Song");
        //shuffle if we are out of songs
        if(_songQueue.Count == 0)
            ShuffleQueue();
        
        //set the current song
        _currentSong = _songQueue.Dequeue();
        
        //set the current song clip to be the next song clip
        var songToUnload = _currentSongClip;
        _currentSongClip = _nextSongClip;
        _nextSongClip = null;
        
        //unload the unused song
        StartCoroutine(UnloadAudioClip(songToUnload));
        
        _internalAudioSource.clip = _currentSongClip;
        _internalAudioSource.Play();
    }
    
    private void ShuffleQueue()
    {
        var numberOfSongs = _stationSongs.Count;
        var songIndexList = new List<string>();
        _songQueue.Clear();

        for (var i = 0; i < numberOfSongs; i++) songIndexList.Add(_stationSongs[i]);

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

    private void CheckAndLoadSongs()
    {
        if (_currentSongClip == null && !_isLoading)
        {
            _isLoading = true;
            StartCoroutine(LoadAudioClip(_currentSong, result =>
            {
                _currentSongClip = result;
                _isLoading = false;
            }));
        }

        if (_nextSongClip == null && !_isLoading)
        {
            _isLoading = true;
            StartCoroutine(LoadAudioClip(_songQueue.Peek(), result =>
            {
                _nextSongClip = result;
                _isLoading = false;
            }));
        }
    }
    
    private IEnumerator ValidateSongs()
    { 
        var validatedSongs = new List<string>();
        foreach (var songName in _stationSongs)
        {
            Debug.Log($"Station: {name} - Validating Song: {songName}");
            var audioFileManager = SingletonMonoBehaviour<SongsFileManager>.Instance;
            var filePath = $"{audioFileManager.GetDataDirectory()}\\{name}\\{songName}";
            
            var dh = new DownloadHandlerAudioClip($"file://{filePath}", AudioType.MPEG);
            dh.compressed = true;
            
            using var wr = new UnityWebRequest($"file://{filePath}", "GET", dh, null);
            yield return wr.SendWebRequest();
            
            if (wr.responseCode == 200)
            {
                validatedSongs.Add(songName);
            }
            else
            {
                Debug.LogError($"Station: {name} - Unable to Load Song: {songName}; Removing from station list");
            }
        }
        
        _stationSongs = validatedSongs;
        
        //shuffle
        ShuffleQueue();
        //set the current song
        _currentSong = _songQueue.Dequeue();

        yield return LoadAudioClip(_currentSong, result =>
        {
            _currentSongClip = result;
        });

        yield return LoadAudioClip(_songQueue.Peek(), result =>
        {
            _nextSongClip = result;
        });
        
        _isValidated = true;
    }

    private IEnumerator UnloadAudioClip(AudioClip audioClip)
    {
        if (audioClip != null && audioClip.loadState == AudioDataLoadState.Loaded)
        {
            Debug.Log($"Station: {name} - Unloading Audio Clip Data {audioClip.name}");
            audioClip.UnloadAudioData();
        }

        yield return null;
    }

    private IEnumerator LoadAudioClip(string songName, System.Action<AudioClip> callback)
    {
        Debug.Log($"Station: {name} - Loading AudioClip: {songName}");
        var audioFileManager = SingletonMonoBehaviour<SongsFileManager>.Instance;

        var filePath = $"{audioFileManager.GetDataDirectory()}\\{name}\\{songName}";

        var dh = new DownloadHandlerAudioClip($"file://{filePath}", AudioType.MPEG);
        dh.compressed = true;

        using var wr = new UnityWebRequest($"file://{filePath}", "GET", dh, null);
        yield return wr.SendWebRequest();

        if (wr.responseCode == 200)
        {
            dh.audioClip.LoadAudioData();
            callback?.Invoke(dh.audioClip);
        }
        else
        {
            Debug.LogError($"Station: {name} - Unable to Load AudioClip: {songName}");
        }
        
    }

    private void PrintCurrentState()
    {
        var message = new StringBuilder();
        message.AppendLine($"Station: {name}");
        message.AppendLine($"Current Song: {_currentSong}");
        message.AppendLine($"NextSong: {_songQueue.Peek()}");
        message.AppendLine($"Current Song Clip Name: {_currentSongClip?.name}");
        message.AppendLine($"Next Song Clip Name: {_nextSongClip?.name}");
        message.AppendLine($"Current Song Load State: {_currentSongClip?.loadState}");
        message.AppendLine($"Next Song Load State: {_nextSongClip?.loadState}");
        message.AppendLine($"Time: {_internalAudioSource.time}");
        
        Debug.LogWarning(message.ToString());
    }
}