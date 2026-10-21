using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public static class NF5KIndustrialFinish
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes";
    public static void Build(Transform root)
    {
        var rubber=Material("Rubber_Window_Seals",new Color(.024f,.027f,.030f),0,.09f);
        var tread=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Wheel_Machined_Tread.mat");
        tread.SetColor("_BaseColor",new Color(.57f,.59f,.61f));tread.SetFloat("_Metallic",1);tread.SetFloat("_Smoothness",.50f);EditorUtility.SetDirty(tread);
        var hardware=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_NF5_Hardware.mat");
        hardware.SetColor("_BaseColor",new Color(.49f,.51f,.53f));EditorUtility.SetDirty(hardware);
        var markings=root.Find("Reference_Livery");
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(markings!=null&&renderer.transform.IsChildOf(markings))renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            string name=renderer.name;
            if(name.EndsWith("_Frame")&&renderer.sharedMaterial.name=="Interior_Blockout_Trim"||name.EndsWith("_Recess_Return")||name.EndsWith("_Inner_Finishing_Lip")||name.EndsWith("_Door_Header_Seal"))renderer.sharedMaterial=rubber;
            if(name.Contains("Door_Lower_Steel")||name.Contains("Door_Top_Rail")||name.Contains("Window_Stile")||name.Contains("Fixed_Door_")||name=="Axlebox"||name=="Bogie_Side_Frame"||name.StartsWith("Coupler_")||name=="Underfloor_Equipment_Cabinet"||name=="Cabinet_Service_Cover")BevelBox(renderer);
            bool roof=name=="Barrel_Roof";
            if(roof||name=="Blue_Belt_Shell"||name=="Lower_Blue_Shell"||name.Contains("Door_Lower_Steel"))Coating(renderer,root,roof);
        }
        var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_Blockout_Glass.mat");
        glass.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.One);
        glass.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_PreserveSpecularLighting",1);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        glass.SetColor("_BaseColor",new Color(.46f,.60f,.66f,.16f));EditorUtility.SetDirty(glass);
        LampOptics(root,glass);
        Debug.Log("NF5K_INDUSTRIAL_FINISH_OK: millimetre bevels, rubber separation, spatial coating variation and preserved glass reflections");
    }
    static void LampOptics(Transform root,Material glass)
    {
        const string path="Assets/Resources/Cab/Headlamp_Optical_Glass.mat";
        var lens=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(lens==null){lens=new Material(glass);AssetDatabase.CreateAsset(lens,path);}else lens.CopyPropertiesFromMaterial(glass);
        lens.SetColor("_BaseColor",new Color(.75f,.80f,.82f,.15f));EditorUtility.SetDirty(lens);
        var reflector=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_NF5_Hardware.mat");
        var bulb=Material("Headlamp_Unlit_Bulb",new Color(.64f,.51f,.30f),.35f,.40f);
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.name!="Roof_Headlamp_Lens"&&renderer.name!="Front_Lamp_Lens")continue;
            renderer.sharedMaterial=lens;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            float diameter=renderer.transform.lossyScale.x;
            Detail("Lamp_Reflector",diameter*.90f,.006f,reflector);
            Detail("Lamp_Unlit_Bulb",diameter*.18f,-.001f,bulb);
            void Detail(string name,float size,float zOffset,Material material)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.layer=renderer.gameObject.layer;
                obj.transform.SetParent(renderer.transform.parent,true);obj.transform.position=renderer.transform.position+Vector3.forward*zOffset;
                obj.transform.rotation=renderer.transform.rotation;obj.transform.localScale=new Vector3(size,.0015f,size);
                obj.GetComponent<Renderer>().sharedMaterial=material;obj.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Object.DestroyImmediate(obj.GetComponent<Collider>());
            }
        }
    }
    static void Coating(MeshRenderer renderer,Transform root,bool roof)
    {
        string id=roof?"Roof_Aged_Enamel":"Body_Painted_Steel";
        string path="Assets/Resources/Cab/"+id+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(mat,path);}else mat.CopyPropertiesFromMaterial(renderer.sharedMaterial);
        string texPath=Folder+"/"+id+"_Variation.asset";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if(tex==null)
        {
            const int width=512,height=256;tex=new Texture2D(width,height,TextureFormat.RGBA32,true,true){name=id+"_Variation",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var colors=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float v=y/(float)(height-1),fine=Mathf.PerlinNoise(x*.25f,y*.19f),broad=Mathf.PerlinNoise(x*.035f,y*.02f);
                float dirt=roof?.02f*Mathf.Pow(Mathf.Sin(v*Mathf.PI),2):.085f*Mathf.Pow(1-v,3);
                float streak=Mathf.PerlinNoise(x*.31f,y*.004f)*.025f;
                float color=.96f+fine*.025f-dirt-streak;
                colors[y*width+x]=new Color(color,color,color,.49f+broad*.28f);
            }
            tex.SetPixels(colors);tex.Apply(true,false);AssetDatabase.CreateAsset(tex,texPath);
        }
        mat.SetTexture("_BaseMap",tex);mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetTextureOffset("_BaseMap",Vector2.zero);
        mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",roof?.32f:.36f);EditorUtility.SetDirty(mat);
        var filter=renderer.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
        if(renderer.name.Contains("Door_Lower_Steel"))
        {
            string coatedPath=Folder+"/Coated_"+renderer.name+".asset";
            var coated=AssetDatabase.LoadAssetAtPath<Mesh>(coatedPath);
            if(coated==null){coated=Object.Instantiate(mesh);AssetDatabase.CreateAsset(coated,coatedPath);}else EditorUtility.CopySerialized(mesh,coated);
            mesh=coated;filter.sharedMesh=coated;
        }
        var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            var p=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
            uv[i]=new Vector2((p.z+p.x*.30f+1.5f)/20,roof?p.x/2.9f+.5f:Mathf.Clamp01(p.y/1.14f));
        }
        mesh.uv=uv;EditorUtility.SetDirty(mesh);renderer.sharedMaterial=mat;
    }
    static void BevelBox(MeshRenderer renderer)
    {
        var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh.name!="Cube")return;
        Vector3 size=renderer.transform.localScale;
        float radius=Mathf.Min(.004f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.12f);
        string key=size.x.ToString("R",CultureInfo.InvariantCulture)+"_"+size.y.ToString("R",CultureInfo.InvariantCulture)+"_"+size.z.ToString("R",CultureInfo.InvariantCulture);
        string path=Folder+"/Beveled_Box_"+Hash128.Compute(key)+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var v=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            Vector3 half=size/2,core=half-Vector3.one*radius;
            var directions=new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(var normal in directions)
            {
                Vector3 a=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up,b=Vector3.Cross(normal,a);
                float ha=Vector3.Dot(Abs(a),half),hb=Vector3.Dot(Abs(b),half),hn=Vector3.Dot(Abs(normal),half);
                float[] xs={-ha,-ha+radius,ha-radius,ha},ys={-hb,-hb+radius,hb-radius,hb};int start=v.Count;
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)
                {
                    Vector3 p=normal*hn+a*xs[x]+b*ys[y];
                    Vector3 q=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));
                    Vector3 n=(p-q).normalized;v.Add(q+n*radius);normals.Add(n);uv.Add(new Vector2(x/3f,y/3f));
                    if(x<3&&y<3){int k=start+y*4+x;t.Add(k);t.Add(k+1);t.Add(k+5);t.Add(k);t.Add(k+5);t.Add(k+4);}
                }
            }
            mesh=new Mesh{name="Industrial_Beveled_Box"};mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        filter.sharedMesh=mesh;renderer.transform.localScale=Vector3.one;
        var collider=renderer.GetComponent<BoxCollider>();if(collider!=null)collider.size=size;
    }
    static Vector3 Abs(Vector3 p)=>new Vector3(Mathf.Abs(p.x),Mathf.Abs(p.y),Mathf.Abs(p.z));
    static Material Material(string name,Color color,float metallic,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(mat);return mat;
    }
}
