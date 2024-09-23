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

        if (!_isLocalPlayerAttached)
            return;

        if (Input.GetKey(KeyCode.Alpha1))
            DecreaseVolume();

        if (Input.GetKey(KeyCode.Alpha2))
            IncreaseVolume();

        if (Input.GetKeyDown(KeyCode.Alpha4))
            NextStation();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            PreviousStation();

        CheckIfRadioIsPlaying();
        
    }

    public void Init(EntityVehicle entityVehicle)
    {
        _entityVehicle = entityVehicle;
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.volume = 0;
        _audioSource.playOnAwake = false;

        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        var isSinglePlayer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (isServer || isSinglePlayer)
            _currentStation = Enumerable.First(_radioManager.GetStationNames());
    }

    public void ChangeBiome(BiomeDefinition newBiome)
    {
    }

    public string GetCurrentStationPlaying()
    {
        return _currentStation;
    }

    private void PreviousStation()
    {
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        var isSinglePlayer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (isServer || isSinglePlayer)
        {
            //get previous station
            var stationList = _radioManager.GetStationNames().ToList();
            var currentStationIndex = stationList.IndexOf(_currentStation);

            var newStationIndex = currentStationIndex - 1;
            if (newStationIndex < 0)
                newStationIndex = stationList.Count - 1;

            var newStation = stationList[newStationIndex];
            var newSong = _radioManager.GetStation(newStation).GetCurrentSongName();
            var currentTime = _radioManager.GetStation(newStation).GetStationTime();

            //update radio
            UpdateRadio(newStation, newSong, currentTime);

            //if we are a server, send the new data to the other clients.
            if (!isServer) return;
            var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSendData>()
                .Setup(_entityVehicle.entityId, newStation, newSong, currentTime);

            cm.SendPackage(package, _allButAttachedToEntityId: _entityVehicle.entityId);
        }
        else
        {
            var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioReqPreviousStation>()
                .Setup(_entityVehicle.entityId);

            cm.SendToServer(package);
        }
    }

    private void NextStation()
    {
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        var isSinglePlayer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (isServer || isSinglePlayer)
        {
            //get the next station
            var stationList = _radioManager.GetStationNames().ToList();
            var currentStationIndex = stationList.IndexOf(_currentStation);

            var newStationIndex = currentStationIndex + 1;
            if (newStationIndex >= stationList.Count)
                newStationIndex = 0;

            var newStation = stationList[newStationIndex];
            var newSong = _radioManager.GetStation(newStation).GetCurrentSongName();
            var currentTime = _radioManager.GetStation(newStation).GetStationTime();

            //update radio
            UpdateRadio(newStation, newSong, currentTime);

            //if we are a server, send the new data to the other clients.
            if (!isServer) return;
            var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSendData>()
                .Setup(_entityVehicle.entityId, newStation, newSong, currentTime);

            cm.SendPackage(package, _allButAttachedToEntityId: _entityVehicle.entityId);
        }
        else
        {
            var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioReqNextStation>()
                .Setup(_entityVehicle.entityId);

            cm.SendToServer(package);
        }
    }

    private void IncreaseVolume()
    {
        _setVolume += Time.deltaTime * 0.5f;
        Math.Clamp(_setVolume, 0.0f, 1.0f);
    }

    private void DecreaseVolume()
    {
        _setVolume -= Time.deltaTime * 0.5f;
        Math.Clamp(_setVolume, 0.0f, 1.0f);
    }

    private void CalculateVolume()
    {
        _audioSource.volume = _setVolume;

        if (!_isLocalPlayerAttached)
            _audioSource.volume = 0;
    }

    private void CheckIfPlayerAttached()
    {
        var localPlayer = _entityVehicle.world.GetLocalPlayers()[0];
        var isLocalPlayerAttachedOld = _isLocalPlayerAttached;
        _isLocalPlayerAttached = _entityVehicle.IsAttached(localPlayer);

        SingletonMonoBehaviour<ModdedXuiManager>.Instance.showRadioInfo = _isLocalPlayerAttached;

        if (_isLocalPlayerAttached && !isLocalPlayerAttachedOld)
            PlayerEnteredVehicle();
        if (!_isLocalPlayerAttached && isLocalPlayerAttachedOld)
            PlayerExitedVehicle();
    }

    private void CheckIfRadioIsPlaying()
    {
        if (_audioSource.isPlaying) return;

        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        var isSinglePlayer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (isServer || isSinglePlayer)
        {
            UpdateRadio(
                _currentStation,
                _radioManager.GetStation(_currentStation).GetCurrentSongName(),
                _radioManager.GetStation(_currentStation).GetStationTime()
            );
        }
        else
        {
            var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioRequestData>()
                .Setup(_entityVehicle.entityId);

            cm.SendToServer(package);
        }
    }

    private void PlayerExitedVehicle()
    {
        _audioSource.Stop();
    }

    private void PlayerEnteredVehicle()
    {
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        var isSinglePlayer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;

        if (isServer || isSinglePlayer)
        {
            UpdateRadio(
                _currentStation,
                _radioManager.GetStation(_currentStation).GetCurrentSongName(),
                _radioManager.GetStation(_currentStation).GetStationTime(),
                true
            );
        }
        else
        {
            var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
            var package = NetPackageManager.GetPackage<NetPackageVehicleRadioRequestData>()
                .Setup(_entityVehicle.entityId);

            cm.SendToServer(package);
        }
    }

    public void UpdateRadio(string newStation, string newSong, float currentTime, bool enteredVehicle = false)
    {
        var isStationChanged = _currentStation != newStation;
        var isSongChanged = _currentSong != newSong;

        _currentStation = newStation;
        _currentSong = newSong;
        var currentStationComponent = _radioManager.GetStation(newStation);
        AudioClip currentSongClip = null;//currentStationComponent.GetSongClip(newSong);

        _audioSource.clip = currentSongClip;
        _audioSource.time = currentTime;

        if (!_audioSource.isPlaying && _isLocalPlayerAttached)
            _audioSource.Play();

        SingletonMonoBehaviour<ModdedXuiManager>.Instance.currentSongToShow = newSong;
        SingletonMonoBehaviour<ModdedXuiManager>.Instance.currentStationToShow = newStation;

        var localPlayer = _entityVehicle.world.GetLocalPlayers()[0];

        var showToolTp  = (isStationChanged || isSongChanged || enteredVehicle) && _isLocalPlayerAttached;
        var tooltipString = new StringBuilder();

        if (!showToolTp)
            return;
        
        if(isStationChanged || enteredVehicle)
            tooltipString.AppendLine($"WWMOD:Station: {newStation}");
        
        if(isSongChanged || enteredVehicle)
            tooltipString.Append($"WWMOD:Song: {Path.GetFileNameWithoutExtension(newSong)}");
        
        GameManager.ShowTooltip(localPlayer, tooltipString.ToString());
    }
}