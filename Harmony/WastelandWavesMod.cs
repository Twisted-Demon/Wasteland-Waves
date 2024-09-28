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

        Log.Out("Initializing SongsFileManager");
        managersGameObject.AddComponent<SongsFileManager>();
        
        Log.Out("Initializing Resources Manager");
        managersGameObject.AddComponent<ResourcesManager>();

        Log.Out("Initializing Radio Manager");
        managersGameObject.AddComponent<RadioManager>();
    }
}