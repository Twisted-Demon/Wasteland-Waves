using System.Reflection;
using UnityEngine;
using Wasteland_Waves.Source;

namespace Wasteland_Waves.Harmony;

public class WastelandWavesMod : IModApi
{
    public void InitMod(Mod modInstance)
    {
        Debug.Log("Patching Code");
        var harmony = new HarmonyLib.Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Debug.Log("Initializing Managers");
        var managersGameObject = new GameObject
        {
            name = "Wasteland-Waves Managers"
        };
        Object.DontDestroyOnLoad(managersGameObject);

        Debug.Log("Initializing Audio File Manager");
        managersGameObject.AddComponent<SongsFileManager>();
        Debug.Log("Audio File Manager Initialized");
        
        Debug.Log("Initializing Resources Manager");
        managersGameObject.AddComponent<ResourcesManager>();
        Debug.Log("Audio Resources Initialized");

        Debug.Log("Initializing Radio Manager");
        managersGameObject.AddComponent<RadioManager>();
        Debug.Log("Audio Radio Initialized");
    }
}