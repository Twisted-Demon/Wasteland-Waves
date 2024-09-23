using HarmonyLib;
using Wasteland_Waves.Source;

namespace Wasteland_Waves.Harmony;

[HarmonyPatch(typeof(GameManager))]
[HarmonyPatch("PlayerSpawnedInWorld")]
public static class GameManagerPatchPlayerSpawnedInWorld
{
    public static void Postfix(ClientInfo _cInfo)
        => SingletonMonoBehaviour<RadioManager>.Instance.PlayerSpawnedInWorld(_cInfo);
}

[HarmonyPatch(typeof(GameManager))]
[HarmonyPatch("PlayerDisconnected")]
public static class GameManagerPatchPlayerDisconnected
{
    public static void Postfix(ClientInfo _cInfo) =>
        SingletonMonoBehaviour<RadioManager>.Instance.PlayerDisconnected(_cInfo);
}

[HarmonyPatch(typeof(GameManager))]
[HarmonyPatch("SaveAndCleanupWorld")]
public static class GameManagerPatchSaveAndCleanupWorld
{
    public static void Postfix() =>
        SingletonMonoBehaviour<RadioManager>.Instance.CleanUp();
}