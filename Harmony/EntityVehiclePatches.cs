﻿using HarmonyLib;
using UnityEngine;
using Wasteland_Waves.Source;

namespace Wasteland_Waves.Harmony;

[HarmonyPatch(typeof(EntityVehicle))]
[HarmonyPatch("Init")]
public class EntityVehicleInit
{
    public static void Postfix(EntityVehicle __instance)
    {
        var gameObject = __instance.gameObject;
        var radio = gameObject.AddComponent<VehicleRadioComponent>();
        radio.Init(__instance);

        Log.Out("Added Radio Component to {0}", gameObject.name);
    }
}