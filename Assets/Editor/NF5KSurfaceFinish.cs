using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class NF5KSurfaceFinish
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes";
    public static void Build(Transform root)
    {
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var mesh=filter.sharedMesh;
            if(mesh==null||!AssetDatabase.GetAssetPath(mesh).StartsWith(Folder)||mesh.uv.Length==mesh.vertexCount)continue;
            var v=mesh.vertices;var n=mesh.normals;var uv=new Vector2[v.Length];
            for(int i=0;i<v.Length;i++)
                uv[i]=Mathf.Abs(n[i].y)>.6f?new Vector2(v[i].x,v[i].z)*3:new Vector2(v[i].z+v[i].x*.7f,v[i].y)*3;
            mesh.uv=uv;EditorUtility.SetDirty(mesh);
        }
        Apply("Interior_NF5_Blue_Exterior",.05f,.31f,false);
        Apply("Interior_NF5_Cream_Exterior",.02f,.27f,false);
        Apply("Interior_NF5_Red_Stripe",.03f,.29f,false);
        Apply("Interior_NF5_Hardware",.80f,.39f,true);
        Apply("Bogie_Cast_Steel",.65f,.20f,true);
        Apply("Bogie_Spring_Steel",.72f,.29f,true);
        Apply("Wheel_Oxidized_Web",.65f,.18f,true);
        Apply("Wheel_Machined_Tread",.91f,.38f,true);
        var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_Blockout_Glass.mat");
        glass.SetColor("_BaseColor",new Color(.47f,.66f,.73f,.14f));glass.SetFloat("_Smoothness",.94f);
        glass.SetFloat("_Metallic",0);EditorUtility.SetDirty(glass);
        AddFasteners(root);
        foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=root.gameObject.layer;
        EnvironmentReflection();
        Debug.Log("NF5K_SURFACE_FINISH_OK: persisted UVs, restrained roughness variation, hardware fasteners, soft environment reflection");
    }
    static void Apply(string name,float metallic,float smooth,bool metal)
    {
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+name+".mat");
        string path=Folder+"/"+(metal?"Metal":"Paint")+"_Microfinish.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture==null)
        {
            const int size=256;
            texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name=metal?"Metal_Microfinish":"Paint_Microfinish",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float grain=Mathf.PerlinNoise(x*.41f,y*.41f),slow=Mathf.PerlinNoise(x*.027f,y*.027f);
                float brushed=metal?Mathf.Sin(y*3.2f+x*.02f)*.018f:0;
                float level=.94f+grain*.045f+slow*.015f+brushed;
                pixels[y*size+x]=new Color(level,level,level,.93f+grain*.07f);
            }
            texture.SetPixels(pixels);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,path);
        }
        mat.SetTexture("_BaseMap",texture);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smooth);
        mat.SetFloat("_SmoothnessTextureChannel",1);mat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        mat.SetTextureScale("_BaseMap",new Vector2(metal?7:4,metal?7:4));EditorUtility.SetDirty(mat);
        string normalPath=Folder+"/Subtle_Surface_Normal.png";
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if(normal==null)
        {
            const int size=256;var pixels=new Color[size*size];
            var source=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(Mathf.PerlinNoise((x+1)*.25f,y*.25f)-Mathf.PerlinNoise((x-1)*.25f,y*.25f))*.10f;
                float dy=(Mathf.PerlinNoise(x*.25f,(y+1)*.25f)-Mathf.PerlinNoise(x*.25f,(y-1)*.25f))*.10f;
                var n=new Vector3(-dx,-dy,1).normalized;pixels[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            source.SetPixels(pixels);source.Apply();File.WriteAllBytes(normalPath,source.EncodeToPNG());Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(normalPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(normalPath);importer.textureType=TextureImporterType.NormalMap;
            importer.sRGBTexture=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        }
        mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",metal?.35f:.20f);mat.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(mat);
    }
    static void AddFasteners(Transform parent)
    {
        var root=new GameObject("Exterior_Hardware_Detail").transform;root.SetParent(parent,false);
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_NF5_Hardware.mat");
        var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Bogie_Cast_Steel.mat");
        foreach(int side in new[]{-1,1})
        {
            foreach(float dx in new[]{-.18f,.18f})foreach(float dy in new[]{-.095f,.095f})
                Bolt("Lamp_Housing_Screw",new Vector3(side*.87f+dx,.72f+dy,-.720f),Quaternion.Euler(90,0,0),.018f,steel,root);
            foreach(float y in new[]{.30f,1.55f})foreach(float dy in new[]{-.04f,.04f})
                Bolt("Door_Hinge_Bolt",new Vector3(side*1.515f,y+dy,1.84f),Quaternion.Euler(0,0,90),.014f,steel,root);
            for(int i=0;i<3;i++)
            {
                var strip=GameObject.CreatePrimitive(PrimitiveType.Cube);strip.name="Worn_Step_Leading_Edge";strip.transform.SetParent(root,false);
                strip.transform.localPosition=new Vector3(side*(1.61f+.04f*i),-.215f-.24f*i,1.49f);
                strip.transform.localScale=new Vector3(.015f,.008f,.60f);strip.GetComponent<Renderer>().sharedMaterial=steel;
            }
            for(int i=0;i<8;i++)
                Bolt("Side_Sill_Fastener",new Vector3(side*1.456f,.09f,2.5f+i*1.8f),Quaternion.Euler(0,0,90),.015f,dark,root);
        }
        foreach(float x in new[]{-.25f,.25f})foreach(float y in new[]{.465f,.595f})
            Bolt("Number_Plate_Fastener",new Vector3(x,y,-.715f),Quaternion.Euler(90,0,0),.012f,steel,root);
    }
    static void Bolt(string name,Vector3 p,Quaternion rotation,float diameter,Material material,Transform parent)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=p;obj.transform.localRotation=rotation;obj.transform.localScale=new Vector3(diameter,.002f,diameter);
        obj.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    static void EnvironmentReflection()
    {
        const string path=Folder+"/Soft_Studio_Environment.asset";
        var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
        if(cube==null)
        {
            const int size=64;cube=new Cubemap(size,TextureFormat.RGBA32,true){name="Soft_Studio_Environment",filterMode=FilterMode.Trilinear};
            foreach(CubemapFace face in new[]{CubemapFace.PositiveX,CubemapFace.NegativeX,CubemapFace.PositiveY,CubemapFace.NegativeY,CubemapFace.PositiveZ,CubemapFace.NegativeZ})
            {
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;
                    Vector3 d=face==CubemapFace.PositiveX?new Vector3(1,-v,-u):face==CubemapFace.NegativeX?new Vector3(-1,-v,u):face==CubemapFace.PositiveY?new Vector3(u,1,v):face==CubemapFace.NegativeY?new Vector3(u,-1,-v):face==CubemapFace.PositiveZ?new Vector3(u,-v,1):new Vector3(-u,-v,-1);
                    d.Normalize();float sky=d.y*.5f+.5f;
                    pixels[y*size+x]=Color.Lerp(new Color(.14f,.15f,.16f),new Color(.60f,.66f,.71f),sky);
                }
                cube.SetPixels(pixels,face);
            }
            cube.Apply(true,false);AssetDatabase.CreateAsset(cube,path);
        }
        RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=cube;RenderSettings.reflectionIntensity=.65f;
    }
}
