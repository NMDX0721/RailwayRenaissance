using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KWiperSetup
{
    public static void Build(Transform fittings,Transform body)
    {
        foreach(var old in body.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Wiper_")).ToArray())
            if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        Material Finish(string name,Color color,float metal,float smooth)
        {
            string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        var paint=Finish("NF5K_Wiper_Housing_Enamel",new Color(.34f,.39f,.36f),.05f,.27f);
        var metal=Finish("NF5K_Wiper_Satin_Links",new Color(.50f,.53f,.54f),.85f,.30f);
        var backbone=Finish("NF5K_Wiper_Dark_Blade_Backbone",new Color(.20f,.23f,.24f),.72f,.25f);
        var rubber=Finish("NF5K_Wiper_Rubber",new Color(.05f,.057f,.06f),0,.12f);
        var grip=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Selector_Graphite_V5.mat");
        var ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Selector_Ivory_Index_V5.mat");
        var screwMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset");
        var slot=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Fastener_Recess_V4.mat");
        var cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Circulator_Smooth_Cylinder_V3.asset");
        var root=new GameObject("NF5K_Wiper_System").transform;root.SetParent(fittings,false);
        Mesh unit=null;string unitPath="Assets/Resources/Cab/Wiper_Dynamic_Link_Unit.asset";
        GameObject Part(string name,Transform parent,Vector3 position,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube,bool dynamic=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.layer=30;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;
            if(type==PrimitiveType.Cylinder)go.GetComponent<MeshFilter>().sharedMesh=cylinder;
            if(dynamic)
            {
                unit??=AssetDatabase.LoadAssetAtPath<Mesh>(unitPath);
                if(unit==null){unit=UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);unit.name="Wiper_Dynamic_Link_Unit";AssetDatabase.CreateAsset(unit,unitPath);}
                go.GetComponent<MeshFilter>().sharedMesh=unit;
            }
            return go;
        }
        void Screw(Transform parent,Vector3 position,Vector3 normal,float diameter)
        {
            var go=new GameObject("Wiper_Captive_Screw").transform;go.SetParent(parent,false);go.localPosition=position;go.localRotation=Quaternion.FromToRotation(Vector3.up,normal);
            var washer=Part("Wiper_Screw_Washer",go,new Vector3(0,.0002f,0),new Vector3(diameter*1.18f,.0002f,diameter*1.18f),metal,PrimitiveType.Cylinder);
            var cap=new GameObject("Wiper_Recessed_Screw_Head");cap.layer=30;cap.transform.SetParent(go,false);cap.transform.localPosition=new Vector3(0,.0004f,0);cap.transform.localScale=new Vector3(diameter,diameter*.4f,diameter);
            cap.AddComponent<MeshFilter>().sharedMesh=screwMesh;cap.AddComponent<MeshRenderer>().sharedMaterials=new[]{metal,slot};
        }
        var links=new CabWiperSystem.Assembly[2];
        for(int i=0;i<2;i++)
        {
            int side=i==0?-1:1;
            var glass=body.GetComponentsInChildren<Transform>().First(t=>t.name=="Front_Glass_"+side+"_Fill");
            var plane=new GameObject(side>0?"Driver_Wiper_Plane":"Assistant_Wiper_Plane").transform;plane.SetParent(root,false);plane.SetPositionAndRotation(glass.position,glass.rotation);
            var main=Part("Wiper_Main_Arm",plane,Vector3.zero,Vector3.one,metal,dynamic:true).transform;
            var parallel=Part("Wiper_Idler_Arm",plane,Vector3.zero,Vector3.one,metal,dynamic:true).transform;
            var carrier=new GameObject("Wiper_Pantograph_Carrier").transform;carrier.SetParent(plane,false);
            Part("Wiper_Carrier_Crossbar",carrier,new Vector3(0,0,-.030f),new Vector3(.050f,.016f,.008f),metal);
            var bridge=Part("Wiper_Blade_Coupling",carrier,new Vector3(0,0,-.017f),new Vector3(.022f,.013f,.022f),metal,PrimitiveType.Cylinder);bridge.transform.localRotation=Quaternion.Euler(90,0,0);
            Part("Wiper_Blade_Spine",carrier,Vector3.zero,new Vector3(.008f,.440f,.010f),backbone);
            var blade=Part("Wiper_Rubber_Contact",carrier,new Vector3(0,0,.007f),new Vector3(.010f,.440f,.006f),rubber).transform;
            foreach(float x in new[]{-.0175f,.0175f})
            {
                var bearing=Part("Wiper_Sealed_Spindle",plane,new Vector3(x,-.398f,-.013f),new Vector3(.024f,.023f,.024f),metal,PrimitiveType.Cylinder);bearing.transform.localRotation=Quaternion.Euler(90,0,0);
                var seal=Part("Wiper_Spindle_Seal",plane,new Vector3(x,-.398f,-.004f),new Vector3(.032f,.0015f,.032f),rubber,PrimitiveType.Cylinder);seal.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            var shaft=Part("Wiper_Through_Bulkhead_Shaft",plane,new Vector3(-.0175f,-.398f,.033f),new Vector3(.016f,.060f,.016f),metal,PrimitiveType.Cylinder);shaft.transform.localRotation=Quaternion.Euler(90,0,0);
            Part("Wiper_Inner_Motor_Mount",plane,new Vector3(-.025f,-.425f,.048f),new Vector3(.188f,.105f,.006f),metal);
            Part("Wiper_Inner_Motor_Housing",plane,new Vector3(-.025f,-.425f,.105f),new Vector3(.170f,.090f,.110f),paint);
            var output=Part("Wiper_Internal_Gear_Nose",plane,new Vector3(-.0175f,-.398f,.070f),new Vector3(.040f,.025f,.040f),paint,PrimitiveType.Cylinder);output.transform.localRotation=Quaternion.Euler(90,0,0);
            Part("Wiper_Inner_Service_Cover",plane,new Vector3(-.025f,-.425f,.1615f),new Vector3(.160f,.080f,.003f),paint);
            foreach(float dx in new[]{-.067f,.067f})foreach(float dy in new[]{-.028f,.028f})Screw(plane,new Vector3(-.025f+dx,-.425f+dy,.163f),Vector3.forward,.0045f);
            NF5KEquipmentPrint.Add(plane,"Wiper_Drive_Identification_"+(i+1),"Cab_Wiper_Drive_W"+(i+1)+"_V1",new Vector3(-.025f,-.425f,.1631f),new Vector2(.118f,.044f));
            Part("Wiper_Cable_Gland",plane,new Vector3(.066f,-.447f,.104f),new Vector3(.020f,.016f,.018f),rubber);
            Part("Wiper_Retained_Wiring",plane,new Vector3(.066f,-.495f,.104f),new Vector3(.008f,.085f,.008f),rubber);
            Part("Wiper_Wiring_P_Clip",plane,new Vector3(.066f,-.487f,.102f),new Vector3(.015f,.006f,.014f),metal);
            Part("Wiper_Wiring_Return",plane,new Vector3(.066f,-.535f,.080f),new Vector3(.008f,.008f,.056f),rubber);
            Part("Wiper_Harness_Bulkhead_Flange",plane,new Vector3(.066f,-.535f,.048f),new Vector3(.034f,.024f,.006f),metal);
            Part("Wiper_Harness_Entry_Grommet",plane,new Vector3(.066f,-.535f,.054f),new Vector3(.014f,.014f,.014f),rubber);
            links[i]=new CabWiperSystem.Assembly{plane=plane,glass=glass,arm=main,parallel=parallel,carrier=carrier,rubber=blade,side=side};
        }
        var panel=new GameObject("Wiper_Desk_Control").transform;panel.SetParent(root,false);panel.localPosition=new Vector3(.36f,.8405f,.29f);
        Part("Wiper_Control_Plate",panel,Vector3.zero,new Vector3(.068f,.003f,.070f),paint);
        foreach(float x in new[]{-.027f,.027f})foreach(float z in new[]{-.027f,.027f})Screw(panel,new Vector3(x,.0015f,z),Vector3.up,.0045f);
        NF5KEquipmentPrint.Add(panel,"Wiper_Control_Scale","Cab_Wiper_Control_Scale_V1",new Vector3(0,.00162f,0),new Vector2(.068f,.070f));panel.Find("Wiper_Control_Scale").localRotation=Quaternion.Euler(-90,0,0);
        var knob=new GameObject("Wiper_Three_Position_Selector").transform;knob.SetParent(panel,false);knob.localPosition=new Vector3(0,.0015f,0);
        Part("Wiper_Selector_Bezel",knob,new Vector3(0,.0008f,0),new Vector3(.024f,.0008f,.024f),metal,PrimitiveType.Cylinder);
        Part("Wiper_Selector_Grip",knob,new Vector3(0,.004f,0),new Vector3(.021f,.0025f,.021f),grip,PrimitiveType.Cylinder);
        Part("Wiper_Selector_Index",knob,new Vector3(0,.00658f,.0045f),new Vector3(.0012f,.00016f,.007f),ivory);
        panel.gameObject.layer=30;panel.gameObject.AddComponent<BoxCollider>().size=new Vector3(.068f,.018f,.070f);
        var system=root.gameObject.AddComponent<CabWiperSystem>();system.Configure(links,panel,knob,body.GetComponentInChildren<CabInteriorLighting>());
        system.BindLegacy(body.GetComponentInChildren<Cab3DSceneController>());
        Debug.Log("NF5K_WIPERS_BUILT: two plane-aligned parallelograms, inner drives, sealed shafts, rubber contact and common three-detent desk control");
    }
}
