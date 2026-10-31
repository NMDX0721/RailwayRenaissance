using System;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KWiperStateReview
{
    public static void Validate()
    {
        var go=new GameObject("Temporary_Wiper_Weather_Separation_Review");go.SetActive(false);
        try
        {
            var controller=go.AddComponent<Cab3DSceneController>();int changes=0;
            controller.WiperStateChanged+=_=>changes++;
            controller.ForceWeather(false);controller.ToggleWipers();
            if(!controller.WipersOn||controller.IsRaining)throw new InvalidOperationException("Dry wiper operation toggles weather");
            controller.ForceWeather(true);
            if(!controller.WipersOn||!controller.IsRaining)throw new InvalidOperationException("Weather overrides wiper selection");
            controller.ToggleWipers();
            if(controller.WipersOn||!controller.IsRaining)throw new InvalidOperationException("Stopping wipers stops rain");
            if(controller.SetWipers(false)||changes!=2)throw new InvalidOperationException("Duplicate wiper state emits changes");
            Debug.Log("NF5K_WIPER_STATE_OK: independent rain/wiper states, dry switching, stopping in rain and deduplicated change events; weather API boundary checked separately from physical linkage");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
