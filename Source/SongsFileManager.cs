using System;
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

        InitializeDirectories();
        InitStations();
    }

    // Initialize all directories and handle any issues that may arise.
    private void InitializeDirectories()
    {
        try
        {
            _gameDirectory = Directory.GetCurrentDirectory();
            _modDirectory = Path.Combine(_gameDirectory, "Mods", "Wasteland-Waves");
            _dataDirectory = Path.Combine(_modDirectory, "Data");

            if (!Directory.Exists(_modDirectory))
            {
                Debug.LogError($"Mod directory not found: {_modDirectory}");
            }

            if (!Directory.Exists(_dataDirectory))
            {
                Debug.LogError($"Data directory not found: {_dataDirectory}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing directories: {ex.Message}");
        }
    }

    // Initialize stations by reading directories and MP3 files.
    private void InitStations()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Debug.LogError($"Data directory does not exist: {_dataDirectory}");
            return;
        }

        try
        {
            // Get the names of all station directories.
            var stations = Directory.GetDirectories(_dataDirectory).Select(Path.GetFileName);

            // Read files from each station directory and store them in the dictionary.
            foreach (var station in stations)
            {
                Debug.Log($"Found Station: {station}");
                var stationDirectory = Path.Combine(_dataDirectory, station);

                var files = Directory.GetFiles(stationDirectory, "*.mp3").Select(Path.GetFileName).ToArray();

                if (files.Length == 0)
                {
                    Debug.LogWarning($"No files found in station: {station}");
                    continue;
                }

                _stationMap[station] = files;

                // Log the files found in each station.
                foreach (var file in files)
                {
                    Debug.Log($"Station: {station} - Found File: {file}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing stations: {ex.Message}");
        }
    }

    // Get a list of all available stations.
    public List<string> GetStations()
    {
        if (_stationMap.Count == 0)
        {
            Debug.LogWarning("No stations available.");
        }

        return _stationMap.Keys.ToList();
    }

    // Get the list of songs for a specific station. 
    // Use TryGetValue to avoid potential KeyNotFoundException.
    public List<string> GetStationSongs(string stationName)
    {
        if (_stationMap.TryGetValue(stationName, out var songs))
        {
            return songs.ToList();
        }

        Debug.LogWarning($"Station not found: {stationName}");
        return new List<string>();  // Return an empty list if the station does not exist.
    }

    // Get the data directory.
    public string GetDataDirectory() => _dataDirectory;
}