using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabLayoutAudit
{
    [Serializable] sealed class Layout
    {
        public string status="Existing scene geometry only; no ergonomic certification or proposed changes applied";
        public Vector3 mainDeckMin,mainDeckMax,assistantShelfMin,assistantShelfMax;
        public float driverSeatX,assistantSeatX,driverWindowX,assistantWindowX,driverEyeX;
        public float mainDeckCenterX,mainDeckWidth,assistantShelfWidth,frontDeskGap;
    }
    public static void BatchAudit()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var body=GameObject.Find("CabInterior_Blockout").transform;
            Renderer Find(string name)=>body.GetComponentsInChildren<Renderer>().First(r=>r.name==name);
            Bounds BoundsInBody(Renderer renderer)
            {
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                var points=mesh.vertices.Select(v=>body.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
                var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);return b;
            }
            var main=BoundsInBody(Find("Contoured_Control_Deck"));var shelf=BoundsInBody(Find("Assistant_Work_Shelf"));
            float SeatX(string name)=>body.InverseTransformPoint(GameObject.Find(name).transform.position).x;
            var d=new Layout {mainDeckMin=main.min,mainDeckMax=main.max,assistantShelfMin=shelf.min,assistantShelfMax=shelf.max,
                mainDeckCenterX=main.center.x,mainDeckWidth=main.size.x,assistantShelfWidth=shelf.size.x,
                driverSeatX=SeatX("Driver_Seat_Pivot"),assistantSeatX=SeatX("Assistant_Seat_Pivot"),
                driverWindowX=BoundsInBody(Find("Front_Glass_1_Fill")).center.x,assistantWindowX=BoundsInBody(Find("Front_Glass_-1_Fill")).center.x,
                driverEyeX=body.InverseTransformPoint(Camera.main.transform.position).x,frontDeskGap=main.min.x-shelf.max.x};
            File.WriteAllText("tools/compile/nf5k_cab_layout_audit.json",JsonUtility.ToJson(d,true));
            Debug.Log("NF5K_CAB_LAYOUT_MEASURED: "+JsonUtility.ToJson(d));
            UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var camera=Camera.main;camera.orthographic=false;camera.transform.position=new Vector3(0,1.76f,1.72f);
            camera.transform.LookAt(new Vector3(0,1.05f,-.13f));camera.fieldOfView=75;
            var fill=new GameObject("Temporary_Cab_Layout_Inspection_Fill").AddComponent<Light>();fill.type=LightType.Point;fill.range=4;fill.intensity=.9f;fill.transform.position=new Vector3(0,1.20f,1.1f);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_layout_existing_rear.png");
            foreach(var renderer in body.GetComponentsInChildren<Renderer>())if(renderer.bounds.min.y>1.75f)renderer.enabled=false;
            camera.orthographic=true;camera.orthographicSize=1.38f;camera.transform.position=new Vector3(0,4,.55f);
            camera.transform.rotation=Quaternion.Euler(90,180,0);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_layout_existing_top.png");
            Debug.Log("NF5K_CAB_LAYOUT_AUDIT_OK: measured saved scene, temporary inspection views only; scene was not saved");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
