using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KLivery
{
    static Transform root;
    static Material pink,pale,branch,pollen;
    public static void Build(Transform parent,Material blue,Material cream,Material red)
    {
        root=new GameObject("Reference_Livery").transform;root.SetParent(parent,false);
        pink=Mat("Livery_Sakura_Pink",new Color(.94f,.45f,.58f));
        pale=Mat("Livery_Sakura_Pale",new Color(.98f,.65f,.74f));
        pollen=Mat("Livery_Sakura_Pollen",new Color(.89f,.65f,.24f));
        branch=Mat("Livery_Branch",new Color(.32f,.13f,.16f));
        blue.SetFloat("_Metallic",.22f);blue.SetFloat("_Smoothness",.32f);EditorUtility.SetDirty(blue);
        cream.SetFloat("_Metallic",.10f);cream.SetFloat("_Smoothness",.27f);EditorUtility.SetDirty(cream);
        foreach(int side in new[]{-1,1})
        {
            foreach(float centre in new[]{3.3f,6.4f,10.8f,14.0f})
            {
                var path=new[]{new Vector2(centre-.75f,.43f),new Vector2(centre-.36f,.48f),new Vector2(centre+.04f,.61f),new Vector2(centre+.38f,.80f),new Vector2(centre+.65f,.89f)};
                Branch(path,side,.009f);
                Branch(new[]{path[1],new Vector2(centre-.16f,.75f),new Vector2(centre-.27f,.91f)},side,.005f);
                Branch(new[]{path[2],new Vector2(centre+.38f,.45f),new Vector2(centre+.64f,.44f)},side,.005f);
                var flowers=new[]{new Vector3(-.65f,.44f,.060f),new Vector3(-.29f,.88f,.073f),new Vector3(-.11f,.72f,.10f),new Vector3(.06f,.61f,.083f),new Vector3(.40f,.79f,.11f),new Vector3(.63f,.88f,.058f),new Vector3(.60f,.44f,.078f)};
                foreach(var f in flowers)Flower(new Vector3(side*1.468f,f.y,centre+f.x),side,f.z);
            }
            // The supplied illustration shows this flag; the opposite side is a symmetric design assumption.
            SideRect("Flag_White_Base",side,.76f,.39f,.80f,.34f,cream);
            SideRect("Flag_Red_Field",side,.76f,.39f,.80f,.22f,red,.003f);
            SideRect("Flag_Blue_Top",side,.91f,.39f,.80f,.035f,blue,.005f);
            SideRect("Flag_Blue_Bottom",side,.61f,.39f,.80f,.035f,blue,.005f);
            Disc("Flag_White_Disc",new Vector3(side*1.474f,.76f,.21f),.090f,cream,side);
            Star("Flag_Red_Star",new Vector3(side*1.477f,.76f,.21f),.072f,red,side);
            Text("Side_Number", "NF-5K",new Vector3(side*1.47f,.65f,2.25f),side>0?Quaternion.Euler(0,-90,0):Quaternion.Euler(0,90,0),.17f,cream);
            Text("Side_Fleet_Number","0721",new Vector3(side*1.47f,.49f,2.25f),side>0?Quaternion.Euler(0,-90,0):Quaternion.Euler(0,90,0),.070f,cream);
            var frontBranch=new[]{new Vector2(side*.39f,.29f),new Vector2(side*.62f,.35f),new Vector2(side*.90f,.47f),new Vector2(side*1.08f,.69f)};
            Branch(frontBranch,0,.006f);
            Branch(new[]{frontBranch[1],new Vector2(side*.77f,.58f),new Vector2(side*.72f,.76f)},0,.004f);
            foreach(var f in new[]{new Vector3(.48f,.32f,.065f),new Vector3(.65f,.40f,.080f),new Vector3(.80f,.49f,.072f),new Vector3(.72f,.73f,.059f),new Vector3(.98f,.59f,.075f),new Vector3(1.06f,.72f,.047f)})
                Flower(new Vector3(side*f.x,f.y,FrontZ(side*f.x)-.009f),0,f.z);
            // Window rail and panel joints remain separate from decorative artwork.
            for(int i=0;i<7;i++)SideRect("Painted_Panel_Seam",side,.73f,3.8f+i*1.8f,.012f,.61f,branch);
        }
        Text("Front_Number_Text","NF-5K",new Vector3(0,.53f,-.710f),Quaternion.identity,.11f,cream);
        Text("Front_Fleet_Number","0721",new Vector3(0,.26f,-.696f),Quaternion.identity,.070f,cream);
        Debug.Log("NF5K_LIVERY_OK: geometric sakura, flags, lettering; no baked illustration pasted onto body");
    }
    static float FrontZ(float x)
    {
        float q=Mathf.Clamp01((Mathf.Abs(x)-1.10f)/.35f);
        return -.15f-.50f*Mathf.Sqrt(1-q*q);
    }
    static Mesh PetalMesh()
    {
        const string path="Assets/Models/Cab/NF5KBodyMeshes/Notched_Sakura_Petal.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var edge=new[]{new Vector2(0,.035f),new Vector2(-.25f,.20f),new Vector2(-.37f,.48f),new Vector2(-.32f,.74f),new Vector2(-.13f,.94f),new Vector2(0,.82f),new Vector2(.13f,.94f),new Vector2(.32f,.74f),new Vector2(.37f,.48f),new Vector2(.25f,.20f)};
        var v=new List<Vector3>{new Vector3(0,.45f,0)};var t=new List<int>();
        foreach(var point in edge)v.Add(point);
        for(int i=0;i<edge.Length;i++){t.Add(0);t.Add(i+1);t.Add((i+1)%edge.Length+1);}
        mesh=new Mesh{name="Notched_Sakura_Petal"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void Flower(Vector3 p,int side,float radius)
    {
        var flower=new GameObject("Painted_Sakura_Blossom").transform;flower.SetParent(root,false);flower.localPosition=p;
        flower.localRotation=side==0?Quaternion.identity:Quaternion.Euler(0,side>0?-90:90,0);
        float phase=Mathf.Sin(p.y*31+p.z*13+p.x*7)*24;
        for(int i=0;i<5;i++)
        {
            var petal=new GameObject("Notched_Petal",typeof(MeshFilter),typeof(MeshRenderer));petal.transform.SetParent(flower,false);
            petal.transform.localPosition=new Vector3(0,0,-i*.00015f);
            petal.transform.localRotation=Quaternion.Euler(0,0,i*72+phase);
            petal.transform.localScale=Vector3.one*radius*(.94f+.08f*Mathf.Sin(i*2.3f+p.y*21));
            petal.GetComponent<MeshFilter>().sharedMesh=PetalMesh();petal.GetComponent<Renderer>().sharedMaterial=i%3==0?pale:pink;
        }
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;Vector3 tip=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius*.23f;
            Stroke("Sakura_Stamen",flower,new Vector3(0,0,-.001f),tip+Vector3.back*.001f,radius*.014f,pollen);
        }
    }
    static void Branch(Vector2[] path,int side,float width)
    {
        for(int i=0;i<path.Length-1;i++)
        {
            Vector3 a,b;
            if(side==0){a=new Vector3(path[i].x,path[i].y,FrontZ(path[i].x)-.006f);b=new Vector3(path[i+1].x,path[i+1].y,FrontZ(path[i+1].x)-.006f);}
            else{a=new Vector3(side*1.466f,path[i].y,path[i].x);b=new Vector3(side*1.466f,path[i+1].y,path[i+1].x);}
            // Project each thin painted segment onto the local surface plane.
            var obj=new GameObject("Painted_Sakura_Branch");obj.transform.SetParent(root,false);obj.transform.localPosition=(a+b)/2;
            obj.transform.localRotation=side==0?Quaternion.identity:Quaternion.Euler(0,side>0?-90:90,0);
            var localA=obj.transform.InverseTransformPoint(root.TransformPoint(a));var localB=obj.transform.InverseTransformPoint(root.TransformPoint(b));
            Stroke("Branch_Ink",obj.transform,localA,localB,width,branch);
        }
    }
    static void Stroke(string name,Transform parent,Vector3 a,Vector3 b,float width,Material material)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=(a+b)/2;obj.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2((b-a).y,(b-a).x)*Mathf.Rad2Deg);
        obj.transform.localScale=new Vector3(Vector3.Distance(a,b),width,.0005f);obj.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
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
            ['0']=new[]{new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(.55f,1),new Vector2(.55f,0),new Vector2(0,0)}},
            ['7']=new[]{new[]{new Vector2(0,1),new Vector2(.55f,1),new Vector2(.10f,0)}},
            ['2']=new[]{new[]{new Vector2(0,1),new Vector2(.55f,1),new Vector2(.55f,.55f),new Vector2(0,.10f),new Vector2(0,0),new Vector2(.55f,0)}},
            ['1']=new[]{new[]{new Vector2(.10f,.75f),new Vector2(.30f,1),new Vector2(.30f,0)},new[]{new Vector2(.08f,0),new Vector2(.52f,0)}},
            ['5']=new[]{new[]{new Vector2(.55f,1),new Vector2(0,1),new Vector2(0,.55f),new Vector2(.55f,.55f),new Vector2(.55f,0),new Vector2(0,0)}},
            ['K']=new[]{new[]{new Vector2(0,0),new Vector2(0,1)},new[]{new Vector2(.55f,1),new Vector2(0,.5f),new Vector2(.55f,0)}}
        };
        for(int c=0;c<value.Length;c++)foreach(var line in glyphs[value[c]])for(int i=0;i<line.Length-1;i++)
        {
            Vector2 a=line[i],b=line[i+1],n=new Vector2(-(b-a).y,(b-a).x).normalized*.050f;
            var offset=new Vector2(c*.75f-value.Length*.375f+.10f,-.5f);
            int k=v.Count;foreach(var point in new[]{a+n,a-n,b-n,b+n})v.Add((Vector3)(point+offset));
            t.Add(k);t.Add(k+2);t.Add(k+1);t.Add(k);t.Add(k+3);t.Add(k+2);
        }
        string path="Assets/Models/Cab/NF5KBodyMeshes/"+name+"_Lettering.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
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
