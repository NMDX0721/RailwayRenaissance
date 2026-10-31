using System;
using System.IO;
using RailwayRenaissance.UI.Tablet.SystemShell;
using UnityEngine;

public static class TabletPresentationStorageReview
{
    public static string Validate()
    {
        void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        string directory=Path.GetFullPath("Library/TabletPresentationReview/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"presentation.json");
        var prefs=new TabletPresentationPreferences(path);Assert(!File.Exists(path),"Read created unsolicited prefs file");
        prefs.SetReduceMotion(true);prefs.SetAccent(TabletAccent.Teal);prefs.SetWallpaperDim(.3f);prefs.SetHighContrast(true);
        var restored=new TabletPresentationPreferences(path);Assert(restored.ReduceMotion&&restored.HighContrast&&restored.Accent==TabletAccent.Teal&&Math.Abs(restored.WallpaperDim-.3)<.001,"Preference round-trip failed");
        string saved=File.ReadAllText(path);restored.SetAccent((TabletAccent)99);restored.SetWallpaperDim(float.NaN);Assert(File.ReadAllText(path)==saved,"Invalid values overwrote stored data");
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
        {restored.SetAccent(TabletAccent.Blue);Assert(!string.IsNullOrEmpty(restored.LastError),"Failed write reported success");}
        Assert(File.ReadAllText(path)==saved,"Failed write destroyed original");
        string damaged=Path.Combine(directory,"damaged.json");File.WriteAllText(damaged,"{}");var invalid=new TabletPresentationPreferences(damaged);
        Assert(!string.IsNullOrEmpty(invalid.LastError)&&File.ReadAllText(damaged)=="{}","Missing schema silently accepted or rewritten");
        invalid.ResetToDefaults();Assert(string.IsNullOrEmpty(invalid.LastError)&&File.ReadAllText(damaged).Contains("schemaVersion"),"Explicit default recovery failed");
        string future=Path.Combine(directory,"future.json");File.WriteAllText(future,"{\"schemaVersion\":999,\"highContrast\":true}");var readOnly=new TabletPresentationPreferences(future);
        string before=File.ReadAllText(future);readOnly.SetReduceMotion(readOnly.ReduceMotion);readOnly.ResetToDefaults();Assert(readOnly.IsReadOnly&&File.ReadAllText(future)==before,"Newer preference schema overwritten");
        prefs.ResetToDefaults();Assert(!new TabletPresentationPreferences(path).ReduceMotion,"Reset did not persist");
        Debug.Log("TABLET_PRESENTATION_STORAGE_OK: independent JSON, round-trip, failed-write original retention, invalid enum/float, missing-schema recovery, newer-schema read-only, scoped defaults");
        return Path.Combine(directory,"runtime.json");
    }
}
