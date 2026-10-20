using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KLivery
{
    static Transform root;
    static Material pink,pale,branch;
    public static void Build(Transform parent,Material blue,Material cream,Material red)
    {
        root=new GameObject("Reference_Livery").transform;root.SetParent(parent,false);
        pink=Mat("Livery_Sakura_Pink",new Color(.83f,.30f,.43f));
        pale=Mat("Livery_Sakura_Pale",new Color(.98f,.64f,.69f));
        branch=Mat("Livery_Branch",new Color(.32f,.13f,.16f));
        blue.SetFloat("_Metallic",.22f);blue.SetFloat("_Smoothness",.32f);EditorUtility.SetDirty(blue);
        cream.SetFloat("_Metallic",.10f);cream.SetFloat("_Smoothness",.27f);EditorUtility.SetDirty(cream);
        foreach(int side in new[]{-1,1})
        {
            for(int group=0;group<5;group++)
            {
                float center=2.7f+group*2.85f;
                for(int flower=0;flower<10;flower++)
                {
                    float z=center+Mathf.Sin(flower*2.1f+group)*.72f;
                    float y=.63f+Mathf.Sin(flower*1.7f+group)*.23f;
                    Flower(new Vector3(side*1.461f,y,z),side,.045f+(flower%3)*.015f);
                }
            }
            // The supplied illustration shows this flag; the opposite side is a symmetric design assumption.
            SideRect("Flag_White_Base",side,.76f,.39f,.80f,.34f,cream);
            SideRect("Flag_Red_Field",side,.76f,.39f,.80f,.22f,red,.003f);
            SideRect("Flag_Blue_Top",side,.91f,.39f,.80f,.035f,blue,.005f);
            SideRect("Flag_Blue_Bottom",side,.61f,.39f,.80f,.035f,blue,.005f);
            Disc("Flag_White_Disc",new Vector3(side*1.474f,.76f,.21f),.090f,cream,side);
            Star("Flag_Red_Star",new Vector3(side*1.477f,.76f,.21f),.072f,red,side);
            Text("Side_Number", "NF-5K",new Vector3(side*1.47f,.61f,2.25f),side>0?Quaternion.Euler(0,-90,0):Quaternion.Euler(0,90,0),.13f,cream);
            for(int i=0;i<10;i++)Flower(new Vector3(side*(.40f+i%3*.27f),.41f+i%4*.105f,-.663f),0,.045f);
            // Window rail and panel joints remain separate from decorative artwork.
            for(int i=0;i<7;i++)SideRect("Painted_Panel_Seam",side,.73f,3.8f+i*1.8f,.012f,.61f,branch);
        }
        Text("Front_Number_Text","NF-5K",new Vector3(0,.53f,-.710f),Quaternion.identity,.085f,cream);
        Debug.Log("NF5K_LIVERY_OK: geometric sakura, flags, lettering; no baked illustration pasted onto body");
    }
    static void Flower(Vector3 p,int side,float radius)
    {
        for(int i=0;i<5;i++)
        {
            float a=i*Mathf.PI*2/5;
            Vector3 offset=side==0?new Vector3(Mathf.Cos(a),Mathf.Sin(a),0):new Vector3(0,Mathf.Sin(a),Mathf.Cos(a));
            Disc("Sakura_Petal",p+offset*radius*.62f,radius*.49f,i%2==0?pink:pale,side);
        }
        Disc("Sakura_Centre",p+(side==0?Vector3.back:Vector3.right*side)*.001f,radius*.20f,pale,side);
    }
    static void SideRect(string name,int side,float y,float z,float width,float height,Material material,float extra=0)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);
        obj.transform.localPosition=new Vector3(side*(1.464f+extra),y,z);obj.transform.localScale=new Vector3(.003f,height,width);
        obj.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    static void Disc(string name,Vector3 position,float radius,Material material,int side)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(root,false);
        obj.transform.localPosition=position;obj.transform.localRotation=side==0?Quaternion.Euler(90,0,0):Quaternion.Euler(0,0,90);
        obj.transform.localScale=new Vector3(radius*2,.001f,radius*2);obj.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    static void Star(string name,Vector3 p,float radius,Material material,int side)
    {
        var points=new List<Vector3>{Vector3.zero};var triangles=new List<int>();
        for(int i=0;i<10;i++)
        {float a=(90+36*i)*Mathf.Deg2Rad,r=i%2==0?radius:radius*.43f;points.Add(new Vector3(0,Mathf.Sin(a)*r,Mathf.Cos(a)*r));}
        for(int i=0;i<10;i++){triangles.Add(0);triangles.Add((i+1)%10+1);triangles.Add(i+1);}
        string path="Assets/Models/Cab/NF5KBodyMeshes/Flag_Star.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.SetVertices(points);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.transform.localPosition=p;
        if(side<0)obj.transform.localRotation=Quaternion.Euler(0,180,0);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void Text(string name,string value,Vector3 p,Quaternion rotation,float size,Material color)
    {
        var v=new List<Vector3>();var t=new List<int>();
        var glyphs=new Dictionary<char,Vector2[][]>
        {
            ['N']=new[]{new[]{new Vector2(0,0),new Vector2(0,1)},new[]{new Vector2(0,1),new Vector2(.55f,0)},new[]{new Vector2(.55f,0),new Vector2(.55f,1)}},
            ['F']=new[]{new[]{new Vector2(0,0),new Vector2(0,1)},new[]{new Vector2(0,1),new Vector2(.55f,1)},new[]{new Vector2(0,.55f),new Vector2(.43f,.55f)}},
            ['-']=new[]{new[]{new Vector2(.04f,.5f),new Vector2(.49f,.5f)}},
            ['5']=new[]{new[]{new Vector2(.55f,1),new Vector2(0,1),new Vector2(0,.55f),new Vector2(.55f,.55f),new Vector2(.55f,0),new Vector2(0,0)}},
            ['K']=new[]{new[]{new Vector2(0,0),new Vector2(0,1)},new[]{new Vector2(.55f,1),new Vector2(0,.5f),new Vector2(.55f,0)}}
        };
        for(int c=0;c<value.Length;c++)foreach(var line in glyphs[value[c]])for(int i=0;i<line.Length-1;i++)
        {
            Vector2 a=line[i],b=line[i+1],n=new Vector2(-(b-a).y,(b-a).x).normalized*.035f;
            var offset=new Vector2(c*.75f-value.Length*.375f+.10f,-.5f);
            int k=v.Count;foreach(var point in new[]{a+n,a-n,b-n,b+n})v.Add((Vector3)(point+offset));
            t.Add(k);t.Add(k+2);t.Add(k+1);t.Add(k);t.Add(k+3);t.Add(k+2);
        }
        string path="Assets/Models/Cab/NF5KBodyMeshes/Number_Lettering.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);}
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);
        obj.transform.localPosition=p;obj.transform.localRotation=rotation;obj.transform.localScale=Vector3.one*size;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=color;
    }

    static Material Mat(string name,Color color)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.29f);EditorUtility.SetDirty(mat);return mat;
    }
}
