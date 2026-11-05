using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KBatteryDetailReview
{
    const string Output="tools/cab_reference/battery_detail/";
    [Serializable] sealed class Report {public int poses,parts;public string[] conflicts;public string scope="Candidate visual construction; no capacity, structural, ventilation or electrical certification";}
    public static void Batch()
    {
        try {
            Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/CabPowerLayoutReview.unity");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/CabBatteryDetailReview.unity",true);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CabBatteryDetailReview.unity");
            var body=GameObject.Find("CabInterior_Blockout").transform;var motion=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();motion.Apply(0,0,0,0,0,20261021,true);
            var root=GameObject.Find("Battery_Box").transform;UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshFilter>());UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());root.localScale=Vector3.one;
            var oldGuide=GameObject.Find("Battery_Box_Service_Direction");if(oldGuide!=null)oldGuide.SetActive(false);
            Material Mat(string name,Color color,float metal){string path="Assets/Resources/Cab/Battery_"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",.30f);EditorUtility.SetDirty(m);return m;}
            var paint=Mat("GreyGreen",new Color(.33f,.40f,.36f),.18f);var steel=Mat("ZincSteel",new Color(.49f,.52f,.49f),.65f);var dark=Mat("Rubber",new Color(.065f,.075f,.068f),0);var cell=Mat("Cell",new Color(.63f,.61f,.50f),0);var red=Mat("TerminalRed",new Color(.48f,.12f,.10f),0);
            Transform Group(string name){var g=new GameObject(name).transform;g.SetParent(root,false);g.gameObject.layer=30;return g;}
            var shell=Group("Fixed_Casing");var drawer=Group("Slide_Out_Tray");var cover=Group("Removable_Cover");
            GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent){var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));o.transform.SetParent(parent,false);o.transform.localPosition=p;o.layer=30;o.GetComponent<MeshFilter>().sharedMesh=NF5KPassengerInterior.RoundedBox(size,.0015f);o.GetComponent<Renderer>().sharedMaterial=mat;o.GetComponent<BoxCollider>().size=size;return o;}
            void Bolt(Vector3 p,Transform parent){var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name="Captive_Fastener";o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localRotation=Quaternion.Euler(0,0,90);o.transform.localScale=new Vector3(.011f,.002f,.011f);o.GetComponent<Renderer>().sharedMaterial=steel;o.layer=30;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.AddComponent<BoxCollider>();}
            Box("Floor_Pan",new Vector3(0,-.168f,0),new Vector3(.36f,.004f,.60f),paint,shell);
            Box("Rear_Wall",new Vector3(.178f,0,0),new Vector3(.004f,.332f,.60f),paint,shell);
            Box("Roof",new Vector3(0,.168f,0),new Vector3(.36f,.004f,.60f),paint,shell);
            foreach(int side in new[]{-1,1}){
                Box("End_Wall",new Vector3(0,0,side*.298f),new Vector3(.352f,.332f,.004f),paint,shell);
                Box("Cantilever_Mount",new Vector3(.015f,.24f,side*.25f),new Vector3(.33f,.05f,.045f),steel,shell);
                Box("Vertical_Mount_Web",new Vector3(.14f,.20f,side*.25f),new Vector3(.025f,.08f,.045f),steel,shell);
                Box("Outer_Rail",new Vector3(0,-.115f,side*.257f),new Vector3(.31f,.027f,.022f),steel,shell);
                Box("Tray_Runner",new Vector3(0,-.108f,side*.233f),new Vector3(.29f,.018f,.022f),steel,drawer);
                Box("Tray_End_Retainer",new Vector3(0,-.025f,side*.237f),new Vector3(.28f,.15f,.018f),steel,drawer);
                Box("Travel_Stop",new Vector3(.157f,-.11f,side*.257f),new Vector3(.008f,.04f,.032f),steel,shell);
            }
            Box("Tray_Base",new Vector3(0,-.125f,0),new Vector3(.29f,.012f,.48f),steel,drawer);
            foreach(float z in new[]{-.117f,.117f}){
                Box("Battery_Block",new Vector3(0,-.025f,z),new Vector3(.24f,.18f,.215f),cell,drawer).GetComponent<MeshFilter>().sharedMesh=NF5KPassengerInterior.RoundedBox(new Vector3(.24f,.18f,.215f),.007f);
                Box("Terminal_Insulation",new Vector3(0,.072f,z),new Vector3(.24f,.014f,.215f),dark,drawer);
                foreach(int side in new[]{-1,1})Box("Terminal_Cap",new Vector3(side*.073f,.088f,z),new Vector3(.03f,.025f,.035f),side>0?red:dark,drawer);
                Box("Hold_Down",new Vector3(0,.095f,z+.055f),new Vector3(.275f,.012f,.018f),steel,drawer);
            }
            Box("Cover_Lower",new Vector3(-.184f,-.055f,0),new Vector3(.003f,.20f,.58f),paint,cover);
            foreach(float z in new[]{-.273f,.273f})Box("Cover_Frame",new Vector3(-.184f,.075f,z),new Vector3(.012f,.11f,.034f),paint,cover);
            Box("Vent_Dark_Back",new Vector3(-.179f,.087f,0),new Vector3(.004f,.095f,.50f),dark,cover);
            for(int i=0;i<4;i++)Box("Weather_Louver",new Vector3(-.192f,.052f+i*.023f,0),new Vector3(.024f,.003f,.50f),paint,cover).transform.localRotation=Quaternion.Euler(0,0,-25);
            foreach(float z in new[]{-.24f,.24f})foreach(float y in new[]{-.12f,.13f})Bolt(new Vector3(-.194f,y,z),cover);
            foreach(float z in new[]{-.17f,.17f})Box("Cover_Pull_Mount",new Vector3(-.202f,-.055f,z),new Vector3(.025f,.018f,.025f),steel,cover);
            Box("Cover_Pull",new Vector3(-.220f,-.055f,0),new Vector3(.018f,.018f,.36f),steel,cover);
            Box("Cable_Gland",new Vector3(.15f,.10f,.307f),new Vector3(.03f,.03f,.025f),dark,shell);
            Box("Isolated_Cable_End",new Vector3(.15f,.10f,.334f),new Vector3(.015f,.015f,.035f),dark,shell);
            NF5KBatteryFineFinish.Apply(root,shell,drawer,cover,paint,steel,dark,cell,red);
            var externals=body.GetComponentsInChildren<Collider>().Where(c=>!c.transform.IsChildOf(root)&&!c.isTrigger).ToArray();var findings=new HashSet<string>();int poses=0;
            void Check(string pose){Physics.SyncTransforms();foreach(var c in root.GetComponentsInChildren<BoxCollider>())foreach(var e in externals)if(c.bounds.Intersects(e.bounds)&&Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,e,e.transform.position,e.transform.rotation,out _,out float depth)&&depth>.002f)findings.Add(pose+": "+c.name+" / "+e.name+" "+depth.ToString("F4"));}
            foreach(double station in new[]{0d,35,60,100,145,170})foreach(double speed in new[]{-3d,0,3}){motion.Apply(station,speed,.3,.5,0,20261021,true);Check("stowed");poses++;}
            motion.Apply(0,0,0,0,0,20261021,true);cover.gameObject.SetActive(false);
            for(int i=0;i<=12;i++){drawer.localPosition=Vector3.left*(i*.02f);Check("service");poses++;}
            drawer.localPosition=Vector3.zero;cover.gameObject.SetActive(true);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Output+"acceptance.json",JsonUtility.ToJson(new Report{poses=poses,parts=root.GetComponentsInChildren<MeshFilter>().Length,conflicts=findings.ToArray()},true));
            foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(!r.transform.IsChildOf(body))r.enabled=false;
            var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=.48f;camera.farClipPlane=100;
            camera.transform.position=root.TransformPoint(new Vector3(-1.6f,.60f,-.9f));camera.transform.LookAt(root.position);
            var light=new GameObject("Battery_Review_Fill").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.LookRotation(root.position-camera.transform.position);
            Shot(camera,"closed.png");cover.gameObject.SetActive(false);drawer.localPosition=Vector3.left*.24f;Shot(camera,"service.png");
            Debug.Log("NF5K_BATTERY_DETAIL_REVIEW_OK: poses="+poses+"; conflicts="+findings.Count+"; stored scene saved, inspection poses not saved");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Shot(Camera camera,string name){bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;var rt=RenderTexture.GetTemporary(4200,3000,24);var resolved=RenderTexture.GetTemporary(1400,1000,0);var previous=RenderTexture.active;var old=camera.targetTexture;var pixels=new Texture2D(1400,1000,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();camera.Render();Graphics.Blit(rt,resolved);RenderTexture.active=resolved;pixels.ReadPixels(new Rect(0,0,1400,1000),0,0);pixels.Apply();File.WriteAllBytes(Output+name,pixels.EncodeToPNG());}finally{camera.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);RenderTexture.ReleaseTemporary(resolved);UnityEngine.Object.DestroyImmediate(pixels);ShaderUtil.allowAsyncCompilation=async;}}
}
