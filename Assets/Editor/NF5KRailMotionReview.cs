using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;
using RailwayRenaissance.Cab.Simulation;

public static class NF5KRailMotionReview
{
    public const string ScenePath="Assets/Scenes/CabRailMotionReview.unity";
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    [Serializable] public sealed class Report
    {
        public bool straight,curve,reverse,phaseFromDistance,stationaryWheels,suspensionSettles,frameNotRotating,brakeClearance,backendDrive;
        public double maxContactErrorM,maxSteeringDegrees,minShoeGapM=100,settledHeaveM,serviceStopM,emergencyStopM;
        public string scope="Kinematic rigid wheelsets, authored test alignment and bounded damped suspension visuals; not wheel/rail contact-force or slip simulation.";
    }
    static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Prepare()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CabSimulationReview.unity");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath,true);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var driver=UnityEngine.Object.FindFirstObjectByType<CabSimulationDriver>();
        var body=GameObject.Find("CabInterior_Blockout").transform;
        var staticTrack=GameObject.Find("Inspection_Track_Not_Vehicle");if(staticTrack!=null)UnityEngine.Object.DestroyImmediate(staticTrack);
        var ground=GameObject.Find("Review_Ground_Not_Vehicle");ground.transform.position=new Vector3(60,-1.68f,-160);ground.transform.localScale=new Vector3(600,.04f,900);
        BuildTrack();
        var motion=new GameObject("Rail_Motion_Controller").AddComponent<CabRailMotion>();motion.Configure(driver,body);
        var day=UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>();day.Apply(CabDaylightEnvironment.DayPreset.Overcast);day.enabled=false;
        var views=motion.gameObject.AddComponent<CabRailReviewViews>();views.Configure(motion,Camera.main);
        driver.ReviewPanelVisible=true;
        var result=Validate(motion);
        motion.Apply(0,0,0,0,0,20261021,true);
        File.WriteAllText("tools/compile/cab_rail_motion_complete_acceptance.json",JsonUtility.ToJson(result,true));
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Debug.Log("NF5K_RAIL_MOTION_STATIC_OK: "+JsonUtility.ToJson(result));
    }
    static Material Material(string name,Color color,float metal,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static void BuildTrack()
    {
        var path=new CabReviewTrack();var root=new GameObject("Authored_Motion_Test_Track").transform;
        var steel=Material("Motion_Rail_Steel",new Color(.56f,.58f,.59f),.88f,.35f);
        var sleeper=Material("Motion_Concrete_Sleeper",new Color(.49f,.47f,.43f),0,.15f);
        var ballast=Material("Motion_Track_Bed",new Color(.33f,.32f,.30f),0,.1f);
        var section=new[]{new Vector2(-.075f,-.172f),new Vector2(.075f,-.172f),new Vector2(.075f,-.151f),new Vector2(.012f,-.14f),new Vector2(.009f,-.04f),new Vector2(.03f,-.035f),new Vector2(.0325f,-.01f),new Vector2(.023f,-.001f),new Vector2(0,0),new Vector2(-.023f,-.001f),new Vector2(-.0325f,-.01f),new Vector2(-.03f,-.035f),new Vector2(-.009f,-.04f),new Vector2(-.012f,-.14f),new Vector2(-.075f,-.151f)};
        foreach(int side in new[]{-1,1})
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();int count=(int)((CabReviewTrack.End-CabReviewTrack.Start)/.25);
            for(int i=0;i<=count;i++)
            {
                var sample=path.At(CabReviewTrack.Start+i*.25,20261021);
                foreach(var p in section)vertices.Add(sample.Position+sample.Right*(side*.75f+p.x)+Vector3.up*p.y);
                if(i==count)continue;
                for(int j=0;j<section.Length;j++)
                {
                    int a=i*section.Length+j,b=i*section.Length+(j+1)%section.Length,c=a+section.Length,d=b+section.Length;
                    triangles.AddRange(new[]{a,d,b,a,c,d});
                }
            }
            MeshObject("Continuous_Rail_"+side,vertices,triangles,steel,root);
        }
        var boxes=new List<CombineInstance>();
        var temporary=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=temporary.GetComponent<MeshFilter>().sharedMesh;
        for(double s=CabReviewTrack.Start;s<=CabReviewTrack.End;s+=.6)
        {
            var p=path.At(s,20261021);boxes.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p.Position-Vector3.up*.225f,Quaternion.LookRotation(-p.Forward),new Vector3(2.6f,.106f,.24f))});
        }
        UnityEngine.Object.DestroyImmediate(temporary);
        var sleepers=new Mesh{name="Motion_Sleepers"};sleepers.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;sleepers.CombineMeshes(boxes.ToArray());SaveMesh("Motion_Sleepers",sleepers,sleeper,root);
        var bed=new List<Vector3>();var bedT=new List<int>();
        for(int i=0;i<=560;i++)
        {
            var p=path.At(CabReviewTrack.Start+i,20261021);bed.Add(p.Position-p.Right*1.8f-Vector3.up*.28f);bed.Add(p.Position+p.Right*1.8f-Vector3.up*.28f);
            if(i<560){int a=i*2;bedT.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
        }
        MeshObject("Motion_Ballast_Bed",bed,bedT,ballast,root);
    }
    static void MeshObject(string name,List<Vector3> vertices,List<int> triangles,Material mat,Transform root)
    {
        var mesh=new Mesh{name=name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(name,mesh,mat,root);
    }
    static void SaveMesh(string name,Mesh candidate,Material mat,Transform root)
    {
        string path=Folder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=candidate;AssetDatabase.CreateAsset(mesh,path);}else{EditorUtility.CopySerialized(candidate,mesh);UnityEngine.Object.DestroyImmediate(candidate);}
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.layer=30;g.transform.SetParent(root,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=mat;EditorUtility.SetDirty(mesh);
    }
    public static Report Validate(CabRailMotion motion)
    {
        var r=new Report();const int seed=20261021;
        motion.Apply(0,0,0,0,0,seed,true);var axle=motion.Bogies[0].axles[0];
        motion.Apply(CabRailMotion.RollingRadius*Math.PI*.5,2,0,0,.02,seed,true);
        Assert(Vector3.Dot(axle.localRotation*Vector3.down,Vector3.forward)>.999f,"Wheel rotation is opposite rolling direction");r.straight=true;
        motion.Apply(2*Math.PI*CabRailMotion.RollingRadius,2,0,0,.02,seed,true);
        Assert(Math.Abs(motion.WheelAngleDegrees+360)<1e-6,"One circumference does not produce one revolution");r.phaseFromDistance=true;
        motion.Apply(-2*Math.PI*CabRailMotion.RollingRadius,-2,0,0,.02,seed,true);Assert(Math.Abs(motion.WheelAngleDegrees-360)<1e-6,"Signed reverse phase failed");r.reverse=true;
        for(int i=0;i<1000;i++)
        {
            double s=60+i*.06;motion.Apply(s,3,.4,i<500?0:1,.02,seed,i==0);CheckContacts(motion,r);
            foreach(var b in motion.Bogies)
            {
                var forward=b.frame.forward;var body=motion.Vehicle.forward;forward.y=body.y=0;
                r.maxSteeringDegrees=Math.Max(r.maxSteeringDegrees,Vector3.Angle(forward,body));
                foreach(var gear in b.frame.GetComponentsInChildren<Transform>())if(gear.name=="Non_Rotating_Axle_Drive")Assert(Quaternion.Angle(gear.localRotation,Quaternion.identity)<.001,"Gearcase rotates with wheel");
                CheckShoes(b,r);
            }
        }
        Assert(r.maxSteeringDegrees>1&&r.maxSteeringDegrees<3,"Bogie steering missing or excessive");r.curve=true;r.frameNotRotating=true;r.brakeClearance=r.minShoeGapM>.002;
        Assert(r.brakeClearance,"Brake shoe penetrates conservative tread envelope");
        double phase=motion.WheelAngleDegrees;
        for(int i=0;i<1000;i++)motion.Apply(119.94,0,0,1,.02,seed);
        Assert(Math.Abs(phase-motion.WheelAngleDegrees)<1e-7,"Stationary wheel continues spinning");r.stationaryWheels=true;
        r.settledHeaveM=Math.Abs(motion.HeaveError);Assert(r.settledHeaveM<1e-5&&Math.Abs(motion.RollDegrees)<1e-4&&Math.Abs(motion.PitchDegrees)<1e-4,"Stopped suspension does not settle");r.suspensionSettles=true;
        var model=new CabSimulation(seed);var input=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,ParkingBrake=true,Brake=1};
        for(int i=0;i<15000;i++)model.Step(input);
        input.ParkingBrake=false;input.Direction=1;input.Throttle=3;
        for(int i=0;i<1500;i++){model.Step(input);var s=model.State;motion.Apply(s.PositionM,s.SpeedMps,s.Acceleration,s.LocoCylinderKpa/380,.02,seed,i==0);CheckContacts(motion,r);}
        Assert(model.State.SpeedMps>1&&Math.Abs(motion.WheelAngleDegrees+model.State.PositionM/CabRailMotion.RollingRadius*180/Math.PI)<1e-5,"Backend motion binding drift");r.backendDrive=true;
        var moving=model.State.Copy();r.serviceStopM=Stop(motion,moving,3);r.emergencyStopM=Stop(motion,moving,4);
        Assert(r.emergencyStopM>0&&r.emergencyStopM<r.serviceStopM,"Service/emergency stopping relation failed");
        return r;
    }
    static double Stop(CabRailMotion motion,SimulationState start,int brake)
    {
        var m=new CabSimulation(start.Seed);m.Restore(start);var input=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,ParkingBrake=false,Direction=1,Brake=brake};
        for(int i=0;i<15000&&Math.Abs(m.State.SpeedMps)>.00001;i++)
        {m.Step(input);var s=m.State;motion.Apply(s.PositionM,s.SpeedMps,s.Acceleration,Math.Max(s.LocoCylinderKpa,s.TrainCylinderKpa)/380,.02,s.Seed,i==0);}
        Assert(m.State.SpeedMps==0,"Braking scenario failed to stop");return m.State.PositionM-start.PositionM;
    }
    static void CheckContacts(CabRailMotion motion,Report report)
    {
        foreach(var b in motion.Bogies)for(int i=0;i<b.axles.Length;i++)foreach(int side in new[]{-1,1})
        {
            var sample=motion.Track.At(motion.Station+b.stationOffset-b.axlePositions[i].z,motion.TrackSeed);
            var contact=b.axles[i].position+b.frame.right*(side*.75f)-Vector3.up*(float)CabRailMotion.RollingRadius;
            double error=Vector3.Distance(contact,sample.Position+sample.Right*(side*.75f));report.maxContactErrorM=Math.Max(report.maxContactErrorM,error);
            Assert(error<.006,"Kinematic contact anchor deviates by more than 6 mm");
        }
    }
    static void CheckShoes(CabRailMotion.Bogie b,Report r)
    {
        for(int i=0;i<b.shoes.Length;i++)
        {
            int nearest=0;for(int j=1;j<b.axles.Length;j++)if(Mathf.Abs(b.axlePositions[j].z-b.shoePositions[i].z)<Mathf.Abs(b.axlePositions[nearest].z-b.shoePositions[i].z))nearest=j;
            var centre=b.frame.InverseTransformPoint(b.axles[nearest].position);
            foreach(var v in b.shoes[i].GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                var point=b.frame.InverseTransformPoint(b.shoes[i].TransformPoint(v))-centre;
                r.minShoeGapM=Math.Min(r.minShoeGapM,Math.Sqrt(point.y*point.y+point.z*point.z)-.462);
            }
        }
    }
    public static void BatchBuild()
    {
        try
        {
            Directory.CreateDirectory("tools/compile");Prepare();
            var motion=UnityEngine.Object.FindFirstObjectByType<CabRailMotion>();var camera=Camera.main;
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            foreach(var pose in new[]{(0.0,"straight"),(110.0,"curve")})
            {
                motion.Apply(pose.Item1,pose.Item1==0?0:3,0,0,0,20261021,true);
                var p=motion.Vehicle.TransformPoint(new Vector3(0,-.3f,2.5f));
                camera.orthographic=true;camera.orthographicSize=2.6f;camera.transform.position=p+motion.Vehicle.rotation*new Vector3(6,2,-4);camera.transform.LookAt(p);
                NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/rail_motion_complete_"+pose.Item2+".png");
            }
            var centre=motion.Vehicle.TransformPoint(new Vector3(0,.1f,8.6f));camera.orthographicSize=12;
            camera.transform.position=centre+motion.Vehicle.rotation*new Vector3(22,12,-18);camera.transform.LookAt(centre);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/rail_motion_complete_overview.png");
            for(int i=0;i<20;i++)
            {
                motion.Apply(110+i*.12,3,0,0,.04,20261021,i==0);
                var p=motion.Vehicle.TransformPoint(new Vector3(0,-.65f,2.5f));camera.orthographicSize=1.65f;
                camera.transform.position=p+motion.Vehicle.rotation*new Vector3(5,.9f,1.8f);camera.transform.LookAt(p);
                NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/rail_motion_complete_frames/frame_"+i.ToString("D3")+".png");
            }
            motion.Apply(0,0,0,0,0,20261021,true);
            NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
