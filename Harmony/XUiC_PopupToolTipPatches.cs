using HarmonyLib;
using UniLinq;
// ReSharper disable InconsistentNaming

namespace Wasteland_Waves.Harmony;

[HarmonyPatch(typeof(XUiC_PopupToolTip))]
[HarmonyPatch("DisplayTooltipText")]
public class XUiC_PopupToolTipDisplayTooltipText
{
    public static bool Prefix(XUiC_PopupToolTip __instance)
    {
        __instance.countdownTooltip.SetTimeout(5.0f);

        return true;
    }
    
    public static void Postfix(XUiC_PopupToolTip __instance)
    {
        if (__instance.tooltipText.Contains("WWMOD:"))
        {
            __instance.tooltipText = __instance.tooltipText.Replace("WWMOD:", "");
        }

    }
}

[HarmonyPatch(typeof(XUiC_PopupToolTip))]
[HarmonyPatch("QueueTooltipInternal")]
public class XuiC_PopupToolTipQueueTooltipInternal
{
    public static bool Prefix(XUiC_PopupToolTip __instance, ref string _text)
    {
        if (!_text.Contains("WWMOD:")) return true;
        
        //clear tooltips so that we can be the next one
        __instance.ClearTooltipsInternal();
        
        //set time out to 0 and force a new tooltip to be shown. 
        __instance.countdownTooltip.SetTimeout(0);
        
        return true;
    }
}
