using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KExtinguisherModel
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    static Material Material(string name,Color color,float metal,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    public static void Build(Transform parent)
    {
        var red=Material("Extinguisher_Powder_Coat",new Color(.54f,.035f,.025f),.24f,.30f);
        var metal=Material("Extinguisher_Valve_Alloy",new Color(.32f,.36f,.35f),.78f,.29f);
        var bezel=Material("Extinguisher_Bezel_Steel",new Color(.63f,.66f,.65f),.95f,.58f);
        var rubber=Material("Extinguisher_Hose_EPDM",new Color(.035f,.043f,.040f),0,.16f);
        var ivory=Material("Extinguisher_Gauge_Face",new Color(.85f,.84f,.72f),0,.18f);
        var green=Material("Extinguisher_Gauge_Green",new Color(.12f,.34f,.15f),0,.18f);
        var yellow=Material("Extinguisher_Tamper_Seal",new Color(.77f,.60f,.12f),0,.2f);
        GameObject Part(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material mat)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.layer=30;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        void Rod(string name,Vector3 a,Vector3 b,float r,Material mat)
        {
            var g=Part(name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(r*2,Vector3.Distance(a,b)*.5f,r*2),mat);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        }
        GameObject MeshPart(string name,Mesh mesh,Material mat)
        {
            var g=new GameObject(name);g.layer=30;g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
            return g;
        }
        void Strip(string name,Vector3 a,Vector3 b,Material mat)
        {
            var g=Part(name,PrimitiveType.Cube,(a+b)*.5f,new Vector3(Vector3.Distance(a,b),.004f,.022f),mat);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.right,(b-a).normalized);
        }
        var profile=new[]{new Vector2(0,.001f),new Vector2(.044f,0),new Vector2(.051f,.003f),new Vector2(.054f,.012f),new Vector2(.054f,.242f),new Vector2(.053f,.254f),new Vector2(.049f,.267f),new Vector2(.041f,.278f),new Vector2(.028f,.290f),new Vector2(.015f,.297f),new Vector2(.015f,.309f),new Vector2(0,.309f)};
        MeshPart("Extinguisher_Cylinder",Lathe("Extinguisher_Drawn_Steel_V2",profile),red);
        MeshPart("Extinguisher_Base_Chime",Lathe("Extinguisher_Base_Chime_V2",new[]{new Vector2(.051f,.001f),new Vector2(.055f,.002f),new Vector2(.055f,.009f),new Vector2(.054f,.012f)}),red);
        Part("Extinguisher_Valve_Body",PrimitiveType.Cylinder,new Vector3(0,.325f,0),new Vector3(.032f,.018f,.032f),metal);
        for(int i=0;i<3;i++)Part("Neck_Thread_Collar",PrimitiveType.Cylinder,new Vector3(0,.308f+i*.003f,0),new Vector3(.034f,.0007f,.034f),metal);
        // Separate fixed carrying handle and moving squeeze lever, with an open grip gap.
        Strip("Handle_Fixed_Stem",new Vector3(0,.335f,0),new Vector3(-.020f,.350f,0),metal);
        Strip("Handle_Fixed_Lower",new Vector3(-.020f,.350f,0),new Vector3(-.075f,.350f,0),metal);
        Strip("Handle_Return",new Vector3(-.075f,.350f,0),new Vector3(-.075f,.365f,0),metal);
        Strip("Squeeze_Lever",new Vector3(.015f,.363f,0),new Vector3(-.078f,.382f,0),red);
        Strip("Squeeze_Lever_Hinge_Web",new Vector3(.009f,.351f,0),new Vector3(.015f,.363f,0),red);
        Rod("Lever_Hinge_Pin",new Vector3(.009f,.351f,-.012f),new Vector3(.009f,.351f,.012f),.003f,metal);
        Rod("Safety_Pin_Shaft",new Vector3(.016f,.348f,-.012f),new Vector3(.016f,.348f,.025f),.0015f,bezel);
        for(int i=0;i<24;i++)
        {
            float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
            Rod("Safety_Pin_Ring",new Vector3(.026f+.009f*Mathf.Cos(a),.348f+.009f*Mathf.Sin(a),.025f),new Vector3(.026f+.009f*Mathf.Cos(b),.348f+.009f*Mathf.Sin(b),.025f),.0012f,bezel);
        }
        Rod("Tamper_Seal_Tether",new Vector3(.025f,.345f,.025f),new Vector3(.04f,.326f,.024f),.0008f,yellow);
        Part("Tamper_Seal_Tag",PrimitiveType.Cube,new Vector3(.04f,.323f,.024f),new Vector3(.009f,.013f,.0015f),yellow);
        Rod("Pressure_Gauge_Stem",new Vector3(0,.327f,.01f),new Vector3(0,.327f,.027f),.005f,metal);
        var rim=MeshPart("Pressure_Gauge_Rolled_Bezel",Lathe("Extinguisher_Gauge_Bezel_V3",new[]{new Vector2(.0155f,0),new Vector2(.018f,0),new Vector2(.0185f,.001f),new Vector2(.0185f,.004f),new Vector2(.0175f,.005f),new Vector2(.0155f,.005f)}),bezel);
        rim.transform.localPosition=new Vector3(0,.327f,.030f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
        var dialMaterial=Material("Extinguisher_Gauge_Print_V3",Color.white,0,.15f);
        dialMaterial.SetTexture("_BaseMap",Print("Extinguisher_Baiyue_Gauge_V3.png",1024));EditorUtility.SetDirty(dialMaterial);
        string dialPath=Folder+"Extinguisher_Gauge_Disc_V3.asset";var disc=AssetDatabase.LoadAssetAtPath<Mesh>(dialPath);
        if(disc==null)
        {
            var v=new List<Vector3>{new Vector3(0,.327f,.0348f)};var uv=new List<Vector2>{new Vector2(.5f,.5f)};var t=new List<int>();
            for(int i=0;i<=96;i++){float a=i*Mathf.PI/48,x=Mathf.Cos(a),y=Mathf.Sin(a);v.Add(new Vector3(x*.015f,.327f+y*.015f,.0348f));uv.Add(new Vector2(.5f-x*.5f,.5f+y*.5f));if(i<96)t.AddRange(new[]{0,i+1,i+2});}
            disc=new Mesh{name="Printed_Gauge_Face"};disc.SetVertices(v);disc.SetUVs(0,uv);disc.SetTriangles(t,0);disc.RecalculateNormals();disc.RecalculateBounds();AssetDatabase.CreateAsset(disc,dialPath);
        }
        MeshPart("Pressure_Gauge_Printed_Face",disc,dialMaterial);
        string needlePath=Folder+"Extinguisher_Gauge_Needle_V3.asset";var needle=AssetDatabase.LoadAssetAtPath<Mesh>(needlePath);
        if(needle==null){needle=new Mesh{name="Tapered_Pressure_Pointer"};needle.vertices=new[]{new Vector3(-.0011f,.325f,.0353f),new Vector3(.0011f,.325f,.0353f),new Vector3(0,.339f,.0353f)};needle.triangles=new[]{0,1,2};needle.RecalculateNormals();needle.RecalculateBounds();AssetDatabase.CreateAsset(needle,needlePath);}
        MeshPart("Pressure_Gauge_Tapered_Pointer",needle,rubber);
        Part("Pressure_Gauge_Pointer_Hub",PrimitiveType.Sphere,new Vector3(0,.327f,.0354f),new Vector3(.003f,.003f,.0008f),bezel);
        var glass=Material("Extinguisher_Gauge_Glass",new Color(.72f,.84f,.82f,.075f),0,.88f);
        glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(glass);
        Part("Pressure_Gauge_Convex_Lens",PrimitiveType.Sphere,new Vector3(0,.327f,.0366f),new Vector3(.031f,.031f,.002f),glass);
        Rod("Hose_Union",new Vector3(.012f,.327f,0),new Vector3(.035f,.327f,0),.006f,metal);
        Vector3 A=new Vector3(.035f,.327f,0),B=new Vector3(.13f,.325f,0),C=new Vector3(.09f,.18f,0),D=new Vector3(.077f,.10f,0);
        Vector3 Point(float t){float s=1-t;return s*s*s*A+3*s*s*t*B+3*s*t*t*C+t*t*t*D;}
        for(int i=0;i<32;i++){Rod("Curved_Discharge_Hose",Point(i/32f),Point((i+1)/32f),.0046f,rubber);Part("Hose_Continuous_Joint",PrimitiveType.Sphere,Point(i/32f),Vector3.one*.0092f,rubber);}
        Rod("Discharge_Nozzle",D,new Vector3(.073f,.068f,0),.007f,rubber);
        Rod("Nozzle_Outlet_Rim",new Vector3(.073f,.068f,0),new Vector3(.0725f,.064f,0),.0075f,metal);
        Part("Nozzle_Outlet_Dark_Bore",PrimitiveType.Sphere,new Vector3(.0725f,.063f,0),new Vector3(.010f,.003f,.010f),rubber);
        Rod("Nozzle_Storage_Clip",new Vector3(.050f,.10f,0),new Vector3(.079f,.10f,0),.003f,metal);
        string texturePath="Assets/Resources/Cab/Extinguisher_Baiyue_Bilingual_V3.png";
        var importer=AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Generate extinguisher reference label first");
        importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        var label=Material("Extinguisher_Printed_Wrap",Color.white,0,.18f);label.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(label);
        MeshPart("Extinguisher_Curved_Instruction_Label",Patch(),label);
    }
    public static Transform BuildBand(Transform parent,Vector3 center,float face)
    {
        string path=Folder+"Extinguisher_Hinged_Band_V2.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<=64;i++)
            {
                float a=Mathf.Lerp(-2.5f,2.5f,i/64f);
                foreach(var p in new[]{new Vector2(.057f,-.009f),new Vector2(.0586f,-.009f),new Vector2(.0586f,.009f),new Vector2(.057f,.009f)})
                    v.Add(new Vector3(p.x*Mathf.Sin(a),p.y,p.x*Mathf.Cos(a)));
                if(i==64)continue;for(int j=0;j<4;j++){int aa=i*4+j,b=i*4+(j+1)%4;t.AddRange(new[]{aa,b,b+4,aa,b+4,aa+4});}
            }
            mesh=new Mesh{name="Hinged_Vehicle_Retaining_Band"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        var rotation=Quaternion.Euler(0,face>0?0:180,0);
        var pivot=new GameObject("Extinguisher_Band_Hinge").transform;pivot.SetParent(parent,false);pivot.localPosition=center+rotation*(Vector3.left*.058f);pivot.localRotation=rotation;pivot.gameObject.layer=30;
        var band=new GameObject("Extinguisher_Releasable_Retaining_Band");band.transform.SetParent(pivot,false);band.transform.localPosition=Vector3.right*.058f;band.layer=30;
        band.AddComponent<MeshFilter>().sharedMesh=mesh;band.AddComponent<MeshRenderer>().sharedMaterial=Material("Extinguisher_Band_Spring_Steel",new Color(.17f,.20f,.18f),.72f,.26f);
        var clasp=GameObject.CreatePrimitive(PrimitiveType.Cube);clasp.name="Over_Centre_Band_Clasp";clasp.transform.SetParent(pivot,false);clasp.transform.localPosition=new Vector3(.11f,0,.01f);clasp.transform.localScale=new Vector3(.018f,.025f,.012f);clasp.layer=30;clasp.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Extinguisher_Valve_Alloy.mat");UnityEngine.Object.DestroyImmediate(clasp.GetComponent<Collider>());
        return pivot;
    }
    static Texture2D Print(string name,int maxSize)
    {
        string path="Assets/Resources/Cab/"+name;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Missing print: "+path);
        importer.maxTextureSize=maxSize;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static void BuildManual(Transform parent)
    {
        var root=new GameObject("Fire_Equipment_Manual_Pocket");root.layer=30;root.transform.SetParent(parent,false);root.transform.localPosition=new Vector3(.95f,.83f,2.00f);
        var enamel=Material("Fire_Manual_Holder_Enamel",new Color(.29f,.38f,.33f),.16f,.23f);
        var paper=Material("Fire_Manual_Page_Edges",new Color(.74f,.71f,.60f),0,.12f);
        var cover=Material("Fire_Manual_Cloth_Cover",new Color(.10f,.20f,.16f),0,.18f);
        void Box(string name,Vector3 p,Vector3 size,Material mat)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(root.transform,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        Box("Manual_Pocket_Back",new Vector3(0,-.02f,-.012f),new Vector3(.18f,.26f,.012f),enamel);
        Box("Manual_Bound_Page_Block",new Vector3(0,.025f,.012f),new Vector3(.148f,.227f,.014f),paper);
        Box("Manual_Back_Cover",new Vector3(0,.025f,.003f),new Vector3(.154f,.234f,.003f),cover);
        Box("Manual_Binding_Spine",new Vector3(.076f,.025f,.012f),new Vector3(.008f,.234f,.02f),cover);
        Box("Manual_Pocket_Front_Lip",new Vector3(0,-.075f,.035f),new Vector3(.18f,.11f,.012f),enamel);
        foreach(float x in new[]{-.086f,.086f})Box("Manual_Pocket_Return",new Vector3(x,-.02f,.012f),new Vector3(.008f,.26f,.05f),enamel);
        foreach(float y in new[]{-.10f,.07f})Box("Manual_Wall_StandOff",new Vector3(0,y,-.030f),new Vector3(.12f,.025f,.04f),enamel);
        var print=Material("Fire_Manual_Printed_Cover",Color.white,0,.18f);print.SetTexture("_BaseMap",Print("Baiyue_Fire_Manual_Cover.png",2048));EditorUtility.SetDirty(print);
        string path=Folder+"Baiyue_Manual_Front_Cover.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            mesh=new Mesh{name="Bilingual_Manual_Front"};mesh.vertices=new[]{new Vector3(.077f,-.092f,.021f),new Vector3(-.077f,-.092f,.021f),new Vector3(.077f,.142f,.021f),new Vector3(-.077f,.142f,.021f)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        var front=new GameObject("Manual_Bilingual_Front_Cover");front.layer=30;front.transform.SetParent(root.transform,false);front.AddComponent<MeshFilter>().sharedMesh=mesh;front.AddComponent<MeshRenderer>().sharedMaterial=print;
        var hit=root.AddComponent<BoxCollider>();hit.center=new Vector3(0,.015f,.02f);hit.size=new Vector3(.18f,.28f,.05f);
        var target=root.AddComponent<RailwayRenaissance.Cab.CabSafetyEquipment>();target.Configure(null,false);target.ConfigureManual();
    }
    static Mesh Lathe(string name,Vector2[] profile)
    {
        string path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
        const int sides=128;var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int j=0;j<profile.Length;j++)for(int i=0;i<=sides;i++)
        {
            float a=i*Mathf.PI*2/sides;v.Add(new Vector3(profile[j].x*Mathf.Sin(a),profile[j].y,profile[j].x*Mathf.Cos(a)));uv.Add(new Vector2(i/(float)sides,j/(float)(profile.Length-1)));
            if(j==profile.Length-1||i==sides)continue;int k=j*(sides+1)+i;tris.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh Patch()
    {
        string path=Folder+"Extinguisher_Conformal_Label_V2.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int y=0;y<2;y++)for(int i=0;i<=64;i++)
        {
            float t=i/64f,a=Mathf.Lerp(-1.10f,1.10f,t);v.Add(new Vector3(.05425f*Mathf.Sin(a),.025f+y*.21f,.05425f*Mathf.Cos(a)));uv.Add(new Vector2(1-t,y));
            if(y!=0||i==64)continue;tris.AddRange(new[]{i,i+1,i+65,i+1,i+66,i+65});
        }
        mesh=new Mesh{name="Conformal_Print_Not_Plate"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
