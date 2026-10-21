using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KInteriorLightingSetup
{
    static Transform root;
    static Material housing,diffuser,liner;
    static List<CabInteriorLighting.Fixture> fixtures;
    public static void Build(Transform parent)
    {
        foreach(string obsolete in new[]{"Ceiling_Light_Base","Ceiling_Light_Diffuser"})
        {var child=parent.Find(obsolete);if(child!=null)Object.DestroyImmediate(child.gameObject);}
        root=new GameObject("Interior_Lighting_Assembly").transform;root.SetParent(parent,false);root.gameObject.layer=30;
        housing=Mat("Interior_Luminaire_Housing",new Color(.16f,.18f,.17f),.65f,.24f,false);
        diffuser=Mat("Interior_Luminaire_Diffuser",new Color(.72f,.73f,.66f),0,.25f,true);
        liner=Mat("Interior_Ceiling_Liner",new Color(.55f,.59f,.54f),0,.18f,false);
        fixtures=new List<CabInteriorLighting.Fixture>();
        Box("Cab_Inner_Ceiling",new Vector3(0,2.17f,.68f),new Vector3(2.60f,.045f,2.22f),liner);
        for(int module=0;module<4;module++)
        {
            float z=3.85f+module*3.9f;
            Box("Passenger_Ceiling_Module_"+module,new Vector3(0,2.17f,z),new Vector3(2.60f,.045f,3.88f),liner);
            foreach(float x in new[]{-1.18f,1.18f})Box("Ceiling_Service_Trim",new Vector3(x,2.135f,z),new Vector3(.025f,.016f,3.84f),housing);
        }
        foreach(int side in new[]{-1,1})
        {
            Fixture("Cab_Ceiling_Light_"+side,InteriorLightCircuit.CabCeiling,new Vector3(side*.64f,2.10f,1.10f),new Vector3(side*.50f,.60f,.78f),new Vector3(.24f,.025f,.42f),7.5f,110,3.4f);
            Box("Task_Lamp_Wall_Plate_"+side,new Vector3(side*1.34f,1.78f,.72f),new Vector3(.025f,.14f,.10f),housing);
            Box("Task_Lamp_Support_"+side,new Vector3(side*1.22f,1.80f,.72f),new Vector3(.22f,.025f,.025f),housing);
            Fixture(side>0?"Driver_Task_Light":"Assistant_Reading_Light",side>0?InteriorLightCircuit.Task:InteriorLightCircuit.AssistantTask,new Vector3(side*1.12f,1.78f,.72f),side>0?new Vector3(.72f,.85f,.12f):new Vector3(-.85f,.70f,.90f),new Vector3(.09f,.02f,.12f),5,42,2.0f);
            Fixture("Cab_Door_Footlight_"+side,InteriorLightCircuit.Access,new Vector3(side*1.30f,.45f,1.065f),new Vector3(side*.92f,.05f,1.35f),new Vector3(.10f,.022f,.07f),1.8f,95,1.8f);
        }
        var state=parent.GetComponentInChildren<CabEngineState>();
        var console=state.transform;
        Bounds panel=new Bounds();bool found=false;
        foreach(var renderer in console.GetComponentsInChildren<Renderer>())
        {
            bool scale=false;foreach(var mat in renderer.sharedMaterials)if(mat!=null&&(mat.name.Contains("Gauge_Scale")||mat.name.Contains("Gauge_Face")||mat.name.Contains("IvoryDial")||mat.name.Contains("Dial_Ivory")))scale=true;
            if(!scale)continue;if(!found){panel=renderer.bounds;found=true;}else panel.Encapsulate(renderer.bounds);
        }
        if(!found)panel=new Bounds(new Vector3(.65f,1.08f,-.04f),new Vector3(.8f,.28f,.15f));
        for(int i=0;i<2;i++)
        {
            float x=panel.center.x+(i==0?-1:1)*panel.extents.x*.52f;
            var target=new Vector3(x,panel.center.y,panel.center.z);
            var p=new Vector3(x,Mathf.Min(panel.max.y+.12f,1.22f),panel.max.z+.18f);
            Box("Instrument_Lamp_Mount_"+i,p-Vector3.up*.065f,new Vector3(.026f,.13f,.028f),housing);
            Fixture("Instrument_Hood_Light_"+i,InteriorLightCircuit.Instrument,p,target,new Vector3(.16f,.015f,.045f),.22f,95,.48f);
        }
        for(int i=0;i<6;i++)
        {
            float z=3.0f+i*2.6f;
            Fixture("Passenger_Luminaire_"+i,i<3?InteriorLightCircuit.PassengerFront:InteriorLightCircuit.PassengerRear,new Vector3(0,2.105f,z),new Vector3(0,.40f,z),new Vector3(.24f,.025f,.72f),15,115,4.0f);
        }
        foreach(float z in new[]{2.25f,9.3f,16.45f})
            Fixture("Emergency_Aisle_Light_"+z.ToString(System.Globalization.CultureInfo.InvariantCulture),InteriorLightCircuit.Emergency,new Vector3(0,2.085f,z),new Vector3(0,.15f,z),new Vector3(.10f,.025f,.15f),3.5f,115,5.0f);
        Fixture("Cab_Emergency_Exit_Light",InteriorLightCircuit.Emergency,new Vector3(0,1.92f,1.75f),new Vector3(0,.15f,1.05f),new Vector3(.18f,.025f,.08f),.65f,85,2.2f);
        parent.gameObject.AddComponent<CabInteriorLighting>().Configure(state,fixtures.ToArray());
        Debug.Log("NF5K_INTERIOR_LIGHTING_BUILT: two cab ceiling lights, independent driver/assistant reading lights, two door footlights, two hooded instrument lights, six zoned passenger lights and four emergency fixtures");
    }
    static void Fixture(string name,InteriorLightCircuit circuit,Vector3 p,Vector3 target,Vector3 size,float intensity,float angle,float range)
    {
        var assembly=new GameObject(name).transform;assembly.SetParent(root,false);assembly.position=p;assembly.gameObject.layer=30;
        assembly.rotation=circuit==InteriorLightCircuit.Instrument?Quaternion.identity:Quaternion.FromToRotation(Vector3.down,(target-p).normalized);
        Part("Housing",Vector3.up*.022f,new Vector3(size.x+.045f,.045f,size.z+.045f),housing);
        var surface=Part("Diffuser",Vector3.zero,size,diffuser);
        foreach(float x in new[]{-size.x/2-.012f,size.x/2+.012f})Part("Retainer",new Vector3(x,-.003f,0),new Vector3(.012f,.025f,size.z+.015f),housing);
        var light=new GameObject("Interior_Beam").AddComponent<Light>();light.transform.SetParent(assembly,false);light.gameObject.layer=30;
        light.transform.localPosition=Vector3.down*(size.y/2+.015f);light.transform.rotation=Quaternion.LookRotation((target-light.transform.position).normalized);
        light.type=LightType.Spot;light.spotAngle=angle;light.innerSpotAngle=angle*.58f;light.range=range;light.intensity=0;light.enabled=false;
        light.color=circuit==InteriorLightCircuit.Instrument?new Color(1,.72f,.43f):circuit==InteriorLightCircuit.Emergency?new Color(1,.89f,.72f):new Color(1,.87f,.70f);
        light.shadows=LightShadows.Soft;light.shadowBias=.003f;light.shadowNormalBias=.01f;light.cullingMask=~0;light.renderMode=LightRenderMode.ForcePixel;
        Light bounce=null;
        if(circuit==InteriorLightCircuit.CabCeiling)
        {
            light.color=new Color(1,.94f,.84f);
            bounce=new GameObject("Cab_Local_Bounce_Approximation").AddComponent<Light>();bounce.gameObject.layer=30;bounce.transform.SetParent(root,false);
            bounce.transform.position=new Vector3(p.x*.4f,1.60f,1.05f);bounce.type=LightType.Point;bounce.range=2.2f;bounce.color=light.color;
            bounce.intensity=0;bounce.enabled=false;bounce.shadows=LightShadows.None;bounce.cullingMask=1<<30;
        }
        fixtures.Add(new CabInteriorLighting.Fixture{light=light,bounce=bounce,diffuser=surface.GetComponent<Renderer>(),circuit=circuit,intensity=intensity});
        GameObject Part(string suffix,Vector3 position,Vector3 dimensions,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name+"_"+suffix;obj.layer=30;obj.transform.SetParent(assembly,false);obj.transform.localPosition=position;obj.transform.localScale=dimensions;
            obj.GetComponent<Renderer>().sharedMaterial=mat;if(suffix=="Diffuser")obj.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
    }
    static void Box(string name,Vector3 p,Vector3 size,Material mat)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.layer=30;obj.transform.SetParent(root,false);obj.transform.localPosition=p;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    static Material Mat(string name,Color color,float metal,float smooth,bool emission)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);
        if(emission){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.white);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
        EditorUtility.SetDirty(mat);return mat;
    }
}
