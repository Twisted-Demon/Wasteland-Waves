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
    
    
    public IEnumerable<string> GetStationNames()=> _radioStations.Keys;

    public RadioStation GetStation(string stationName)=> _radioStations[stationName];

    public void UpdateRadioStationFromServer(string stationName, string newCurrentSong, string newNextSong, float time)
    {
        _radioStations[stationName].UpdateStationFromServer(newCurrentSong, newNextSong, time);
    }

    public void CleanUp()
    {
        foreach(var station in _radioStations.Values)
            station.CleanUp();
    }
    
    public void PlayerDisconnected(ClientInfo clientInfo)
    {
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if (!isServer) return;
        
        if(clientInfo == null) return;
        
        Debug.LogWarning("Client Disconnected");

        foreach (var station in _radioStations.Values)
        {
            if (station == null) continue;
            station.PlayerDisconnected(clientInfo);
        }
    }
    public void PlayerSpawnedInWorld(ClientInfo clientInfo)
    {
        var isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if (!isServer) return;
        
        if(clientInfo == null) return;
        
        Debug.LogWarning("Client Spawned in World, sending data.");

        foreach (var station in _radioStations.Values)
        {
            if (station == null) continue;
            station.PlayerSpawnedInWorld(clientInfo);
        }
            
    }

    private void CreateAndInitRadioStations()
    {
        var stationNames = SingletonMonoBehaviour<SongsFileManager>.Instance.GetStations();

        foreach (var stationName in stationNames)
        {
            var stationGameObject = new GameObject
            {
                name = stationName
            };

            DontDestroyOnLoad(stationGameObject);
            var radioStation = stationGameObject.AddComponent<RadioStation>();
            _radioStations.Add(stationName, radioStation);
            radioStation.Init(stationName);
        }
    }
}