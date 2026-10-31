using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KWallEquipment
{
    [System.Serializable] sealed class PrintPack { public string id; }
    [System.Serializable] sealed class PrintProfile { public PrintPack[] packs; }
    public static void Build(Transform fittings,Transform body)
    {
        var enamel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Service_Satin_Enamel.mat");
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Service_Fitting_Steel.mat");
        var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Service_Hose_Rubber.mat");
        var paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Service_Paper.mat");
        Material Finish(string name,Color color,float metal,float smooth)
        {
            string path="Assets/Resources/Cab/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        var profile=JsonUtility.FromJson<PrintProfile>(Resources.Load<TextAsset>("Cab/first_aid_label_profile").text);
        if(profile.packs.Length!=3)throw new System.InvalidOperationException("Expected three first-aid label designs");
        var green=Finish("Cab_First_Aid_Green",new Color(.085f,.26f,.18f),.15f,.24f);
        var cream=Finish("Cab_First_Aid_Packs",new Color(.47f,.51f,.45f),0,.12f);
        GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;
            g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }
        void Text(Transform parent,string name,string text,Vector3 p,float size,Quaternion rotation,Color? color=null)
        {
            var g=new GameObject(name);g.layer=30;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localRotation=rotation;
            var t=g.AddComponent<TextMesh>();t.text=text;t.font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color??new Color(.88f,.86f,.73f);
            g.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_World_Label.mat");g.AddComponent<CabWorldLabel>();
        }
        var cabinet=new GameObject("Passenger_First_Aid_Cabinet").transform;cabinet.SetParent(fittings,false);cabinet.localPosition=new Vector3(-.90f,1.43f,1.998f);
        cabinet.gameObject.layer=30;
        Box(cabinet,"Aid_Cabinet_Back",new Vector3(0,0,-.044f),new Vector3(.38f,.32f,.008f),enamel);
        foreach(float y in new[]{-.156f,.156f})Box(cabinet,"Aid_Cabinet_Folded_Return",new Vector3(0,y,0),new Vector3(.38f,.008f,.09f),enamel,true);
        foreach(float x in new[]{-.186f,.186f})Box(cabinet,"Aid_Cabinet_Side",new Vector3(x,0,0),new Vector3(.008f,.304f,.09f),enamel,true);
        foreach(float x in new[]{-.16f,.16f})foreach(float y in new[]{-.127f,.127f})
        {
            Box(cabinet,"Aid_Cabinet_Anchor_Washer",new Vector3(x,y,-.037f),new Vector3(.016f,.016f,.004f),steel);
            Box(cabinet,"Aid_Cabinet_Anchor_Head",new Vector3(x,y,-.034f),new Vector3(.009f,.009f,.004f),steel);
        }
        Box(cabinet,"Aid_Shelf",new Vector3(0,-.025f,0),new Vector3(.364f,.006f,.074f),enamel,true);
        foreach(float y in new[]{-.102f,.038f})foreach(float x in new[]{-.107f,0f,.107f})
        {
            Box(cabinet,"Aid_Sealed_Pack",new Vector3(x,y,-.001f),new Vector3(.094f,y<0?.10f:.12f,.058f),cream);
            Box(cabinet,"Aid_Pack_Seam",new Vector3(x,y+.038f,.029f),new Vector3(.085f,.003f,.001f),paper);
            Box(cabinet,"Aid_Pack_Label",new Vector3(x,y,.02865f),new Vector3(.078f,.066f,.001f),paper);
            var pack=profile.packs[x<-.05f?0:x>.05f?2:1];
            NF5KEquipmentPrint.Add(cabinet,"Aid_Pack_Identification",pack.id+"_Print_V2",new Vector3(x,y,.0293f),new Vector2(.078f,.066f));
            Box(cabinet,"Aid_Elastic_Retainer",new Vector3(x+.044f,y,.033f),new Vector3(.007f,y<0?.104f:.124f,.003f),dark);
        }
        // Retainer feet overlap the shelves/back returns so packs are not loose on a moving train.
        foreach(float x in new[]{-.107f,0f,.107f})foreach(float y in new[]{-.150f,-.046f,-.022f,.100f})
            Box(cabinet,"Aid_Retainer_Foot",new Vector3(x+.044f,y,.014f),new Vector3(.012f,.007f,.042f),steel);
        var pivot=new GameObject("Aid_Door_Hinge").transform;pivot.SetParent(cabinet,false);pivot.localPosition=new Vector3(-.183f,0,.047f);
        var door=Box(pivot,"Aid_Cabinet_Door",new Vector3(.183f,0,0),new Vector3(.374f,.314f,.008f),enamel,true);
        foreach(float x in new[]{.007f,.359f})Box(pivot,"Aid_Door_Gasket_V",new Vector3(x,0,-.006f),new Vector3(.004f,.294f,.004f),dark);
        foreach(float y in new[]{-.145f,.145f})Box(pivot,"Aid_Door_Gasket_H",new Vector3(.183f,y,-.006f),new Vector3(.348f,.004f,.004f),dark);
        Box(pivot,"Aid_Green_Identity_Field",new Vector3(.183f,.034f,.0048f),new Vector3(.22f,.17f,.002f),green);
        NF5KEquipmentPrint.Add(pivot,"Aid_Cabinet_Identity_Print","First_Aid_Cabinet_Print_V3",new Vector3(.183f,.034f,.0062f),new Vector2(.22f,.17f));
        var latch=new GameObject("Aid_Quarter_Turn_Latch").transform;latch.SetParent(pivot,false);latch.localPosition=new Vector3(.333f,-.031f,.008f);
        Box(latch,"Aid_Latch_Handle",Vector3.zero,new Vector3(.018f,.034f,.010f),steel);
        Box(latch,"Aid_Latch_Recess",new Vector3(0,0,.006f),new Vector3(.003f,.017f,.002f),dark);
        Box(latch,"Aid_Latch_Spindle",new Vector3(0,0,-.006f),new Vector3(.008f,.008f,.023f),steel);
        Box(latch,"Aid_Latch_Tongue",new Vector3(.018f,0,-.015f),new Vector3(.042f,.010f,.003f),steel);
        Box(cabinet,"Aid_Fixed_Catch",new Vector3(.177f,-.031f,.049f),new Vector3(.018f,.050f,.014f),steel);
        foreach(float x in new[]{-.146f,.087f})
            Box(cabinet,"Aid_Lower_Edge_Service_Scuff",new Vector3(x,-.1597f,.025f),new Vector3(.013f,.0008f,.001f),steel);
        foreach(float y in new[]{-.112f,.112f})
        {
            var hinge=GameObject.CreatePrimitive(PrimitiveType.Cylinder);hinge.name="Aid_Fixed_Hinge_Barrel";hinge.layer=30;
            hinge.transform.SetParent(cabinet,false);hinge.transform.localPosition=new Vector3(-.183f,y,.047f);hinge.transform.localScale=new Vector3(.012f,.023f,.012f);hinge.GetComponent<Renderer>().sharedMaterial=steel;Object.DestroyImmediate(hinge.GetComponent<Collider>());
            Box(cabinet,"Aid_Fixed_Hinge_Leaf",new Vector3(-.184f,y,.027f),new Vector3(.009f,.045f,.044f),steel);
            Box(pivot,"Aid_Moving_Hinge_Leaf",new Vector3(.012f,y,0),new Vector3(.030f,.045f,.008f),steel);
        }
        cabinet.gameObject.AddComponent<CabStorageCabinet>().Configure(pivot,latch);

        // Cab-facing cover: preserve the existing hatch, add depth and hardware without implying powered circuitry.
        Transform hatch=null;foreach(var t in body.GetComponentsInChildren<Transform>())if(t.name=="Rear_Electrical_Service_Panel"){hatch=t;break;}
        if(hatch!=null)
        {
            var panel=new GameObject("Cab_Rear_Electrical_Cover_Detail").transform;panel.SetParent(fittings,false);panel.localPosition=new Vector3(.85f,1.48f,1.83f);
            Box(panel,"Electrical_Recessed_Gasket",Vector3.zero,new Vector3(.61f,.31f,.005f),dark);
            Box(panel,"Electrical_Folded_Cover",new Vector3(0,0,-.006f),new Vector3(.585f,.285f,.009f),enamel);
            foreach(float x in new[]{-.273f,.273f})foreach(float y in new[]{-.119f,.119f})
            {
                Box(panel,"Electrical_Captive_Fastener",new Vector3(x,y,-.012f),new Vector3(.012f,.012f,.004f),steel);
                Box(panel,"Electrical_Fastener_Slot",new Vector3(x,y,-.0144f),new Vector3(.007f,.002f,.001f),dark);
            }
            for(int i=0;i<6;i++)
            {
                Box(panel,"Electrical_Vent_Shadow",new Vector3(-.12f,.07f-i*.022f,-.011f),new Vector3(.19f,.009f,.002f),dark);
                var lip=Box(panel,"Electrical_Vent_Hood",new Vector3(-.12f,.074f-i*.022f,-.015f),new Vector3(.198f,.009f,.010f),enamel);
                lip.transform.localRotation=Quaternion.Euler(-15,0,0);
            }
            Text(panel,"Electrical_Panel_Legend","辅助电器\nAUXILIARY ELECTRICS",new Vector3(.105f,.035f,-.0115f),.0022f,Quaternion.identity);
            Text(panel,"Electrical_Panel_ID","EC-01",new Vector3(.105f,-.068f,-.0115f),.0022f,Quaternion.identity);
        }
        Debug.Log("NF5K_WALL_EQUIPMENT_BUILT: rear electrical cover and hinged first-aid storage; no medical or electrical backend");
    }
}
