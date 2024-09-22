using System.Collections.Generic;
using UnityEngine;

namespace Wasteland_Waves.Source;

public class RadioManager : SingletonMonoBehaviour<RadioManager>
{
    private readonly Dictionary<string, RadioStation> _radioStations = new();

    public override void Awake()
    {
        base.Awake();
        IsPersistant = true;

        CreateAndInitRadioStations();
    }
    
    public IEnumerable<string> GetStationNames()=> _radioStations.Keys;

    public RadioStation GetStation(string stationName)=> _radioStations[stationName];

    private void CreateAndInitRadioStations()
    {
        var stationNames = SingletonMonoBehaviour<AudioFileManager>.Instance.GetStations();

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