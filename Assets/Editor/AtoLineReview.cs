using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;
using RailwayRenaissance.Cab.Research;

// Puts the NF5K review vehicle on the ATO test line (Blender track kit) and checks the route geometry, grade pose and end protection.
// Batch entry: AtoLineReview.Batch -> Assets/Scenes/AtoLineReview.unity, tools/compile/ato_line_review.json, tools/ato_track/review/vehicle_*.png.
public static class AtoLineReview
{
    public const string ScenePath="Assets/Scenes/AtoLineReview.unity";
    const int Seed=20261105;
    [Serializable] public sealed class Report
    {
        public double flatMaxContactErrorM,hillMaxContactErrorM,hillPitchErrorDeg,flatEndSpanM;
        public bool legacyValidateOk,gradeFeed,flatPitchZero,noFalseEndBrake,endBrakeNearEnd,legacyEndBrakeWouldFire;
        public string scope="Kinematic pose on the Blender line; contact error is wheel contact vs the modelled rail-top mesh (raycast), not a contact-force result.";
    }
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("ATO_LINE_REVIEW_FAIL: "+message);}

    public static void Batch()
    {
        try{Run();EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogError(e);EditorApplication.Exit(1);}
    }
    static void Run()
    {
        AtoTestLineBuilder.BuildKit();
        EditorSceneManager.OpenScene("Assets/Scenes/CabSimulationReview.unity");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath,true);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var driver=UnityEngine.Object.FindAnyObjectByType<CabSimulationDriver>();
        var body=GameObject.Find("CabInterior_Blockout").transform;
        var old=GameObject.Find("Inspection_Track_Not_Vehicle");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        var ground=GameObject.Find("Review_Ground_Not_Vehicle");
        if(ground!=null){ground.transform.position=new Vector3(0,-2.04f,-560);ground.transform.localScale=new Vector3(900,.04f,1500);} // below the ballast underside (-1.96)
        var day=UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>();if(day!=null){day.Apply(CabDaylightEnvironment.DayPreset.Overcast);day.enabled=false;}
        driver.EnsureInitialized();
        var motion=new GameObject("Rail_Motion_Controller").AddComponent<CabRailMotion>();motion.Configure(driver,body);
        var report=new Report();

        // 1. Hill kit first (grade pose and mesh heights), then the flat test line.
        var hillTrack=AtoTestLineBuilder.TrackFor(AtoTestLineBuilder.DemoLine);
        AtoTestLineBuilder.Assemble("ATO_Kit_Demo",AtoTestLineBuilder.DemoLine);
        motion.UseTrack(hillTrack);
        Require(driver.RouteGradePermille!=null,"Grade provider not wired");
        report.gradeFeed=Math.Abs(driver.RouteGradePermille(50)-20)<1e-9&&Math.Abs(driver.RouteGradePermille(200)+20)<1e-9&&Math.Abs(driver.RouteGradePermille(0))<1e-9;
        Require(report.gradeFeed,"Route grade does not match the piece list (+20 at 50 m, -20 at 200 m, 0 at 0 m)");
        report.hillMaxContactErrorM=ContactError(motion,"ATO_Kit_Demo",-40,455,.5);
        Require(report.hillMaxContactErrorM<.006,"Hill wheel contact differs from rail mesh by "+report.hillMaxContactErrorM+" m");
        motion.Apply(60,5,0,0,.02,Seed,true);          // inside the +20 permille run
        double pitch=VehiclePitchDeg(body);double expect=Math.Atan(.02)*Mathf.Rad2Deg;
        report.hillPitchErrorDeg=Math.Abs(pitch-expect);Require(report.hillPitchErrorDeg<.06,"Vehicle pitch "+pitch+" deg, expected "+expect);
        Shoot("vehicle_hill_up",motion,60,new Vector3(7,3.5f,-9));
        motion.Apply(225,5,0,0,.02,Seed,true);          // inside the -20 permille run
        Require(Math.Abs(VehiclePitchDeg(body)+expect)<.06,"Downhill pitch wrong");
        Shoot("vehicle_hill_down",motion,225,new Vector3(7,3.5f,-9));
        UnityEngine.Object.DestroyImmediate(GameObject.Find("ATO_Kit_Demo"));

        // 2. Flat ATO test line: contact, no pitch, end protection.
        var flatTrack=AtoTestLineBuilder.TrackFor(AtoTestLineBuilder.FlatLine);
        AtoTestLineBuilder.Assemble("ATO_Test_Line",AtoTestLineBuilder.FlatLine);
        motion.UseTrack(flatTrack);
        report.flatEndSpanM=flatTrack.RangeEnd-flatTrack.RangeStart;Require(Math.Abs(report.flatEndSpanM-1260)<1e-9,"Flat line span");
        report.flatMaxContactErrorM=ContactError(motion,"ATO_Test_Line",-40,1190,.5);
        Require(report.flatMaxContactErrorM<.006,"Flat wheel contact differs from rail mesh by "+report.flatMaxContactErrorM+" m");
        motion.Apply(500,5,0,0,.02,Seed,true);report.flatPitchZero=Math.Abs(VehiclePitchDeg(body))<.01;Require(report.flatPitchZero,"Flat pitch not zero");

        var task=AtoRouteTask.Standard(0);bool falseBrake=false;
        for(double s=0;s<=900;s+=1)
        {
            double v=Math.Min(task.LimitAt(s),task.PreviewSpeed(s));   // speed a compliant run may carry, slowing ahead of each lower limit
            motion.Apply(s,v,0,0,.02,Seed,true);if(motion.RouteEndBrakeRequired(v))falseBrake=true;
        }
        report.noFalseEndBrake=!falseBrake;Require(report.noFalseEndBrake,"End protection fires during a limit-compliant 0..900 m run");
        motion.Apply(1150,18,0,0,.02,Seed,true);report.endBrakeNearEnd=motion.RouteEndBrakeRequired(18);Require(report.endBrakeNearEnd,"End protection silent at 1150 m, 18 m/s");
        motion.UseTrack(new CabReviewTrack());motion.Apply(600,18,0,0,.02,Seed,true);
        report.legacyEndBrakeWouldFire=motion.RouteEndBrakeRequired(18);Require(report.legacyEndBrakeWouldFire,"Legacy line should brake past 500 m (regression reference)");
        // Regression: the existing 180 m curve acceptance (steering, contacts, shoes, backend binding, stopping) on the legacy track, unchanged.
        var legacy=NF5KRailMotionReview.Validate(motion);report.legacyValidateOk=legacy.backendDrive&&legacy.curve&&legacy.brakeClearance;
        Require(report.legacyValidateOk,"Legacy rail motion validation incomplete");
        motion.UseTrack(flatTrack);motion.Apply(0,0,0,0,0,Seed,true);

        Shoot("vehicle_start",motion,0,new Vector3(6.5f,2.4f,-7.5f));
        Shoot("vehicle_start_front",motion,0,new Vector3(4.2f,1.6f,-13f),true);
        Directory.CreateDirectory("tools/compile");
        File.WriteAllText("tools/compile/ato_line_review.json",JsonUtility.ToJson(report,true));
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ATO_LINE_REVIEW_OK "+JsonUtility.ToJson(report));
    }
    static double VehiclePitchDeg(Transform body){var f=-body.forward;return Math.Atan2(f.y,Math.Sqrt(f.x*f.x+f.z*f.z))*Mathf.Rad2Deg;}

    // Wheel contact points of all four wheelsets, dropped onto the modelled rail meshes; returns the worst vertical gap.
    static double ContactError(CabRailMotion motion,string lineName,double from,double to,double step)
    {
        var root=GameObject.Find(lineName);var added=new System.Collections.Generic.List<MeshCollider>();
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>())if(mf.name.StartsWith("Rail_")){var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;added.Add(c);}
        Physics.SyncTransforms();double worst=0;
        for(double s=from;s<=to;s+=step)
        {
            motion.Apply(s,4,0,0,.02,Seed,s==from);
            foreach(var b in motion.Bogies)for(int i=0;i<b.axles.Length;i++)foreach(int side in new[]{-1,1})
            {
                var contact=b.axles[i].position+b.frame.right*(side*.75f)-Vector3.up*(float)CabRailMotion.RollingRadius;
                // The vehicle has its own colliders above the wheel, so only the rail colliders count.
                bool found=false;double gap=0;
                foreach(var hit in Physics.RaycastAll(contact+Vector3.up*1f,Vector3.down,2f))
                    if(added.Contains(hit.collider as MeshCollider)){found=true;gap=Math.Abs(hit.point.y-contact.y);break;}
                Require(found,"No rail under wheel at s="+s);
                worst=Math.Max(worst,gap);
            }
        }
        foreach(var c in added)UnityEngine.Object.DestroyImmediate(c);
        return worst;
    }

    static void Shoot(string name,CabRailMotion motion,double station,Vector3 offsetFromBody,bool fromFront=false)
    {
        motion.Apply(station,0,0,0,0,Seed,true);
        var cam=Camera.main;if(cam==null)cam=new GameObject("Cam").AddComponent<Camera>();
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.72f,.75f,.77f);cam.fieldOfView=45;cam.nearClipPlane=.1f;cam.farClipPlane=3000;cam.cullingMask=~0;
        var centre=motion.Vehicle.TransformPoint(new Vector3(0,.2f,8.6f));
        var fwd=-motion.Vehicle.forward;fwd.y=0;fwd.Normalize();var right=Vector3.Cross(Vector3.up,fwd);
        // offsetFromBody: x to the right of travel, y up, z along travel (negative = behind the body centre, mirrored for the front view)
        cam.transform.position=centre+right*offsetFromBody.x+Vector3.up*offsetFromBody.y+fwd*(fromFront?-offsetFromBody.z+14:offsetFromBody.z);
        cam.transform.LookAt(centre+(fromFront?fwd*6:Vector3.zero));
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))l.intensity=Mathf.Max(l.intensity,1.0f);
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        var rt=RenderTexture.GetTemporary(1400,800,24);var prev=RenderTexture.active;var oldTarget=cam.targetTexture;var img=new Texture2D(1400,800,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,1400,800),0,0);img.Apply();
            Directory.CreateDirectory("tools/ato_track/review");File.WriteAllBytes("tools/ato_track/review/"+name+".png",img.EncodeToPNG());}
        finally{cam.targetTexture=oldTarget;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(img);ShaderUtil.allowAsyncCompilation=async;}
    }
}
