using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabLightRefinement
{
    public static void Apply(CabInteriorLighting lighting)
    {
        var body=lighting.transform;var fixtures=lighting.Fixtures;
        foreach(var t in body.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Instrument_Lamp_Mount_")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        for(int i=0;i<fixtures.Length;i++)
        {
            var f=fixtures[i];if(f.light==null)continue;
            if(f.circuit==InteriorLightCircuit.Instrument)
            {
                var assembly=f.light.transform.parent;
                if(assembly.Find("Low_Profile_Migration")==null)
                {
                    assembly.position-=body.up*.10f+body.forward*.12f;
                    new GameObject("Low_Profile_Migration").transform.SetParent(assembly,false);
                }
                var housing=assembly.GetComponentsInChildren<Transform>().First(t=>t.name.EndsWith("Housing"));housing.localPosition=Vector3.up*.008f;housing.localScale=new Vector3(.18f,.016f,.045f);
                foreach(Transform part in assembly)if(part.name.EndsWith("Retainer"))part.localScale=new Vector3(.008f,.012f,.042f);
                f.intensity=.16f;f.light.spotAngle=110;f.light.innerSpotAngle=35;
            }
            if(f.circuit==InteriorLightCircuit.CabCeiling){f.intensity=4;f.light.innerSpotAngle=35;}
            fixtures[i]=f;
        }
        EditorUtility.SetDirty(lighting);
    }
    public static void Batch()
    {
        try
        {
            foreach(string n in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+n+".unity");Apply(UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>());EditorSceneManager.SaveScene(scene);
            }
            NF5KDriverLightAudit.Batch();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
