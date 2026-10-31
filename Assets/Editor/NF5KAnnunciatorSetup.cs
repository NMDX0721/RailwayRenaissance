using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KAnnunciatorSetup
{
    [Serializable] sealed class Catalog {public string[] zh,en,color;}
    public static void Build(Transform fittings,Transform body)
    {
        var source=body.GetComponentsInChildren<MeshFilter>().Single(t=>t.name=="Auxiliary_Control_Plate");
        var controls=fittings.Find("NF5K_Exterior_Light_Control_Panel");
        foreach(var r in body.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Unbound_Warning_Lens_")||r.name.StartsWith("Warning_Legend_")))r.enabled=false;
        var root=new GameObject("NF5K_Ten_Window_Annunciator").transform;root.SetParent(fittings,false);
        root.SetPositionAndRotation(source.transform.position+controls.up*.049f+controls.forward*.00425f,controls.rotation);
        var frame=Material("Cab_Annunciator_Frame",new Color(.055f,.063f,.061f),.45f,.23f);
        var seal=Material("Cab_Annunciator_Gasket",new Color(.013f,.016f,.016f),0,.08f);
        var rim=Material("Cab_Annunciator_Rim",new Color(.20f,.22f,.21f),.8f,.36f);
        var diffuser=Material("Cab_Annunciator_Diffuser_V2",new Color(.06f,.025f,.014f),0,.40f);EnableEmission(diffuser);
        foreach(string map in new[]{"Frost","Diffusion","MicroNormal"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath($"Assets/Resources/Cab/Cab_Annunciator_{map}_V2.png");
            importer.userData="CabAnnunciatorOpticalMap";
            importer.textureType=map=="MicroNormal"?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=map=="Frost";importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
        diffuser.SetTexture("_BaseMap",Resources.Load<Texture2D>("Cab/Cab_Annunciator_Frost_V2"));
        diffuser.SetTexture("_EmissionMap",Resources.Load<Texture2D>("Cab/Cab_Annunciator_Diffusion_V2"));
        diffuser.SetTexture("_BumpMap",Resources.Load<Texture2D>("Cab/Cab_Annunciator_MicroNormal_V2"));diffuser.SetFloat("_BumpScale",.30f);diffuser.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(diffuser);
        GameObject Box(string name,Vector3 p,Vector3 size,Material m)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;return g;
        }
        Box("Annunciator_Seated_Backplate",Vector3.zero,new Vector3(.370f,.086f,.0025f),frame);
        var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Cab/annunciator_profile").text);
        var lenses=new Renderer[10];var labels=new Renderer[10];var colors=new Color[10];
        for(int i=0;i<10;i++)
        {
            float x=.142f-(i%5)*.071f,y=i<5?.020f:-.020f;
            Box("Annunciator_Seal_"+i,new Vector3(x,y,.002f),new Vector3(.066f,.035f,.002f),seal);
            foreach(float side in new[]{-1f,1f})
            {
                Box("Annunciator_Rim_Side_"+i+"_"+side,new Vector3(x+side*.03075f,y,.0045f),new Vector3(.0025f,.028f,.004f),rim);
                Box("Annunciator_Rim_Rail_"+i+"_"+side,new Vector3(x,y+side*.01525f,.0045f),new Vector3(.064f,.0025f,.004f),rim);
            }
            Box("Annunciator_Recess_"+i,new Vector3(x,y,.004f),new Vector3(.062f,.031f,.002f),seal);
            Box("Annunciator_Smoked_Pane_"+i,new Vector3(x,y,.005f),new Vector3(.059f,.028f,.0012f),seal);
            string optical="Annunciator_Optical_Face_"+i;
            NF5KEquipmentPrint.Add(root,optical,"Cab_Annunciator_Frost_V2",new Vector3(x,y,.00562f),new Vector2(.059f,.028f));
            lenses[i]=root.Find(optical).GetComponent<Renderer>();lenses[i].sharedMaterial=diffuser;
            var opticalMesh=lenses[i].GetComponent<MeshFilter>().sharedMesh;opticalMesh.RecalculateTangents();EditorUtility.SetDirty(opticalMesh);
            string label="Annunciator_Legend_"+i;
            NF5KEquipmentPrint.Add(root,label,$"Cab_Annunciator_Legend_{i:00}",new Vector3(x,y,.00585f),new Vector2(.057f,.026f));
            labels[i]=root.Find(label).GetComponent<Renderer>();var m=labels[i].sharedMaterial;NF5KLightingKeysSetup.ConfigureInk(m);
            EnableEmission(m);m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));EditorUtility.SetDirty(m);
            labels[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;labels[i].receiveShadows=false;
            colors[i]=catalog.color[i]=="red"?new Color(1,.055f,.025f):catalog.color[i]=="green"?new Color(.12f,.8f,.30f):catalog.color[i]=="white"?new Color(.75f,.84f,1):new Color(1,.52f,.045f);
        }
        var screwMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset");
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Satin_Steel.mat");var slot=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Fastener_Recess_V4.mat");
        foreach(float x in new[]{-.177f,.177f})foreach(float y in new[]{-.034f,.034f})
        {
            var g=new GameObject("Annunciator_Captive_Screw");g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=new Vector3(x,y,.0014f);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,Vector3.forward);g.transform.localScale=new Vector3(.0045f,.0015f,.0045f);
            g.AddComponent<MeshFilter>().sharedMesh=screwMesh;g.AddComponent<MeshRenderer>().sharedMaterials=new[]{steel,slot};
        }
        var panel=root.gameObject.AddComponent<CabAnnunciatorPanel>();panel.Configure(body.GetComponentInChildren<CabEngineState>(),lenses,labels,colors);
        var button=new GameObject("Annunciator_Lamp_Test_Button").transform;button.SetParent(root,false);button.localPosition=new Vector3(-.199f,-.009f,.0013f);button.gameObject.layer=30;
        var cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Circulator_Smooth_Cylinder_V3.asset");
        GameObject Round(string name,Vector3 p,Vector3 scale,Material material)
        {
            var g=new GameObject(name);g.layer=30;g.transform.SetParent(button,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(90,0,0);g.transform.localScale=scale;
            g.AddComponent<MeshFilter>().sharedMesh=cylinder;g.AddComponent<MeshRenderer>().sharedMaterial=material;return g;
        }
        Round("Lamp_Test_Threaded_Bezel",Vector3.forward*.0015f,new Vector3(.022f,.0015f,.022f),steel);
        var cap=Round("Lamp_Test_Spring_Cap",Vector3.forward*.0046f,new Vector3(.017f,.0018f,.017f),Material("Cab_Annunciator_Test_Cap",new Color(.32f,.35f,.32f),.15f,.24f));
        var hit=button.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(.024f,.024f,.012f);hit.center=Vector3.forward*.004f;
        button.gameObject.AddComponent<CabLampTestButton>().Configure(panel,cap.transform);
        NF5KEquipmentPrint.Add(root,"Annunciator_Test_Label","Cab_Annunciator_Test_Label_V1",new Vector3(-.199f,.017f,.0014f),new Vector2(.029f,.021f));
        var testLabel=root.Find("Annunciator_Test_Label").GetComponent<Renderer>();NF5KLightingKeysSetup.ConfigureInk(testLabel.sharedMaterial);testLabel.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;testLabel.receiveShadows=false;
        Debug.Log("NF5K_ANNUNCIATOR_BUILT: 10 recessed bilingual windows, original four unbound lamps hidden");
    }
    public static void EnableEmission(Material material)
    {
        // URP validation derives its emission keyword from the material's emissive flag.
        // Runtime property blocks keep unselected panes dark; no GI update is requested here.
        material.SetColor("_EmissionColor",Color.black);material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);
    }
    static Material Material(string name,Color color,float metal,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
}
