using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KPowerLayoutReview
{
    const string Output="tools/cab_reference/power_layout/";
    const string Scene="Assets/Scenes/CabPowerLayoutReview.unity";
    [Serializable] public sealed class Item { public string id; public Vector3 center,size; public string service; }
    [Serializable] sealed class Finding { public string item,obstacle; public double station; public float overlapM; }
    [Serializable] sealed class Report {
        public string scope="Candidate external equipment envelopes only; not rated equipment or engineering clearance approval";
        public Item[] items;public Finding[] conflicts;public int poses;
        public string[] unresolved={"Exhaust and combustion-air routing, fire/heat isolation and discharge limits", "Cooling airflow and hot-air recirculation", "Fuel capacity/heat shielding and battery service interfaces", "Structural hangers and removal tooling; old compressor cabinet remains generic"};
    }
    static readonly Item[] Items={
        new Item{id="Diesel_Generator",center=new Vector3(0,-.68f,8.25f),size=new Vector3(1.90f,.65f,1.90f),service="Right side disconnect; downward removal, tooling not modeled"},
        new Item{id="Cooling_Pack",center=new Vector3(0,-.65f,9.90f),size=new Vector3(1.80f,.58f,1.0f),service="Right side withdrawal; airflow unverified"},
        new Item{id="Battery_Box",center=new Vector3(-1.17f,-.58f,6.00f),size=new Vector3(.36f,.34f,.60f),service="Left side tray, 0.70m free workspace candidate"},
        new Item{id="Fuel_Tank",center=new Vector3(0,-.70f,10.70f),size=new Vector3(1.20f,.40f,.45f),service="Downward removal; volume is external envelope, not usable fuel capacity"}
    };
    public static void Batch()
    {
        try {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene,true);
            var scene=EditorSceneManager.OpenScene(Scene);
            var body=GameObject.Find("CabInterior_Blockout").transform;
            var motion=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();
            motion.Apply(0,0,0,0,0,20261021,true);
            foreach(var t in body.GetComponentsInChildren<Transform>())
                if((t.name=="Underfloor_Equipment_Cabinet"||t.name=="Cabinet_Service_Cover")&&body.InverseTransformPoint(t.position).z>7)t.gameObject.SetActive(false);
            var root=new GameObject("Power_Layout_Candidate").transform;root.SetParent(body,false);
            var modules=new List<Renderer>();var lines=new List<string>{"id,x,y,z,width,height,length,service"};
            for(int i=0;i<Items.Length;i++) {
                var item=Items[i];var color=new[]{new Color(.40f,.52f,.58f),new Color(.48f,.61f,.52f),new Color(.63f,.56f,.38f),new Color(.52f,.48f,.46f)}[i];
                var mat=Mat("Power_Envelope_"+item.id,color);
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=item.id;obj.transform.SetParent(root,false);obj.transform.localPosition=item.center;obj.transform.localScale=item.size;obj.layer=30;obj.GetComponent<Renderer>().sharedMaterial=mat;modules.Add(obj.GetComponent<Renderer>());
                lines.Add(string.Join(",",item.id,F(item.center.x),F(item.center.y),F(item.center.z),F(item.size.x),F(item.size.y),F(item.size.z),item.service));
                // Draw the access direction only: no fictitious mechanical assembly is implied.
                var guide=new GameObject(item.id+"_Service_Direction").AddComponent<LineRenderer>();guide.transform.SetParent(root,false);guide.useWorldSpace=false;guide.positionCount=2;float side=item.center.x<0?-1:1;guide.SetPosition(0,new Vector3(item.center.x+side*item.size.x*.5f,item.center.y,item.center.z));guide.SetPosition(1,new Vector3(side*2.1f,item.center.y,item.center.z));guide.startWidth=guide.endWidth=.018f;guide.sharedMaterial=mat;guide.gameObject.layer=30;
            }
            File.WriteAllLines(Output+"layout.csv",lines);
            var obstacles=body.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null&&!f.transform.IsChildOf(root)&&f.GetComponent<Renderer>()!=null).ToArray();
            var conflicts=new List<Finding>();int poses=0;
            foreach(double station in new[]{0d,35,60,100,145,170})foreach(double speed in new[]{-3d,0,3}) {
                motion.Apply(station,speed,.3,.5,0,20261021,true);poses++;
                foreach(var module in modules)foreach(var obstacle in obstacles) {
                    var box=LocalBounds(obstacle,body);var mb=new Bounds(module.transform.localPosition,module.transform.localScale);
                    if(!mb.Intersects(box))continue;
                    Vector3 overlap=Vector3.Min(mb.max,box.max)-Vector3.Max(mb.min,box.min);float depth=Mathf.Min(overlap.x,Mathf.Min(overlap.y,overlap.z));
                    if(depth>.002f&&!conflicts.Any(c=>c.item==module.name&&c.obstacle==obstacle.name))conflicts.Add(new Finding{item=module.name,obstacle=obstacle.name,station=station,overlapM=depth});
                }
            }
            var report=new Report{items=Items,conflicts=conflicts.ToArray(),poses=poses};
            File.WriteAllText(Output+"conflicts.json",JsonUtility.ToJson(report,true));
            motion.Apply(0,0,0,0,0,20261021,true);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if(!renderer.transform.IsChildOf(body))renderer.enabled=false;
            var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=4;camera.farClipPlane=100;
            camera.transform.position=body.TransformPoint(new Vector3(13,-.2f,8));camera.transform.LookAt(body.TransformPoint(new Vector3(0,-.6f,8)));
            var light=new GameObject("Power_Inspection_Fill").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(-25,-70,0);
            Shot(camera,"side.png");
            camera.transform.position=body.TransformPoint(new Vector3(8,-5,10));camera.transform.LookAt(body.TransformPoint(new Vector3(0,-.5f,8)));camera.orthographicSize=3.8f;Shot(camera,"underside.png");
            Debug.Log("NF5K_POWER_LAYOUT_REVIEW_OK: independent candidate scene; poses="+poses+"; conservative overlap findings="+conflicts.Count+"; unresolved routing retained");
            EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static string F(float value)=>value.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
    static Bounds LocalBounds(MeshFilter f,Transform body) {
        var b=f.sharedMesh.bounds;var result=new Bounds(body.InverseTransformPoint(f.transform.TransformPoint(b.min)),Vector3.zero);
        for(int i=0;i<8;i++)result.Encapsulate(body.InverseTransformPoint(f.transform.TransformPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z))));return result;
    }
    static Material Mat(string name,Color color){string path="Assets/Resources/Cab/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(mat);return mat;}
    static void Shot(Camera camera,string name){bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;var rt=RenderTexture.GetTemporary(1600,1000,24);var previous=RenderTexture.active;var old=camera.targetTexture;var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();File.WriteAllBytes(Output+name,pixels.EncodeToPNG());}finally{camera.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels);ShaderUtil.allowAsyncCompilation=async;}}
}
