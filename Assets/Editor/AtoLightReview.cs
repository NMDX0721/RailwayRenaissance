using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

// Batch matrix of cab lighting: daylight presets x cab lamp presets, at the driver's eye, one frame each.
static class AtoRenderQualityAccess { public static bool Set(bool on) => RailwayRenaissance.Cab.AtoRenderQuality.SetAmbientOcclusion(on); }

public static class AtoLightReview
{
    static void Shoot(Camera cam, string path)
    {
        var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var prevActive = RenderTexture.active; cam.targetTexture = rt;
        for (int i = 0; i < 3; i++) cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = prevActive;
        Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
    }

    public static void Batch()
    {
        Directory.CreateDirectory("tools/ato_track/review/light");
        EditorSceneManager.OpenScene(AtoLineClosedLoop.GradedScenePath);
        var cam = Camera.main;
        var day = Object.FindAnyObjectByType<CabDaylightEnvironment>();
        var lamps = Object.FindAnyObjectByType<CabInteriorLighting>();
        Debug.Log("LIGHT_REVIEW day=" + (day != null) + " lamps=" + (lamps != null));
        cam.transform.SetParent(null);
        cam.transform.position = new Vector3(0.65f, 1.38f, 0.78f);
        cam.transform.rotation = Quaternion.LookRotation(new Vector3(-.05f, -.16f, -1.58f));
        cam.fieldOfView = 70; cam.farClipPlane = 3000;
        day.UseSkyInstance();
        var presets = new[] { CabDaylightEnvironment.DayPreset.Clear, CabDaylightEnvironment.DayPreset.LowSun, CabDaylightEnvironment.DayPreset.Dusk, CabDaylightEnvironment.DayPreset.Night };
        string[] lampNames = { "normal", "night", "service" };
        foreach (var p in presets)
            for (int l = 0; l < 3; l++)
            {
                day.Apply(p); lamps.SetCabPreset(l);
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var prevActive = RenderTexture.active; cam.targetTexture = rt;
                for (int i = 0; i < 3; i++) cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                File.WriteAllBytes("tools/ato_track/review/light/" + p + "_" + lampNames[l] + ".png", tex.EncodeToPNG());
                cam.targetTexture = null; RenderTexture.active = prevActive;
                Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            }
        // Passenger saloon and exterior views: lamps on, headlights bright, one frame per daylight preset.
        var exterior = Object.FindAnyObjectByType<CabExteriorLighting>();
        if (exterior != null) exterior.SetMode(ExteriorLightMode.Bright);
        lamps.SetCabPreset(0); lamps.SetPassengerZone(0, true); lamps.SetPassengerZone(1, true);
        string[] viewNames = { "driver", "passenger", "exterior" };
        foreach (var p in presets)
            foreach (var v in new[] { 1, 2 })
            {
                day.Apply(p); day.SetView(v); cam.fieldOfView = v == 1 ? 70 : 55; cam.farClipPlane = 3000;
                bool dark = p == CabDaylightEnvironment.DayPreset.Dusk || p == CabDaylightEnvironment.DayPreset.Night;
                lamps.SetPassengerZone(0, dark); lamps.SetPassengerZone(1, dark);
                if (exterior != null) exterior.SetMode(dark ? ExteriorLightMode.Dim : ExteriorLightMode.Off);
                Shoot(cam, "tools/ato_track/review/light/" + p + "_" + viewNames[v] + ".png");
            }
        // Time-of-day sweep at the exterior and driver views (auto lamps off so only the clock is judged).
        Directory.CreateDirectory("tools/ato_track/review/light/time");
        lamps.AllOff(); if (exterior != null) exterior.SetMode(ExteriorLightMode.Off);
        foreach (var h in new[] { 5f, 6.3f, 8f, 12f, 16f, 18f, 18.7f, 19.5f, 21f, 0f })
        {
            day.SetHour(h);
            foreach (var v in new[] { 0, 2 })
            {
                day.SetView(v); cam.fieldOfView = v == 0 ? 70 : 55; cam.farClipPlane = 3000;
                Shoot(cam, "tools/ato_track/review/light/time/h" + h.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture) + "_" + viewNames[v] + ".png");
            }
        }
        // Saloon in sunlight at several hours (lamps off): window patches should move across seats and floor.
        lamps.AllOff(); lamps.SetPassengerZone(0, false); lamps.SetPassengerZone(1, false);
        foreach (var h in new[] { 9f, 11f, 13f, 15f, 17f })
        {
            day.SetHour(h); day.SetView(1); cam.fieldOfView = 70;
            Shoot(cam, "tools/ato_track/review/light/time/saloon_" + h.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + ".png");
        }
        // SSAO off/on comparison in the saloon at midday.
        day.SetHour(11f); day.SetView(1); cam.fieldOfView = 70;
        foreach (var on in new[] { false, true })
        {
            Debug.Log("LIGHT_REVIEW ao=" + on + " found=" + AtoRenderQualityAccess.Set(on));
            Shoot(cam, "tools/ato_track/review/light/time/saloon_ao_" + (on ? "on" : "off") + ".png");
        }
        AtoRenderQualityAccess.Set(false);
        Debug.Log("LIGHT_REVIEW done");
        EditorApplication.Exit(0);
    }
}
