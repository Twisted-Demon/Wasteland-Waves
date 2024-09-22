using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Wasteland_Waves.Source;

public class SongsFileManager : SingletonMonoBehaviour<SongsFileManager>
{
    private string _gameDirectory;
    private string _modDirectory;
    private string _dataDirectory;
    private readonly Dictionary<string, string[]> _stationMap = new();

    public override void Awake()
    {
        base.Awake();
        IsPersistant = true;

        _gameDirectory = Directory.GetCurrentDirectory();
        _modDirectory = $@"{_gameDirectory}\Mods\Wasteland-Waves";
        _dataDirectory = $@"{_modDirectory}\Data";

        InitStations();
    }


    private void InitStations()
    {
        //get the names of all stations.
        var stations = Directory.GetDirectories(_dataDirectory).Select(Path.GetFileName);

        //get the files names in every station.
        foreach (var station in stations)
        {
            Debug.LogWarning($"Found Station: {station}");
            var files = Directory.GetFiles($"{_dataDirectory}\\{station}", "*.mp3").Select(Path.GetFileName).ToArray();

            if (files.Length == 0) continue;

            _stationMap.Add(station, files);

            foreach (var file in files)
                Debug.LogWarning($"Found File: {file}");
        }
    }

    public List<string> GetStations()
    {
        return _stationMap.Keys.ToList();
    }

    public List<string> GetStationSongs(string stationName)
    {
        return _stationMap[stationName].ToList();
    }

    public string GetDataDirectory() => _dataDirectory;
}