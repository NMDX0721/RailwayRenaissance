using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KServiceControlSetup
{
    public static void Build(Transform fittings,Transform body)
    {
        var state=body.GetComponentInChildren<CabEngineState>();
        var old=body.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Horn_Push");var anchor=old.bounds.center;
        var deck=body.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Contoured_Control_Deck");float top=deck.GetComponent<Renderer>().bounds.max.y;
        foreach(var r in body.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Horn_Push"||r.name=="Horn_Push_Bezel"||r.name=="Horn_Legend"))r.enabled=false;
        var root=new GameObject("NF5K_Horn_And_Sanding_Controls").transform;root.SetParent(fittings,false);root.position=new Vector3(anchor.x+.008f,top+.0015f,anchor.z+.006f);
        Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+name+".mat");
        var plate=Load("Cab_Annunciator_Frame");var steel=Load("Cab_Fan_Satin_Steel");var dark=Load("Cab_Fan_Selector_Graphite_V5");
        var cylinder=LoadMesh("Cab_Circulator_Smooth_Cylinder_V3");
        GameObject Part(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool round=false)
        {
            var g=GameObject.CreatePrimitive(round?PrimitiveType.Cylinder:PrimitiveType.Cube);g.layer=30;g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;if(round)g.GetComponent<MeshFilter>().sharedMesh=cylinder;return g;
        }
        var mounting=Part("Service_Control_Seated_Plate",root,Vector3.zero,new Vector3(.110f,.003f,.060f),plate);
        foreach(bool sand in new[]{false,true})
        {
            var target=new GameObject(sand?"Manual_Sanding_Button":"Horn_Button").transform;target.SetParent(root,false);target.localPosition=new Vector3(sand?.024f:-.024f,.0015f,-.008f);target.gameObject.layer=30;
            Part("Service_Button_Seal",target,Vector3.up*.0005f,new Vector3(.027f,.0005f,.027f),dark,true);
            Part("Service_Button_Metal_Collar",target,Vector3.up*.0021f,new Vector3(.024f,.0015f,.024f),steel,true);
            var cap=Part("Service_Button_Moving_Cap",target,Vector3.up*.006f,new Vector3(.019f,.002f,.019f),sand?Finish("Cab_Sanding_Amber_Cap",new Color(.40f,.24f,.045f)):Load("Cab_Fan_Selector_Ivory_Index_V5"),true);
            var hit=target.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(.031f,.021f,.031f);hit.center=Vector3.up*.006f;
            target.gameObject.AddComponent<CabServiceControl>().Configure(state,sand,cap.transform);
            string label=sand?"Sand":"Horn";
            NF5KEquipmentPrint.Add(root,"Service_"+label+"_Legend","Cab_Service_"+label+"_Legend_V1",new Vector3(sand?.024f:-.024f,.00166f,.016f),new Vector2(.048f,.018f));
            var print=root.Find("Service_"+label+"_Legend");print.localRotation=Quaternion.Euler(-90,0,0);var r=print.GetComponent<Renderer>();NF5KLightingKeysSetup.ConfigureInk(r.sharedMaterial);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
        var screws=LoadMesh("Cab_Fan_Recessed_Slotted_Head_V6");var slot=Load("Cab_Fan_Fastener_Recess_V4");
        foreach(float x in new[]{-.048f,.048f})foreach(float z in new[]{-.024f,.024f})
        {
            var g=new GameObject("Service_Plate_Captive_Screw");g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=new Vector3(x,.0016f,z);g.transform.localScale=new Vector3(.0045f,.0015f,.0045f);
            g.AddComponent<MeshFilter>().sharedMesh=screws;g.AddComponent<MeshRenderer>().sharedMaterials=new[]{steel,slot};
        }
        var temporary=new GameObject("Service_Deck_Contact_Probe");temporary.transform.SetParent(deck.transform,false);
        try
        {
            var probe=temporary.AddComponent<MeshCollider>();probe.sharedMesh=deck.sharedMesh;Physics.SyncTransforms();var b=mounting.GetComponent<Renderer>().bounds;
            foreach(float x in new[]{b.min.x+.002f,b.max.x-.002f})foreach(float z in new[]{b.min.z+.002f,b.max.z-.002f})
                if(!probe.Raycast(new Ray(new Vector3(x,top+.05f,z),Vector3.down),out var hit,.08f)||Mathf.Abs(hit.point.y-top)>.0015f)throw new InvalidOperationException("Service control plate overhangs desk");
            var fan=fittings.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Fan_Control_Metal_Plate");
            foreach(var r in fan)if(b.Intersects(r.bounds))throw new InvalidOperationException("Service plate overlaps fan selector");
            var clearance=b;clearance.Expand(new Vector3(.030f,.006f,.030f));
            var brakes=body.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Short_")||r.name.StartsWith("Tall_"));
            foreach(var r in brakes)if(clearance.Intersects(r.bounds))throw new InvalidOperationException("Service plate lacks 15 mm brake envelope clearance: "+r.name);
            Debug.Log($"NF5K_SERVICE_BRAKE_CLEARANCE_OK: plate x={b.min.x:F3}..{b.max.x:F3}, z={b.min.z:F3}..{b.max.z:F3}; 15 mm horizontal expanded envelope clear of brake parts at mounting height");
        }
        finally{UnityEngine.Object.DestroyImmediate(temporary);}
        var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Resources/Cab/NF5K_Horn_Synthetic_Loop_V1.wav");
        var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        var sourceObject=new GameObject("NF5K_Air_Horn_Audio");sourceObject.transform.SetParent(fittings,false);sourceObject.transform.position=new Vector3(0,2.25f,-1.1f);
        var voice=sourceObject.AddComponent<AudioSource>();voice.clip=Resources.Load<AudioClip>("Cab/NF5K_Horn_Synthetic_Loop_V1");voice.playOnAwake=false;voice.loop=true;voice.volume=0;
        voice.spatialBlend=1;voice.minDistance=3;voice.maxDistance=100;voice.rolloffMode=AudioRolloffMode.Logarithmic;voice.dopplerLevel=0;
        sourceObject.AddComponent<CabHornAudio>().Configure(state,voice);
        if(voice.clip==null)throw new InvalidOperationException("Generate horn loop before building controls");
        Debug.Log($"NF5K_SERVICE_CONTROLS_BUILT: existing horn location, momentary horn/sand, seated 3 mm plate; root {root.position}");
    }
    static Mesh LoadMesh(string name)=>AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/"+name+".asset");
    static Material Finish(string name,Color color)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.05f);m.SetFloat("_Smoothness",.24f);EditorUtility.SetDirty(m);return m;
    }
}
