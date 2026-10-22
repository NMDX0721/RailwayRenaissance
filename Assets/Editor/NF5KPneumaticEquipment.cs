using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KPneumaticEquipment
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    static Material tank,pipe,rubber,hardware,red,sand;
    public static void Apply(Transform body)
    {
        tank=Mat("Air_Reservoir_Warm_Grey",new Color(.48f,.53f,.52f),.08f,.3f);
        pipe=Mat("Air_Pipe_Grey_Blue",new Color(.28f,.37f,.40f),.2f,.3f);
        rubber=Mat("Service_Hose_Rubber",new Color(.075f,.085f,.09f),0,.18f);
        hardware=Mat("Service_Fitting_Steel",new Color(.60f,.63f,.64f),.75f,.35f);
        red=Mat("Service_Valve_Red",new Color(.56f,.12f,.10f),.04f,.3f);
        sand=Mat("Sandbox_Warm_Enamel",new Color(.56f,.55f,.44f),.05f,.28f);
        foreach(var hose in body.GetComponentsInChildren<CabFlexibleHose>(true))UnityEngine.Object.DestroyImmediate(hose.gameObject);
        DestroyChild(body,"Pneumatic_Service_Equipment");var root=Group("Pneumatic_Service_Equipment",body);
        foreach(int side in new[]{-1,1})
        {
            Reservoir("Main_Reservoir",new Vector3(side*.64f,-.59f,12),.22f,1.35f,root);
            Line(side<0?"Main_Reservoir_Trunk":"Brake_Pipe_Trunk",new[]{new Vector3(side*1.26f,-.28f,.40f),new Vector3(side*1.26f,-.28f,17.1f)},.011f,pipe,root);
            for(float z=.8f;z<17;z+=1.4f)Box("Trunk_Pipe_Clamp",new Vector3(side*1.25f,-.28f,z),new Vector3(side<0?.075f:.22f,.038f,.022f),hardware,root);
            Line("Reservoir_Feed",new[]{new Vector3(side*.64f,-.59f,11.12f),new Vector3(side*.64f,-.42f,10.98f)},.012f,pipe,root);
        }
        Line("Reservoir_Common_Header",new[]{new Vector3(.64f,-.42f,10.98f),new Vector3(-1.26f,-.42f,10.98f),new Vector3(-1.26f,-.28f,10.98f)},.012f,pipe,root);
        Line("Brake_Cylinder_Trunk",new[]{new Vector3(1.32f,-.28f,.4f),new Vector3(1.32f,-.28f,17.1f)},.009f,pipe,root);
        Reservoir("Auxiliary_Reservoir",new Vector3(-.70f,-.58f,4.75f),.12f,.50f,root);
        Box("Brake_Valve_Manifold",new Vector3(.65f,-.58f,4.75f),new Vector3(.28f,.32f,.40f),tank,root);
        int circuit=0;foreach(float z in new[]{4.62f,4.75f,4.88f})
        {
            Rod("Manifold_Union",new Vector3(.80f,-.58f,z),new Vector3(.85f,-.58f,z),.022f,hardware,root);
            float x=circuit==0?-1.26f:circuit==1?1.26f:1.32f;
            Line(circuit==0?"Manifold_MR_Supply":circuit==1?"Manifold_BP_Control":"Manifold_BC_Output",new[]{new Vector3(.85f,-.58f,z),new Vector3(x,-.58f,z),new Vector3(x,-.28f,z)},.009f,pipe,root);circuit++;
        }
        Line("Auxiliary_To_Valve",new[]{new Vector3(-.70f,-.58f,4.38f),new Vector3(-.70f,-.40f,4.30f),new Vector3(.65f,-.40f,4.30f),new Vector3(.65f,-.58f,4.54f)},.010f,pipe,root);
        foreach(float x in new[]{-.20f,.20f})
        {
            Rod("Air_Dryer_Canister",new Vector3(x,-.67f,7),new Vector3(x,-.45f,7),.08f,tank,root);
            Sphere("Dryer_Lower_Cap",new Vector3(x,-.67f,7),.08f,tank,root);Sphere("Dryer_Upper_Cap",new Vector3(x,-.45f,7),.08f,tank,root);
            Rod("Dryer_Purge_Port",new Vector3(x,-.75f,7),new Vector3(x,-.80f,7),.014f,hardware,root);
            Box("Dryer_Mount",new Vector3(x,-.35f,7),new Vector3(.20f,.07f,.23f),hardware,root);
        }
        Line("Compressor_Pack_To_Dryer",new[]{new Vector3(.4f,-.57f,6.73f),new Vector3(.4f,-.47f,6.84f),new Vector3(-.38f,-.47f,6.84f),new Vector3(-.28f,-.47f,7)},.012f,pipe,root);
        Line("Dryer_To_MR_Trunk",new[]{new Vector3(.28f,-.47f,7),new Vector3(.38f,-.47f,7.14f),new Vector3(-1.26f,-.47f,7.14f),new Vector3(-1.26f,-.28f,7.14f)},.012f,pipe,root);
        Line("Dryer_Changeover",new[]{new Vector3(-.20f,-.40f,7),new Vector3(.20f,-.40f,7)},.010f,hardware,root);
        Line("Front_Brake_Trunk_Connection",new[]{new Vector3(1.26f,-.28f,.40f),new Vector3(1.26f,-.38f,-.38f),new Vector3(.49f,-.38f,-.38f),new Vector3(.49f,-.10f,-.73f),new Vector3(.49f,.015f,-.66f)},.012f,pipe,root);
        int boxes=0;
        foreach(var node in body.GetComponentsInChildren<Transform>())
        {
            if(node==null||!node.name.StartsWith("Powered_Bogie_"))continue;
            DestroyChild(node,"Sanding_Equipment");var sanding=Group("Sanding_Equipment",node);
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
            {
                Box("Sand_Container",new Vector3(side*1.02f,-.55f,end*1.95f),new Vector3(.27f,.30f,.30f),sand,sanding);
                Box("Sand_Filler_Lid",new Vector3(side*1.02f,-.387f,end*1.95f),new Vector3(.18f,.018f,.20f),hardware,sanding);
                Box("Lid_Latch",new Vector3(side*1.02f,-.37f,end*1.95f),new Vector3(.06f,.016f,.025f),hardware,sanding);
                Rod("Sandbox_Hanger",new Vector3(side*1.05f,-.44f,end*1.40f),new Vector3(side*1.05f,-.43f,end*1.95f),.022f,hardware,sanding);
                Rod("Sandbox_Brace",new Vector3(side*1.08f,-.58f,end*1.45f),new Vector3(side*1.08f,-.65f,end*1.95f),.016f,hardware,sanding);
                var outlet=new Vector3(side*.75f,-1.3062222f,end*1.37f);
                Line("Sand_Delivery_Hose",new[]{new Vector3(side*1.02f,-.71f,end*1.95f),new Vector3(side*.90f,-.85f,end*1.80f),new Vector3(side*.90f,-1.21f,end*1.72f),outlet},.010f,rubber,sanding);
                var nip=new Vector3(side*.75f,CabReviewTrack.RailDatum,end*1.10f);
                Rod("Sand_Nozzle",outlet-((nip-outlet).normalized*.065f),outlet,.012f,hardware,sanding);
                Rod("Nozzle_Support",new Vector3(side*1.05f,-.50f,end*1.50f),new Vector3(side*.90f,-1.19f,end*1.72f),.014f,hardware,sanding);
                Line("Sandbox_Air_Line",new[]{new Vector3(side*1.245f,-.32f,0),new Vector3(side*1.245f,-.32f,end*1.72f),new Vector3(side*1.12f,-.55f,end*1.95f)},.006f,pipe,sanding);
                boxes++;
            }
            Line("Sander_MR_Crossfeed",new[]{new Vector3(-1.245f,-.32f,.16f),new Vector3(-1.245f,-.72f,.16f),new Vector3(1.245f,-.72f,.16f),new Vector3(1.245f,-.32f,.16f)},.006f,pipe,sanding);
            foreach(int end in new[]{-1,1})
            {
                Line("BC_Distribution",new[]{new Vector3(1.245f,-.36f,.24f),new Vector3(1.245f,-.75f,.24f),new Vector3(1.245f,-.75f,end*.29f),new Vector3(-.795f,-.75f,end*.29f),new Vector3(-.795f,-.80f,end*.29f)},.008f,pipe,sanding);
                Line("BC_Right_Branch",new[]{new Vector3(.795f,-.75f,end*.29f),new Vector3(.795f,-.80f,end*.29f)},.008f,pipe,sanding);
            }
            float station=node.localPosition.z;
            foreach(int side in new[]{-1,1})
            {
                var bodyPort=Group("Body_Air_Port",root);bodyPort.localPosition=new Vector3(side<0?-1.26f:1.32f,-.28f,station-.16f);
                var bogiePort=Group("Bogie_Air_Port",sanding);bogiePort.localPosition=new Vector3(side*1.245f,side<0?-.32f:-.36f,side<0?.16f:.24f);
                var hose=Group("Flexible_Bogie_Service_Hose_"+station+"_"+side,body);hose.gameObject.AddComponent<MeshFilter>();hose.gameObject.AddComponent<MeshRenderer>().sharedMaterial=rubber;
                var connection=hose.gameObject.AddComponent<CabFlexibleHose>();connection.Configure(bodyPort,bogiePort);
                var mesh=new Mesh{name=hose.name};connection.UpdateGeometry(mesh);string path=Folder+hose.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null){AssetDatabase.CreateAsset(mesh,path);existing=mesh;}else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);}
                hose.GetComponent<MeshFilter>().sharedMesh=existing;EditorUtility.SetDirty(existing);
            }
        }
        if(boxes!=8)throw new InvalidOperationException("Expected eight sand containers");
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=body.gameObject.layer;
        foreach(var t in body.GetComponentsInChildren<Transform>())if(t.name=="Sanding_Equipment"||t.name.StartsWith("Flexible_Bogie_Service_Hose"))foreach(var part in t.GetComponentsInChildren<Transform>())part.gameObject.layer=body.gameObject.layer;
        Validate(body);Debug.Log("NF5K_PNEUMATIC_EQUIPMENT_OK: two main reservoirs, auxiliary reservoir, twin dryer, manifold, eight sand containers/nozzles and four flexible body/bogie connections; visual equipment only");
    }
    static void Reservoir(string name,Vector3 p,float radius,float length,Transform root)
    {
        Rod(name,p-Vector3.forward*length*.5f,p+Vector3.forward*length*.5f,radius,tank,root);
        foreach(int end in new[]{-1,1})Sphere(name+"_Dished_End",p+Vector3.forward*(end*length*.5f),radius,tank,root);
        foreach(float z in new[]{-length*.30f,length*.30f})
        {
            Band(p+Vector3.forward*z,radius,root);
            float top=p.y+radius;Box("Reservoir_Saddle",new Vector3(p.x,(top-.315f)*.5f,p.z+z),new Vector3(radius*1.8f,-.315f-top+.012f,.075f),hardware,root);
        }
        Rod("Reservoir_Drain_Cock",p-Vector3.up*radius,p-Vector3.up*(radius+.075f),.012f,hardware,root);
        Rod("Drain_Closed_Lever",p+new Vector3(-.04f,-radius-.055f,0),p+new Vector3(.04f,-radius-.055f,0),.008f,red,root);
    }
    static void Validate(Transform body)
    {
        var obstacles=new List<Renderer>();foreach(var r in body.GetComponentsInChildren<Renderer>())if(r.name=="Steel_Wheel"||r.name=="Axle_Gearcase"||r.name=="Traction_Motor_Envelope"||r.name=="Underfloor_Equipment_Cabinet")obstacles.Add(r);
        foreach(var r in body.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Main_Reservoir")||r.name.StartsWith("Auxiliary_Reservoir")||r.name=="Sand_Container")
            foreach(var obstacle in obstacles)if(r.bounds.Intersects(obstacle.bounds))throw new InvalidOperationException(r.name+" overlaps "+obstacle.name);
        float minGap=10;
        foreach(var node in body.GetComponentsInChildren<Transform>())if(node.name.StartsWith("Powered_Bogie_"))
        foreach(var nozzle in node.GetComponentsInChildren<MeshFilter>())if(nozzle.name=="Sand_Nozzle"||nozzle.name=="Sand_Delivery_Hose")
        foreach(var vertex in nozzle.sharedMesh.vertices)
        {
            var p=node.InverseTransformPoint(nozzle.transform.TransformPoint(vertex));float x=Mathf.Abs(p.x);if(x<.68f||x>.88f)continue;
            float radius=x<.735f?.49f:.462f;float dz=p.z-Mathf.Sign(p.z)*1.1f;
            minGap=Mathf.Min(minGap,Mathf.Sqrt((p.y+.90f)*(p.y+.90f)+dz*dz)-radius);
        }
        if(minGap<.002f)throw new InvalidOperationException("Sanding nozzle intersects conservative wheel envelope: "+minGap);
        Debug.Log("NF5K_SANDING_CLEARANCE_OK: conservative nozzle/tube gap "+minGap+" m; no discharge simulation");
    }
    static void Band(Vector3 p,float radius,Transform root)
    {
        string path=Folder+"Reservoir_Flat_Band_"+Mathf.RoundToInt(radius*1000)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<=72;i++)
            {
                float a=i*Mathf.PI/36;
                foreach(var q in new[]{new Vector2(radius+.001f,-.020f),new Vector2(radius+.004f,-.020f),new Vector2(radius+.004f,.020f),new Vector2(radius+.001f,.020f)})v.Add(new Vector3(q.x*Mathf.Cos(a),q.x*Mathf.Sin(a),q.y));
                if(i==72)continue;for(int j=0;j<4;j++){int aa=i*4+j,b=i*4+(j+1)%4,c=b+4,d=aa+4;t.AddRange(new[]{aa,c,b,aa,d,c});}
            }
            mesh=new Mesh{name="Flat_Reservoir_Strap"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        var g=Group("Reservoir_Retaining_Band",root);g.localPosition=p;g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=hardware;
    }
    static void DestroyChild(Transform root,string name){var t=root.Find(name);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
    static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Material Mat(string name,Color color,float metallic,float smooth){string p="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
    static void Box(string name,Vector3 p,Vector3 scale,Material m,Transform root){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
    static void Sphere(string name,Vector3 p,float radius,Material m,Transform root){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=Vector3.one*(radius*2);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
    static void Rod(string name,Vector3 a,Vector3 b,float radius,Material m,Transform root){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=(a+b)*.5f;g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);g.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
    static void Line(string name,Vector3[] p,float radius,Material m,Transform root){for(int i=0;i<p.Length-1;i++)Rod(name,p[i],p[i+1],radius,m,root);for(int i=1;i<p.Length-1;i++)Sphere(name+"_Elbow",p[i],radius,m,root);}
    public static void BatchBuild()
    {
        try
        {
            foreach(string name in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"}){var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");Apply(GameObject.Find("CabInterior_Blockout").transform);EditorSceneManager.SaveScene(scene);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();var light=new GameObject("Temporary_Service_Inspection_Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.8f;light.transform.rotation=Quaternion.Euler(30,-50,0);
            var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=2.15f;camera.transform.position=new Vector3(4,-.05f,14);camera.transform.LookAt(new Vector3(0,-.50f,11.8f));
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/pneumatic_reservoir_verified.png");
            camera.orthographicSize=1.85f;camera.transform.position=new Vector3(4,.05f,.3f);camera.transform.LookAt(new Vector3(.6f,-.65f,2.5f));
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/sanding_equipment_verified.png");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
