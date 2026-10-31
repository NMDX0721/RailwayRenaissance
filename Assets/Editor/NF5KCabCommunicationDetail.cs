using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabCommunicationDetail
{
    [Serializable] sealed class Profile { public string radioTitle,radioText,recordTitle,recordText; }
    public static void Build(Transform root,Material enamel,Material steel,Material black,Material paper)
    {
        var profile=JsonUtility.FromJson<Profile>(Resources.Load<TextAsset>("Cab/workstation_equipment_profile").text);
        Transform Find(string name)=>root.GetComponentsInChildren<Transform>().First(t=>t.name==name);
        GameObject Box(string name,Vector3 p,Vector3 size,Material material,bool solid=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(root,false);
            g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        // The front bearer connects the outer return and the inboard apron without occupying the knee opening.
        Box("Aux_Underdeck_Front_Bearer",new Vector3(-.80f,.778f,-.225f),new Vector3(.91f,.032f,.040f),steel,true);
        Box("Aux_Underdeck_Cable_Trunk",new Vector3(-.80f,.766f,-.182f),new Vector3(.86f,.035f,.035f),enamel,true);
        foreach(float x in new[]{-1.18f,-.42f})
        {
            Box("Aux_Bearer_End_Gusset",new Vector3(x,.752f,-.224f),new Vector3(.070f,.070f,.018f),steel);
            foreach(float y in new[]{.73f,.77f})Box("Aux_Bearer_Gusset_Bolt",new Vector3(x,y,-.211f),new Vector3(.010f,.010f,.008f),steel);
        }
        foreach(float x in new[]{-1.10f,-.50f})foreach(float y in new[]{.645f,.745f})
            Box("Aux_Service_Captive_Fastener",new Vector3(x,y,-.254f),new Vector3(.009f,.009f,.005f),steel);
        Box("Aux_Desk_Cable_Gland",new Vector3(-1.14f,.844f,-.20f),new Vector3(.030f,.010f,.025f),black);

        var radio=new GameObject("Crew_Train_Radio").transform;radio.gameObject.layer=30;radio.SetParent(root,false);radio.localPosition=new Vector3(-.99f,.904f,-.17f);
        foreach(var t in root.Cast<Transform>().Where(t=>t.name.StartsWith("Radio_")).ToArray())t.SetParent(radio,true);
        var receiver=Find("Radio_Receiver_Visual_Only");receiver.name="Radio_Receiver_Enclosure";
        foreach(float x in new[]{-1.09f,-.89f})
        {
            Box("Radio_Securing_Washer",new Vector3(x,.853f,-.13f),new Vector3(.018f,.003f,.018f),steel).transform.SetParent(radio,true);
            Box("Radio_Securing_Bolt",new Vector3(x,.856f,-.13f),new Vector3(.009f,.004f,.009f),steel).transform.SetParent(radio,true);
        }
        for(int i=0;i<6;i++)Box("Radio_Case_Vent",new Vector3(-.99f+i*.013f,.9693f,-.18f),new Vector3(.004f,.001f,.070f),black).transform.SetParent(radio,true);
        NF5KEquipmentPrint.Add(radio,"Radio_Bilingual_Face","Cab_Radio_Face_V1",new Vector3(0,0,.0945f),new Vector2(.26f,.105f));
        var knobs=radio.GetComponentsInChildren<Transform>().Where(t=>t.name=="Radio_Control_Knob").OrderBy(t=>t.position.x).ToArray();
        var oldIndexes=radio.GetComponentsInChildren<Transform>().Where(t=>t.name=="Radio_Knob_Index").OrderBy(t=>t.position.x).ToArray();
        for(int i=0;i<knobs.Length;i++)oldIndexes[i].SetParent(knobs[i],true);
        var hit=radio.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(.29f,.13f,.18f);hit.center=new Vector3(0,0,.01f);
        Material Lamp(string name,Color color)
        {
            string path="Assets/Resources/Cab/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.28f);EditorUtility.SetDirty(material);return material;
        }
        var off=Lamp("Crew_Radio_Lamp_Off",new Color(.045f,.075f,.035f));var on=Lamp("Crew_Radio_Lamp_On",new Color(.38f,.68f,.20f));
        var lamp=Box("Radio_Power_Indicator",new Vector3(-1.095f,.943f,-.071f),new Vector3(.012f,.006f,.003f),off);lamp.transform.SetParent(radio,true);
        radio.gameObject.AddComponent<CabWorkstationEquipment>().Configure(CabWorkstationEquipment.EquipmentKind.Radio,profile.radioText,knobs[0],knobs[1],lamp.GetComponent<Renderer>(),off,on);

        var clipboard=new GameObject("Crew_Check_Record").transform;clipboard.gameObject.layer=30;clipboard.SetParent(root,false);clipboard.localPosition=new Vector3(-.58f,.843f,.12f);
        foreach(var t in root.Cast<Transform>().Where(t=>t.name.StartsWith("Timetable_")).ToArray())t.SetParent(clipboard,true);
        foreach(var t in clipboard.GetComponentsInChildren<Transform>().Where(t=>t.name=="Timetable_Print_Rule").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        Find("Timetable_Blank_Insert").localScale=new Vector3(.18f,.0046f,.23f);
        NF5KEquipmentPrint.Add(clipboard,"Crew_Record_Print","Cab_Crew_Record_V1",new Vector3(0,.008f,0),new Vector2(.178f,.228f));
        clipboard.Find("Crew_Record_Print").localRotation=Quaternion.Euler(-90,0,0);
        var recordMaterial=clipboard.Find("Crew_Record_Print").GetComponent<Renderer>().sharedMaterial;
        recordMaterial.SetFloat("_SpecularHighlights",0);recordMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        recordMaterial.SetFloat("_EnvironmentReflections",0);recordMaterial.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        EditorUtility.SetDirty(recordMaterial);
        var recordHit=clipboard.gameObject.AddComponent<BoxCollider>();recordHit.size=new Vector3(.20f,.018f,.25f);recordHit.center=new Vector3(0,.004f,0);
        clipboard.gameObject.AddComponent<CabWorkstationEquipment>().Configure(CabWorkstationEquipment.EquipmentKind.Documents,profile.recordText);
        // Discreet, deterministic wear on the actual board edge, not random object displacement.
        foreach(float z in new[]{.04f,.071f})Box("Clipboard_Edge_Scuff",new Vector3(-.481f,.845f,z),new Vector3(.0015f,.002f,.009f),paper).transform.SetParent(clipboard,true);
        var handset=Find("Handset_Cradle");
        foreach(float z in new[]{-.20f,-.06f})
        {
            Box("Handset_Retaining_Cup",new Vector3(-1.18f,.8845f,z),new Vector3(.065f,.006f,.056f),black);
            foreach(float x in new[]{-1.21f,-1.15f})Box("Handset_Cradle_Side_Stop",new Vector3(x,.894f,z),new Vector3(.006f,.023f,.056f),steel);
        }
        Box("Handset_Cable_Strain_Relief",new Vector3(-1.18f,.892f,-.045f),new Vector3(.014f,.025f,.015f),black);
        Box("Handset_Fixed_Lead_Clamp",new Vector3(-1.14f,.883f,-.15f),new Vector3(.018f,.018f,.018f),steel);
        Debug.Log("NF5K_COMMUNICATION_DETAIL_BUILT: front bearer/cable trunk, restrained handset, labelled radio and readable crew record; no dispatch transmission");
    }
}
