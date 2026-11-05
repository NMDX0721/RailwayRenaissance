using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KFuelTankReview
{
    const string Output="tools/cab_reference/fuel_detail/";
    [Serializable] sealed class Report {public int poses,parts;public string[] conflicts;public string scope="Candidate welded tank geometry, no usable volume, crashworthiness or fuel-system certification";}
    public static void Batch()
    {
        try {
            Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/CabBatteryDetailReview.unity");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/CabFuelTankReview.unity",true);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CabFuelTankReview.unity");var body=GameObject.Find("CabInterior_Blockout").transform;
            var motion=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();motion.Apply(0,0,0,0,0,20261021,true);
            var root=GameObject.Find("Fuel_Tank").transform;UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshFilter>());UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());root.localScale=Vector3.one;
            var guide=GameObject.Find("Fuel_Tank_Service_Direction");if(guide!=null)guide.SetActive(false);
            Material Mat(string name,Color color,float metallic,float smooth){var path="Assets/Resources/Cab/Fuel_"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
            var paint=Mat("Paint",new Color(.28f,.33f,.30f),.20f,.25f);var steel=Mat("Steel",new Color(.46f,.48f,.43f),.68f,.36f);var rubber=Mat("Rubber",new Color(.055f,.065f,.058f),0,.12f);var red=Mat("Cap",new Color(.39f,.12f,.09f),.15f,.28f);
            Transform Group(string name){var t=new GameObject(name).transform;t.SetParent(root,false);t.gameObject.layer=30;return t;}
            var fittings=Group("External_Fittings");var internals=Group("Internal_Baffles_And_Pickups");
            GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent){var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));o.transform.SetParent(parent,false);o.transform.localPosition=p;o.layer=30;o.GetComponent<MeshFilter>().sharedMesh=NF5KPassengerInterior.RoundedBox(size,.0015f);o.GetComponent<Renderer>().sharedMaterial=mat;o.GetComponent<BoxCollider>().size=size;return o;}
            void Rod(string name,Vector3 a,Vector3 b,float diameter,Material mat,Transform parent){var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=(a+b)*.5f;o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);o.transform.localScale=new Vector3(diameter,(b-a).magnitude*.5f,diameter);o.GetComponent<Renderer>().sharedMaterial=mat;o.layer=30;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.AddComponent<BoxCollider>();}
            var profile=new[]{new Vector2(-.55f,-.20f),new Vector2(.55f,-.20f),new Vector2(.60f,-.15f),new Vector2(.60f,.15f),new Vector2(.55f,.20f),new Vector2(-.55f,.20f),new Vector2(-.60f,.15f),new Vector2(-.60f,-.15f)};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int i=0;i<profile.Length;i++){var a=profile[i];var b=profile[(i+1)%8];Quad(new Vector3(a.x,a.y,-.225f),new Vector3(b.x,b.y,-.225f),new Vector3(b.x,b.y,.225f),new Vector3(a.x,a.y,.225f));}
            foreach(int side in new[]{-1,1})for(int i=0;i<8;i++){int n=vertices.Count;vertices.Add(new Vector3(0,0,side*.225f));vertices.Add(new Vector3(profile[i].x,profile[i].y,side*.225f));var p=profile[(i+1)%8];vertices.Add(new Vector3(p.x,p.y,side*.225f));triangles.AddRange(side>0?new[]{n,n+1,n+2}:new[]{n,n+2,n+1});}
            string meshPath="Assets/Models/Cab/NF5KBodyMeshes/Fuel_Chamfered_Shell.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var shell=new GameObject("Welded_Chamfered_Shell",typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));shell.transform.SetParent(root,false);shell.layer=30;shell.GetComponent<MeshFilter>().sharedMesh=mesh;shell.GetComponent<Renderer>().sharedMaterial=paint;shell.GetComponent<BoxCollider>().size=new Vector3(1.2f,.4f,.45f);
            foreach(float z in new[]{-.226f,.226f})for(int i=0;i<8;i++){var a=profile[i];var b=profile[(i+1)%8];Rod("End_Plate_Weld",new Vector3(a.x,a.y,z),new Vector3(b.x,b.y,z),.003f,steel,fittings);}
            foreach(float z in new[]{-.145f,.145f}) {
                for(int i=0;i<8;i++){var a=profile[i]*1.012f;var b=profile[(i+1)%8]*1.012f;var o=Box("Retention_Strap",new Vector3((a.x+b.x)/2,(a.y+b.y)/2,z),new Vector3(.004f,Vector2.Distance(a,b),.032f),steel,fittings);o.transform.localRotation=Quaternion.LookRotation(Vector3.forward,new Vector3(b.x-a.x,b.y-a.y,0));}
                foreach(int side in new[]{-1,1}){
                    Box("Hanger_Web",new Vector3(side*.45f,.302f,z),new Vector3(.045f,.16f,.04f),steel,fittings);
                    Box("Hanger_Foot",new Vector3(side*.45f,.38f,z),new Vector3(.11f,.010f,.07f),steel,fittings);
                    Box("Isolation_Pad",new Vector3(side*.45f,.213f,z),new Vector3(.075f,.018f,.04f),rubber,fittings);
                    Rod("Mount_Fastener",new Vector3(side*.45f,.365f,z),new Vector3(side*.45f,.374f,z),.018f,steel,fittings);
                }
            }
            // Side filler is brought out to the vehicle side rather than ending behind other equipment.
            Rod("Filler_Neck",new Vector3(.57f,.08f,0),new Vector3(1.32f,.08f,0),.052f,steel,fittings);
            Rod("Filler_Flange",new Vector3(1.315f,.08f,0),new Vector3(1.325f,.08f,0),.090f,steel,fittings);
            Rod("Locking_Filler_Cap",new Vector3(1.326f,.08f,0),new Vector3(1.345f,.08f,0),.078f,red,fittings);
            Box("Cap_Grip",new Vector3(1.349f,.08f,0),new Vector3(.012f,.015f,.055f),steel,fittings);
            Rod("Cap_Seal",new Vector3(1.324f,.08f,0),new Vector3(1.327f,.08f,0),.081f,rubber,fittings);
            for(int i=0;i<4;i++){float a=i*Mathf.PI/2+Mathf.PI/4;var p=new Vector3(1.326f,.08f+Mathf.Cos(a)*.041f,Mathf.Sin(a)*.041f);Rod("Filler_Flange_Fastener",p,p+Vector3.right*.004f,.005f,steel,fittings);}
            foreach(float x in new[]{1.26f,1.28f})Rod("Filler_Neck_Collar",new Vector3(x-.003f,.08f,0),new Vector3(x+.003f,.08f,0),.058f,steel,fittings);
            foreach(float x in new[]{.76f,1.15f}){Box("Filler_Support",new Vector3(x,.240f,0),new Vector3(.025f,.29f,.025f),steel,fittings);Rod("Filler_Clamp",new Vector3(x-.008f,.08f,0),new Vector3(x+.008f,.08f,0),.060f,steel,fittings);}
            Rod("Sender_Flange",new Vector3(.20f,.201f,0),new Vector3(.20f,.211f,0),.120f,steel,fittings);
            Rod("Sender_Cover",new Vector3(.20f,.212f,0),new Vector3(.20f,.220f,0),.095f,paint,fittings);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var p=new Vector3(.20f+Mathf.Cos(a)*.049f,.212f,Mathf.Sin(a)*.049f);Rod("Flange_Bolt",p,p+Vector3.up*.006f,.009f,steel,fittings);}
            foreach(float z in new[]{-.07f,.07f}){
                Rod("Supply_Return_Union",new Vector3(-.25f,.20f,z),new Vector3(-.25f,.25f,z),.023f,steel,fittings);
                Rod("Supply_Return_Interface",new Vector3(-.25f,.25f,z),new Vector3(-.36f,.25f,z),.012f,rubber,fittings);
                Rod("Pickup_Return_Standpipe",new Vector3(-.25f,.20f,z),new Vector3(-.25f,z<0?-.165f:-.05f,z),.012f,steel,internals);
            }
            Rod("Vent_Standpipe",new Vector3(-.45f,.20f,0),new Vector3(-.45f,.29f,0),.018f,steel,fittings);
            Rod("Vent_Side_Interface",new Vector3(-.45f,.29f,0),new Vector3(-.55f,.29f,0),.012f,steel,fittings);
            Rod("Drain_Boss",new Vector3(.35f,-.20f,0),new Vector3(.35f,-.232f,0),.032f,steel,fittings);
            Rod("Drain_Plug",new Vector3(.35f,-.232f,0),new Vector3(.35f,-.244f,0),.024f,steel,fittings);
            foreach(int side in new[]{-1,1}){
                Box("Drain_Guard_Web",new Vector3(.35f,-.226f,side*.043f),new Vector3(.10f,.058f,.006f),steel,fittings);
                Box("Drain_Guard_Skid",new Vector3(.35f,-.255f,side*.043f),new Vector3(.10f,.005f,.015f),steel,fittings);
            }
            Box("Fuel_ID_Plate",new Vector3(.0f,.015f,-.228f),new Vector3(.18f,.055f,.003f),steel,fittings);
            var labelMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Battery_Label.mat");
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");font.RequestCharactersInTexture("DIESEL",64);
            if(labelMat!=null){
                var label=new GameObject("Diesel_Identification");label.transform.SetParent(fittings,false);label.transform.localPosition=new Vector3(0,.015f,-.230f);label.layer=30;
                var text=label.AddComponent<TextMesh>();text.font=font;text.fontSize=64;text.characterSize=.006f;text.anchor=TextAnchor.MiddleCenter;text.text="DIESEL";text.color=new Color(.04f,.05f,.04f);
                var m=new Material(labelMat);m.mainTexture=font.material.mainTexture;string path="Assets/Resources/Cab/Fuel_Label.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing==null){AssetDatabase.CreateAsset(m,path);existing=m;}else{existing.CopyPropertiesFromMaterial(m);UnityEngine.Object.DestroyImmediate(m);EditorUtility.SetDirty(existing);}label.GetComponent<Renderer>().sharedMaterial=existing;
            }
            foreach(float x in new[]{-.20f,.20f}){
                Box("Baffle_Upper",new Vector3(x,.10f,0),new Vector3(.003f,.13f,.39f),steel,internals);
                foreach(int side in new[]{-1,1})Box("Baffle_Lower_Web",new Vector3(x,-.08f,side*.125f),new Vector3(.003f,.22f,.14f),steel,internals);
            }
            var obstacles=body.GetComponentsInChildren<Collider>().Where(c=>!c.transform.IsChildOf(root)&&!c.isTrigger).ToArray();var conflicts=new HashSet<string>();int poses=0;
            void Check(string pose){Physics.SyncTransforms();foreach(var c in root.GetComponentsInChildren<BoxCollider>())foreach(var e in obstacles)if(c.bounds.Intersects(e.bounds)&&Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,e,e.transform.position,e.transform.rotation,out _,out float depth)&&depth>.002f)conflicts.Add(pose+": "+c.name+" / "+e.name+" "+depth.ToString("F4"));}
            foreach(double station in new[]{0d,35,60,100,145,170})foreach(double speed in new[]{-3d,0,3}){motion.Apply(station,speed,.3,.5,0,20261021,true);Check("stowed");poses++;}
            motion.Apply(0,0,0,0,0,20261021,true);var original=root.localPosition;
            for(int i=1;i<=10;i++){root.localPosition=original+Vector3.down*(i*.035f);Check("lowering");poses++;}root.localPosition=original;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"acceptance.json",JsonUtility.ToJson(new Report{poses=poses,parts=root.GetComponentsInChildren<MeshFilter>().Length,conflicts=conflicts.ToArray()},true));
            foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(!r.transform.IsChildOf(body)||r is LineRenderer)r.enabled=false;
            var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=.78f;camera.farClipPlane=100;camera.transform.position=root.TransformPoint(new Vector3(3f,-.4f,.40f));camera.transform.LookAt(root.position);
            var light=new GameObject("Fuel_Inspection_Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.LookRotation(root.position-camera.transform.position);
            Shot(camera,"installed.png");
            foreach(var r in body.GetComponentsInChildren<Renderer>())if(!r.transform.IsChildOf(root))r.enabled=false;
            camera.orthographicSize=.84f;camera.transform.position=root.TransformPoint(new Vector3(2.0f,.85f,-1.2f));camera.transform.LookAt(root.TransformPoint(new Vector3(.20f,.04f,0)));
            light.transform.rotation=Quaternion.LookRotation(root.position-camera.transform.position);
            Shot(camera,"detail.png");shell.SetActive(false);Shot(camera,"internal.png");shell.SetActive(true);
            camera.orthographicSize=.22f;camera.transform.position=root.TransformPoint(new Vector3(1.9f,.25f,-.40f));camera.transform.LookAt(root.TransformPoint(new Vector3(1.32f,.08f,0)));Shot(camera,"filler.png");
            Debug.Log("NF5K_FUEL_DETAIL_REVIEW_OK: poses="+poses+"; conflicts="+conflicts.Count+"; external modelling only");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Shot(Camera camera,string name){bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;var rt=RenderTexture.GetTemporary(4200,3000,24);var resolved=RenderTexture.GetTemporary(1400,1000,0);var previous=RenderTexture.active;var old=camera.targetTexture;var pixels=new Texture2D(1400,1000,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();camera.Render();Graphics.Blit(rt,resolved);RenderTexture.active=resolved;pixels.ReadPixels(new Rect(0,0,1400,1000),0,0);pixels.Apply();File.WriteAllBytes(Output+name,pixels.EncodeToPNG());}finally{camera.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);RenderTexture.ReleaseTemporary(resolved);UnityEngine.Object.DestroyImmediate(pixels);ShaderUtil.allowAsyncCompilation=async;}}
}
