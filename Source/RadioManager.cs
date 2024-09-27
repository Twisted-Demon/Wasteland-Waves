using System.Collections.Generic;
using UnityEngine;

namespace Wasteland_Waves.Source;

public class RadioManager : SingletonMonoBehaviour<RadioManager>
{
    private readonly Dictionary<string, RadioStation> _radioStations = new();
    
    public override void singletonAwake()
    {
        base.singletonAwake();
        IsPersistant = true;

        CreateAndInitRadioStations();
    }

    // Retrieve the station names as an IEnumerable
    public IEnumerable<string> GetStationNames() => _radioStations.Keys;

    // Safely retrieve a station by name with exception handling
    public RadioStation GetStation(string stationName)
    {
        if (_radioStations.TryGetValue(stationName, out var station))
        {
            return station;
        }

        Log.Error("Station {0} not found.", stationName);
        return null;
    }

    // Updates the station from server data
    public void UpdateRadioStationFromServer(string stationName, string newCurrentSong, string newNextSong, float time)
    {
        if (_radioStations.TryGetValue(stationName, out var station))
        {
            station.UpdateStationFromServer(newCurrentSong, newNextSong, time);
        }
        else
        {
            Log.Error("Failed to update, Station {0} not found.", stationName);
        }
    }

    // Cleans up all stations
    public void CleanUp()
    {
        if (_radioStations.Count == 0) return;

        foreach (var station in _radioStations.Values)
        {
            station?.CleanUp();
        }
    }

    // Called when a player disconnects; server-side only
    public void PlayerDisconnected(ClientInfo clientInfo)
    {
        if (!IsServer()) return;

        if (clientInfo == null)
        {
            Log.Warning("ClientInfo is null during PlayerDisconnected.");
            return;
        }

        Log.Out("Client {0} disconnected.", clientInfo.playerName);

        foreach (var station in _radioStations.Values)
        {
            station?.PlayerDisconnected(clientInfo);
        }
    }

    // Called when a player spawns into the world; server-side only
    public void PlayerSpawnedInWorld(ClientInfo clientInfo)
    {
        if (!IsServer()) return;

        if (clientInfo == null)
        {
            Log.Warning("ClientInfo is null during PlayerSpawnedInWorld.");
            if (Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();
            return;
        }

        Log.Out("Client {0} spawned in world. Sending radio data.", clientInfo.playerName);

        foreach (var station in _radioStations.Values)
        {
            station?.PlayerSpawnedInWorld(clientInfo);
        }
    }

    // Helper method to check if we are running on the server
    private bool IsServer()
    {
        return SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
    }

    // Initialize the radio stations
    private void CreateAndInitRadioStations()
    {
        var stationNames = SingletonMonoBehaviour<SongsFileManager>.Instance.GetStations();

        foreach (var stationName in stationNames)
        {
            var stationGameObject = new GameObject(stationName);

            DontDestroyOnLoad(stationGameObject);
            var radioStation = stationGameObject.AddComponent<RadioStation>();
            _radioStations.Add(stationName, radioStation);
            radioStation.Init(stationName);
        }
    }
}
