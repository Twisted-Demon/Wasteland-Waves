using System;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Wasteland_Waves.Source;

public class ResourcesManager : SingletonMonoBehaviour<ResourcesManager>
{
    private AssetBundle _assetBundle;
    
    public override void singletonAwake()
    {
        base.singletonAwake();

        //_assetBundle = AssetBundle.LoadFromFile($"Mods\\Wasteland-Waves\\Resources\\data.unity3d");
        
        
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.K))
            return;

        var guiWindows = GameManager.Instance.windowManager.windows;

        var playerUiWindows = GameManager.Instance.World.GetLocalPlayers()[0].playerUI.windowManager.windows;
        var playerUi = GameManager.Instance.World.GetLocalPlayers()[0].playerUI;
        
    }
}
