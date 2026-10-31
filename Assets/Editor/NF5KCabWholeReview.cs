using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

// Inspects saved scenes without rebuilding assets or saving temporary lighting/camera states.
public static class NF5KCabWholeReview
{
    [Serializable] sealed class Report
    {
        public string scope="Saved geometry and authored clearance candidates; not certified reach or anthropometry";
        public int scenes, closureRays, captures;
        public float driverSeatTop, assistantSeatTop, driverEyeAboveSeat;
    }
    static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException(message);}
    static void Closure(Transform body,Report report)
    {
        var meshes=body.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.StartsWith("Ceiling_Closed_")).ToArray();
        Require(meshes.Length==3,"Missing side/front ceiling closure in "+body.gameObject.scene.name);
        var temporary=new GameObject("Temporary_Ceiling_Closure_Probes");
        try
        {
            var colliders=meshes.Select(m=>
            {
                var g=new GameObject(m.name);g.transform.SetParent(temporary.transform);
                g.transform.SetPositionAndRotation(m.transform.position,m.transform.rotation);g.transform.localScale=m.transform.lossyScale;
                var c=g.AddComponent<MeshCollider>();c.sharedMesh=m.sharedMesh;return c;
            }).ToArray();
            void Ray(Vector3 origin,Vector3 direction)
            {
                var ray=new UnityEngine.Ray(body.TransformPoint(origin),body.TransformDirection(direction));
                Require(colliders.Any(c=>c.Raycast(ray,out _,.5f)),"Ceiling closure leak: "+origin+" / "+direction+" in "+body.gameObject.scene.name);
                report.closureRays++;
            }
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-.14f,0f,.3f,.8f,1.5f,1.78f})
                Ray(new Vector3(side*1.20f,2.07f,z),Vector3.right*side);
            foreach(float x in new[]{-1.25f,-1.15f,-.8f,-.4f,0f,.4f,.8f,1.15f,1.25f})
                Ray(new Vector3(x,2.07f,-.1f),Vector3.back);
        }
        finally{UnityEngine.Object.DestroyImmediate(temporary);}
    }
    public static void Batch()
    {
        try
        {
            var report=new Report();
            foreach(string scene in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");
                var lighting=UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>();
                Require(lighting!=null,"Missing interior in "+scene);
                NF5KCabWorkstationReview.Validate(lighting.transform);
                Closure(lighting.transform,report);report.scenes++;
            }
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var env=UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>();
            var lights=UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>();var cam=Camera.main;
            var body=lights.transform;
            var seats=body.GetComponentsInChildren<Renderer>();
            report.driverSeatTop=seats.Single(r=>r.name=="Driver_Seat_Cushion").bounds.max.y;
            report.assistantSeatTop=seats.Single(r=>r.name=="Assistant_Seat_Cushion").bounds.max.y;
            report.driverEyeAboveSeat=1.38f-report.driverSeatTop;
            var positions=new[]{new Vector3(.65f,1.38f,.78f),new Vector3(-.65f,1.38f,.93f),new Vector3(0,1.65f,1.65f),new Vector3(.65f,1.38f,.78f),new Vector3(-.65f,1.38f,.93f),new Vector3(.65f,1.1f,.95f)};
            var targets=new[]{new Vector3(.55f,1.05f,-.45f),new Vector3(-.55f,.95f,-.3f),new Vector3(0,1.1f,-.3f),new Vector3(0,2.08f,.35f),new Vector3(0,2.08f,.35f),new Vector3(.65f,.20f,.18f)};
            var names=new[]{"driver","assistant","doorway","roof_right","roof_left","legroom"};
            foreach(bool night in new[]{false,true})
            {
                env.Apply(CabDaylightEnvironment.DayPreset.Overcast);lights.SetSupply(true,true);lights.SetCabPreset(0);
                if(night)
                {
                    RenderSettings.sun.enabled=false;RenderSettings.skybox=null;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.005f,.008f,.016f);
                    RenderSettings.ambientSkyColor=new Color(.015f,.02f,.03f);RenderSettings.ambientEquatorColor=new Color(.01f,.012f,.018f);RenderSettings.ambientGroundColor=Color.black;
                }
                lights.GetComponent<CabDiffuseFill>().Refresh();lights.GetComponent<CabInteriorReflections>()?.RefreshIfChanged();
                for(int i=0;i<names.Length;i++)
                {
                    cam.orthographic=false;cam.fieldOfView=i==2?78:70;
                    cam.transform.SetPositionAndRotation(body.TransformPoint(positions[i]),Quaternion.LookRotation(body.TransformDirection(targets[i]-positions[i]),body.up));
                    NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/whole_"+(night?"night_":"day_")+names[i]+".png");report.captures++;
                }
            }
            File.WriteAllText("tools/compile/cab_whole_review.json",JsonUtility.ToJson(report,true));
            Debug.Log("NF5K_CAB_WHOLE_REVIEW_OK: "+JsonUtility.ToJson(report));
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
