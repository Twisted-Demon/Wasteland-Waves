using System;
using System.IO;
using System.Text;
using UniLinq;
using UnityEngine;
using Wasteland_Waves.Source.NetPackages;

namespace Wasteland_Waves.Source;
public class VehicleRadioComponent : MonoBehaviour
{
    //Radio Components
    private readonly RadioManager _radioManager = SingletonMonoBehaviour<RadioManager>.Instance;
    private AudioSource _radioAudioSource;
    private AudioLowPassFilter _audioLowPassFilter;
    private EntityVehicle _attachedVehicle;
    
    //current Playback State
    private string _currentSongName = string.Empty;
    private string _currentRadioStationName = string.Empty;
    
    //player Interaction State
    private bool _isLocalPlayerAttachedToVehicle;
    private float _finalRadioVolume = 0.65f;
    private bool _isMuted = true;
    
    private void Update()
    {
        //here we check if we are in the vehicle
        CheckIfPlayerAttached();
        
        //calculate the volume
        CalculateVolume();
        
        //check if radio is playing, update if we are not
        CheckIfRadioIsPlayingAndUpdate();
        
        //handle volume control and station control if we are in the vehicle
        if (!_isLocalPlayerAttachedToVehicle) return;

        HandleVolumeControl();
        HandleStationControl();
        HandleMuteControl();
    }
    
    public void Init(EntityVehicle entityVehicle)
    {
        _attachedVehicle = entityVehicle;
        
        SetUpAudioSource();
        
        if (IsServerOrSinglePlayer())
        {
            var stationNames = _radioManager.GetStationNames().ToList();
            if (stationNames.Any())
            {
                _currentRadioStationName = stationNames.First();
            }

            UpdateRadio(_currentRadioStationName, displayRadioInfo: false);
        }
        else
        {
            RequestRadioDataFromServer();
        }
    }
    
    public string GetCurrentStationNamePlaying()
    {
        return _currentRadioStationName;
    }

    private void SetUpAudioSource()
    {
        _radioAudioSource = gameObject.AddComponent<AudioSource>();
        _radioAudioSource.volume = 0.0f;
        _radioAudioSource.playOnAwake = false;
        _radioAudioSource.minDistance = 3.0f;
        _radioAudioSource.maxDistance = 25.0f;
        _radioAudioSource.rolloffMode = AudioRolloffMode.Custom;
        _radioAudioSource.dopplerLevel = 0.0f;
        _radioAudioSource.spread = 0.0f;
        
        SetSpatialAudio(true);
        
        _audioLowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        
        //set up the spatial curve
        var customRolloffCurve = new AnimationCurve();
        customRolloffCurve.AddKey(_radioAudioSource.minDistance, 1.0f);
        customRolloffCurve.AddKey(_radioAudioSource.maxDistance * 0.35f, 0.5f);
        customRolloffCurve.AddKey(_radioAudioSource.maxDistance, 0.0f);

        _radioAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloffCurve);
    }
    
    private void HandleStationControl()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4))
            ChangeStation(true);  // Next Station
        if (Input.GetKeyDown(KeyCode.Alpha3))
            ChangeStation(false);  // Previous Station
    }

    private void HandleMuteControl()
    {
        if (!Input.GetKeyDown(KeyCode.Alpha5)) return;
        if(IsServer())
            SyncRadioMuteWithClients();
        else
            SendMuteRequestToServer(!_isMuted);
    }
    
    private void HandleVolumeControl()
    {
        if (Input.GetKey(KeyCode.Alpha1))
            AdjustVolume(-Time.deltaTime * 0.5f);
        if (Input.GetKey(KeyCode.Alpha2))
            AdjustVolume(Time.deltaTime * 0.5f);
    }
    
    private void AdjustVolume(float delta)
    {
        _finalRadioVolume = Mathf.Clamp(_finalRadioVolume + delta, 0.0f, 1.0f);
    }
    
    private void CalculateVolume()
    {
        //set the final volume
        _radioAudioSource.volume = _finalRadioVolume;
        
        //mute the car if needed
        _radioAudioSource.mute = _isMuted;
        
        //set the low pass filter for if we are in the car or not.
        _audioLowPassFilter.cutoffFrequency = !_isLocalPlayerAttachedToVehicle ? 1000f : 10000f;
        
        //set if we need to be using spatial audio
        //i.e if we are in vehicle or not
        SetSpatialAudio(!_isLocalPlayerAttachedToVehicle);
    }
    
    private void CheckIfPlayerAttached()
    {
        var localPlayer = _attachedVehicle.world.GetLocalPlayers()[0];
        var wasPlayerAttached = _isLocalPlayerAttachedToVehicle;
        _isLocalPlayerAttachedToVehicle = _attachedVehicle.IsAttached(localPlayer);

        if (_isLocalPlayerAttachedToVehicle && !wasPlayerAttached)
            PlayerEnteredVehicle();
        else if (!_isLocalPlayerAttachedToVehicle && wasPlayerAttached)
            PlayerExitedVehicle();
    }
    
    private void CheckIfRadioIsPlayingAndUpdate()
    {
        if (!_radioAudioSource.isPlaying && !string.IsNullOrEmpty(_currentRadioStationName))
        {
            UpdateRadio(_currentRadioStationName, displayRadioInfo: _isLocalPlayerAttachedToVehicle);
        }
    }
    
    private void PlayerExitedVehicle()
    {
        
    }
    
    private void PlayerEnteredVehicle()
    {
        DisplayRadioInfo(_currentRadioStationName, true, true);
    }
    
    private void RequestRadioDataFromServer()
    {
        _currentRadioStationName = string.Empty;
        _currentSongName = string.Empty;

        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioRequestData>()
            .Setup(_attachedVehicle.entityId);

        cm.SendToServer(package);
    } 
    
    public void UpdateRadio(string newStation, bool enteredVehicle = false, bool displayRadioInfo = true)
    {
        var station = _radioManager.GetStation(newStation);

        var stationChanged = _currentRadioStationName != newStation;
        var songChanged = _currentSongName != station.GetCurrentSongName();

        _currentRadioStationName = newStation;
        _currentSongName = station.GetCurrentSongName();
        
        SetAndPlayAudioClip(station.GetCurrentSongClip());
        
        _radioAudioSource.time = station.GetStationTime();

        if (_isLocalPlayerAttachedToVehicle && (stationChanged || songChanged || enteredVehicle) && displayRadioInfo)
        {
            DisplayRadioInfo(newStation, songChanged, enteredVehicle);
        }

        if (IsServer())
        {
            SyncRadioDataWithClients(newStation, _currentSongName, station.GetStationTime());
        }
    }

    public void SetMute(bool value) => _isMuted = value;

    public bool IsMuted() => _isMuted;

    private void SetSpatialAudio(bool enable)
    {
        _radioAudioSource.spatialBlend = enable? 1.0f : 0.0f;
    }
    
    private void SetAndPlayAudioClip(AudioClip clip)
    {
        _radioAudioSource.clip = clip;
        if(!_radioAudioSource.isPlaying) _radioAudioSource.Play();
    }
    
    private void DisplayRadioInfo(string station, bool songChanged, bool enteredVehicle)
    {
        var localPlayer = _attachedVehicle.world.GetLocalPlayers()[0];
        var tooltip = new StringBuilder();

        if (enteredVehicle || songChanged)
        {
            tooltip.AppendLine($"WWMOD:Station: {station}");
            tooltip.Append($"WWMOD:Song: {Path.GetFileNameWithoutExtension(_currentSongName)}");
        }

        GameManager.ShowTooltip(localPlayer, tooltip.ToString());
    }
    
    private void SyncRadioDataWithClients(string newStation, string currentSong, float stationTime)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSendData>()
            .Setup(_attachedVehicle.entityId, newStation, currentSong, stationTime, _isMuted);

        cm.SendPackage(package, _allButAttachedToEntityId: _attachedVehicle.entityId);
    }

    private void SyncRadioMuteWithClients()
    {
        _isMuted = !_isMuted;
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSetMute>()
            .Setup(_attachedVehicle.entityId, _isMuted);
        
        cm.SendPackage(package, _allButAttachedToEntityId: _attachedVehicle.entityId);
    }

    private void SendMuteRequestToServer(bool muted)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioRequestMute>()
            .Setup(_attachedVehicle.entityId, muted);
        cm.SendToServer(package);
    }
    
    private void ChangeStation(bool isNext)
    {
        if (IsServerOrSinglePlayer())
        {
            var stationList = _radioManager.GetStationNames().ToList();
            var currentStationIndex = stationList.IndexOf(_currentRadioStationName);
            var newIndex = isNext ? (currentStationIndex + 1) % stationList.Count :
                (currentStationIndex - 1 + stationList.Count) % stationList.Count;
            UpdateRadio(stationList[newIndex]);
        }
        else
        {
            SendStationChangeRequestToServer(isNext);
        }
    }
    
    private void SendStationChangeRequestToServer(bool isNext)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;

        if (isNext)
        {
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioReqNextStation>()
                .Setup(_attachedVehicle.entityId);
            cm.SendToServer(package);
        }
        else
        {
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioReqPreviousStation>()
                .Setup(_attachedVehicle.entityId);
            cm.SendToServer(package);
        }
    }
    
    private bool IsServer()
    {
        return SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
    }
    
    private bool IsServerOrSinglePlayer()
    {
        var connectionManager = SingletonMonoBehaviour<ConnectionManager>.Instance;
        return connectionManager.IsServer || connectionManager.IsSinglePlayer;
    }
}