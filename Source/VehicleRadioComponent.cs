using System;
using System.IO;
using System.Text;
using UniLinq;
using UnityEngine;
using Wasteland_Waves.Source.NetPackages;
using Enumerable = System.Linq.Enumerable;

namespace Wasteland_Waves.Source;
public class VehicleRadioComponent : MonoBehaviour
{
    private readonly RadioManager _radioManager = SingletonMonoBehaviour<RadioManager>.Instance;
    private AudioSource _audioSource;
    private EntityVehicle _entityVehicle;
    
    private string _currentSong = string.Empty;
    private string _currentStation = string.Empty;
    private bool _isLocalPlayerAttached;
    private float _setVolume = 0.65f;

    private void Update()
    {
        CheckIfPlayerAttached();
        CalculateVolume();

        if (!_isLocalPlayerAttached) return;

        HandleVolumeControl();
        HandleStationControl();
        CheckIfRadioIsPlaying();
    }

    public void Init(EntityVehicle entityVehicle)
    {
        _entityVehicle = entityVehicle;
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.volume = 0;
        _audioSource.playOnAwake = false;

        if (!IsServerOrSinglePlayer()) return;
        
        var stationNames = _radioManager.GetStationNames().ToList();
        if (stationNames.Any())
        {
            _currentStation = stationNames.First();
        }
    }

    public void ChangeBiome(BiomeDefinition newBiome) { }

    public string GetCurrentStationPlaying()
    {
        return _currentStation;
    }

    private void HandleStationControl()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4))
            ChangeStation(true);  // Next Station
        if (Input.GetKeyDown(KeyCode.Alpha3))
            ChangeStation(false);  // Previous Station
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
        _setVolume = Mathf.Clamp(_setVolume + delta, 0.0f, 1.0f);
    }

    private void CalculateVolume()
    {
        _audioSource.volume = _isLocalPlayerAttached ? _setVolume : 0;
    }

    private void CheckIfPlayerAttached()
    {
        var localPlayer = _entityVehicle.world.GetLocalPlayers()[0];
        var wasPlayerAttached = _isLocalPlayerAttached;
        _isLocalPlayerAttached = _entityVehicle.IsAttached(localPlayer);

        if (_isLocalPlayerAttached && !wasPlayerAttached)
            PlayerEnteredVehicle();
        else if (!_isLocalPlayerAttached && wasPlayerAttached)
            PlayerExitedVehicle();
    }

    private void CheckIfRadioIsPlaying()
    {
        if (!_audioSource.isPlaying && !string.IsNullOrEmpty(_currentStation))
        {
            UpdateRadio(_currentStation);
        }
    }

    private void PlayerExitedVehicle()
    {
        _audioSource.Stop();
    }

    private void PlayerEnteredVehicle()
    {
        if (IsServerOrSinglePlayer())
        {
            UpdateRadio(_currentStation, true);
        }
        else
        {
            RequestRadioDataFromServer();
        }
    }

    private void RequestRadioDataFromServer()
    {
        _currentStation = string.Empty;
        _currentSong = string.Empty;

        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioRequestData>()
            .Setup(_entityVehicle.entityId);

        cm.SendToServer(package);
    }

    public void UpdateRadio(string newStation, bool enteredVehicle = false)
    {
        var station = _radioManager.GetStation(newStation);

        var stationChanged = _currentStation != newStation;
        var songChanged = _currentSong != station.GetCurrentSongName();

        _currentStation = newStation;
        _currentSong = station.GetCurrentSongName();
        _audioSource.clip = station.GetCurrentSongClip();
        _audioSource.time = station.GetStationTime();

        if (!_audioSource.isPlaying && _isLocalPlayerAttached)
        {
            _audioSource.Play();
        }

        if (_isLocalPlayerAttached && (stationChanged || songChanged || enteredVehicle))
        {
            DisplayRadioInfo(newStation, songChanged, enteredVehicle);
        }

        if (IsServer())
        {
            SyncRadioDataWithClients(newStation, _currentSong, station.GetStationTime());
        }
    }

    private void DisplayRadioInfo(string station, bool songChanged, bool enteredVehicle)
    {
        var localPlayer = _entityVehicle.world.GetLocalPlayers()[0];
        var tooltip = new StringBuilder();

        if (enteredVehicle || songChanged)
        {
            tooltip.AppendLine($"WWMOD:Station: {station}");
            tooltip.Append($"WWMOD:Song: {Path.GetFileNameWithoutExtension(_currentSong)}");
        }

        GameManager.ShowTooltip(localPlayer, tooltip.ToString());
    }

    private void SyncRadioDataWithClients(string newStation, string currentSong, float stationTime)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSendData>()
            .Setup(_entityVehicle.entityId, newStation, currentSong, stationTime);

        cm.SendPackage(package, _allButAttachedToEntityId: _entityVehicle.entityId);
    }

    private void ChangeStation(bool isNext)
    {
        if (IsServerOrSinglePlayer())
        {
            var stationList = _radioManager.GetStationNames().ToList();
            var currentStationIndex = stationList.IndexOf(_currentStation);
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
                .Setup(_entityVehicle.entityId);
            cm.SendToServer(package);
        }
        else
        {
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioReqPreviousStation>()
                .Setup(_entityVehicle.entityId);
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