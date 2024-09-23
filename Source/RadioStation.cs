using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Wasteland_Waves.Source.NetPackages;
using Random = System.Random;

namespace Wasteland_Waves.Source;

public class RadioStation : MonoBehaviour
{
    private readonly Queue<string> _songQueue = new();
    private List<string> _stationSongs = new();
    
    private string _currentSongName = string.Empty;
    private AudioClip _currentSongClip = null;
    private AudioClip _nextSongClip = null;
    private bool _isValidated = false;
    private bool _isCurrentLoading = false;
    private bool _isNextLoading = false;

    private float _time;
    public bool isPlaying = true;

    private float _elapsedUpdateTime = 0.0f;
    private readonly List<ClientInfo> _clients = new();

    private void Update()
    {
        if (!_isValidated)
            return;
        
        var isServerOrSinglePlayer =
            SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ||
            SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (!isServerOrSinglePlayer)
            return;
        
        UpdateRadio();
        
        if(Input.GetKeyDown(KeyCode.Alpha9))
            PrintCurrentState();
        
        _elapsedUpdateTime += Time.deltaTime;
        if (_elapsedUpdateTime >= 1f)
        {
            _elapsedUpdateTime = 0;
            UpdateClients();
        }
        
        //We only get to this point if we are in single player, or are the server.
        CheckAndLoadSongs();
        
        //play next song if one is not playing
        if (isPlaying) return;
        ReadyNextSong();
    }
    
    public void Init(string stationName)
    {
        name = stationName;
        
        //get a list of songs to validate
        _stationSongs = SingletonMonoBehaviour<SongsFileManager>.Instance.GetStationSongs(name);
        //validate the songs
        StartCoroutine(ValidateSongs());
        
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if (!isServer) return;
    }

    public void CleanUp()
    {
        _clients.Clear();
    }
    public void PlayerSpawnedInWorld(ClientInfo clientInfo)
    {
        _clients.Add(clientInfo);
    }

    public void PlayerDisconnected(ClientInfo clientInfo)
    {
        _clients.Remove(clientInfo);
    }

    public void UpdateRadio()
    {
        this._time += Time.deltaTime;

        if (_currentSongClip is null) return;
        
        if (_time > _currentSongClip.length)
        {
            isPlaying = false;
        }
    }
    
    public float GetStationTime() => _time;

    public string GetCurrentSongName()=> _currentSongName;

    public AudioClip GetCurrentSongClip() => _currentSongClip;
    
    public void Play() => isPlaying = true;

    public void ResetAndPlay()
    {
        isPlaying = true;
        _time = 0;
    }

    public void Stop()
    {
        isPlaying = false;
        _time = 0;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void UpdateStationFromServer(string newCurrentSong, string newNextSong, float time)
    {
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if (isServer) return;

        _time = time;
            
        //only update if we have a new song.
        if (_currentSongName == newCurrentSong) return;
        
        Debug.LogWarning($"Station: {name} - Updating from server");
        
        //unload the old song clip
        var songClipToUnload = _currentSongClip;
        StartCoroutine(UnloadAudioClip(songClipToUnload));
        
        //set the current song and load
        _currentSongName = newCurrentSong;
        
        // If the next song clip is loaded and matches the new current song, 
        // set the current clip to the next one (no need to reload).
        if (_nextSongClip != null && _nextSongClip.name == newCurrentSong && _nextSongClip.loadState == AudioDataLoadState.Loaded)
        {
            _currentSongClip = _nextSongClip;
            _nextSongClip = null;
        }
        // Otherwise, load the new song and set it as the current song.
        else
        {
            StartCoroutine(LoadAudioClip(newCurrentSong, result =>
            {
                _currentSongClip = result;
            }));
        }
        
        //load the next song regardless.
        StartCoroutine(LoadAudioClip(newNextSong, result =>
        {
            _nextSongClip = result;
        })); 
    }

    private void ReadyNextSong()
    {
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        var isSinglePlayer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (!isServer && !isSinglePlayer) return;
        
        Debug.Log($"Station: {name} - Current Song Finished, Starting New Song");
            
        //shuffle if we are out of songs
        if(_songQueue.Count == 0)
            ShuffleQueue();
        
        //set the current song
        _currentSongName = _songQueue.Dequeue();
        
        //set the current song clip to be the next song clip
        var songToUnload = _currentSongClip;
        _currentSongClip = _nextSongClip;
        _nextSongClip = null;
        
        //unload the unused song
        StartCoroutine(UnloadAudioClip(songToUnload));

        ResetAndPlay();
        
        //update clients of the change
        UpdateClients();
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
        if (_currentSongClip == null && !_isCurrentLoading)
        {
            _isCurrentLoading = true;
            StartCoroutine(LoadAudioClip(_currentSongName, result =>
            {
                _currentSongClip = result;
                _isCurrentLoading = false;
            }));
        }

        if (_songQueue.Count == 0) return;
        
        if (_nextSongClip == null && !_isNextLoading)
        {
            _isNextLoading = true;
            StartCoroutine(LoadAudioClip(_songQueue.Peek(), result =>
            {
                _nextSongClip = result;
                _isNextLoading = false;
            }));
        }
    }

    private void UpdateClients()
    {
        //If we are the server, update the other players
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if(!isServer) return;

        if (_songQueue.Peek() == null)
            return;

        var package = NetPackageManager.GetPackage<NetPackageUpdateRadioStation>().Setup(
            name,
            _currentSongName, 
            _songQueue.Peek(),
            _time
        );

        foreach (var clientInfo in _clients)
        {
            clientInfo.SendPackage(package);
        }
    }
    
    private IEnumerator ValidateSongs()
    { 
        var validatedSongs = new List<string>();
        foreach (var songName in _stationSongs)
        {
            Debug.Log($"Station: {name} - Validating Song: {songName}");
            var audioFileManager = SingletonMonoBehaviour<SongsFileManager>.Instance;
            var filePath = $@"{audioFileManager.GetDataDirectory()}\{name}\{songName}";
            
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
            
            yield return null; // Yield after each song validation to avoid blocking
        }
        
        _stationSongs = validatedSongs;
        
        //shuffle
        ShuffleQueue();
        //set the current song
        _currentSongName = _songQueue.Dequeue();

        yield return LoadAudioClip(_currentSongName, result =>
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
            Resources.UnloadUnusedAssets();
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
            dh.audioClip.name = songName;
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
        message.AppendLine($"Current Song: {_currentSongName}");
        message.AppendLine($"Current Song Clip Name: {_currentSongClip?.name}");
        message.AppendLine($"Next Song Clip Name: {_nextSongClip?.name}");
        message.AppendLine($"Current Song Load State: {_currentSongClip?.loadState}");
        message.AppendLine($"Next Song Load State: {_nextSongClip?.loadState}");
        message.AppendLine($"Time: {_time}");
        
        Debug.LogWarning(message.ToString());
    }
}