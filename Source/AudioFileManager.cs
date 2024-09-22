using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Wasteland_Waves.Source;

public class AudioFileManager : SingletonMonoBehaviour<AudioFileManager>
{
    public string gameDirectory;
    public string modDirectory;
    public string dataDirectory;
    private readonly Dictionary<string, string[]> _stationMap = new();

    public override void Awake()
    {
        base.Awake();
        IsPersistant = true;

        gameDirectory = Directory.GetCurrentDirectory();
        modDirectory = $@"{gameDirectory}\Mods\Wasteland-Waves";
        dataDirectory = $@"{modDirectory}\Data";

        InitStations();
    }


    private void InitStations()
    {
        //get the names of all stations.
        var stations = Directory.GetDirectories(dataDirectory).Select(Path.GetFileName);

        //get the files names in every station.
        foreach (var station in stations)
        {
            Debug.LogWarning($"Found Station: {station}");
            var files = Directory.GetFiles($"{dataDirectory}\\{station}", "*.mp3").Select(Path.GetFileName).ToArray();

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

    //IEnumerator LoadAudioClip()
    //{
    //    var filepath = $"file://{_audioFilesDirectory}\\{_audioFiles[0]}";
    //    Debug.LogWarning($"Loading {filepath}");
//
    //    var dh = new DownloadHandlerAudioClip(filepath, AudioType.MPEG);
    //    dh.compressed = true;
//
    //    using var wr = new UnityWebRequest(filepath, "Get", dh, null);
    //    yield return wr.SendWebRequest();
    //    if (wr.responseCode == 200)
    //    {
    //        Debug.LogWarning($"LOADED SONG");
    //    }
    //    else
    //    {
    //        Debug.LogError($"SONG NOT LOADED"); 
    //    }
    //}
}