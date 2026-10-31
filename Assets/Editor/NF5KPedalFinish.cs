using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KPedalFinish
{
    const string Folder="Assets/Resources/Cab/";
    public static Mesh Wedge()
    {
        const string path=Folder+"Cab_Pedal_Wedge_V3.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var v=new List<Vector3>();var t=new List<int>();
        Vector3 Top(float x,float z)=>new Vector3(x,.055f-Mathf.Tan(15*Mathf.Deg2Rad)*z,z);
        Vector3 Bottom(float x,float z)=>new Vector3(x,.006f,z);
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var q=b;b=d;d=q;}
            int i=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
        }
        void Patch(float x0,float x1,float z0,float z1)=>Quad(Top(x0,z0),Top(x1,z0),Top(x1,z1),Top(x0,z1),Vector3.up);
        // Continuous folded shell, with an actual aperture around the active tread.
        Patch(-.18f,.04f,-.125f,.125f);Patch(.16f,.22f,-.125f,.125f);
        Patch(.04f,.16f,-.125f,-.095f);Patch(.04f,.16f,.095f,.125f);
        Quad(Bottom(-.18f,-.125f),Bottom(.22f,-.125f),Bottom(.22f,.125f),Bottom(-.18f,.125f),Vector3.down);
        var corners=new[]{new Vector2(-.18f,-.125f),new Vector2(.22f,-.125f),new Vector2(.22f,.125f),new Vector2(-.18f,.125f)};
        for(int i=0;i<4;i++){var a=corners[i];var b=corners[(i+1)%4];var n=Vector3.Cross(Top(b.x,b.y)-Top(a.x,a.y),Vector3.down);Quad(Top(a.x,a.y),Bottom(a.x,a.y),Bottom(b.x,b.y),Top(b.x,b.y),n);}
        mesh=new Mesh{name="Pedal_Folded_Wedge_15deg"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        mesh.uv=v.ConvertAll(p=>new Vector2(p.x*4,p.z*4)).ToArray();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    public static Material Material(string name,Color color,float metal,float smooth,bool rubber)
    {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        string tp=Folder+name+"_Grain.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        if(tex==null)
        {
            const int size=128;tex=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name=name+"_Grain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float grain=Mathf.PerlinNoise(x*.83f+17,y*.83f+31)-.5f;
                float foot=Mathf.Exp(-((x-64)*(x-64)/900f+(y-63)*(y-63)/1800f));
                float value=rubber?.84f+grain*.15f+foot*.14f:.90f+grain*.08f;
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            tex.SetPixels(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,tp);
        }
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.SetTexture("_BaseMap",tex);EditorUtility.SetDirty(m);return m;
    }
    public static Mesh Rounded(Vector3 size,float radius)
    {
        string key=size.ToString("F5")+radius.ToString("F5",System.Globalization.CultureInfo.InvariantCulture);
        string path=Folder+"Pedal_Rounded_"+Hash128.Compute(key)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        var half=size/2;radius=Mathf.Min(radius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.85f);var core=half-Vector3.one*radius;
        Vector3 Abs(Vector3 a)=>new Vector3(Mathf.Abs(a.x),Mathf.Abs(a.y),Mathf.Abs(a.z));
        foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            var a=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;var b=Vector3.Cross(normal,a);
            float ha=Vector3.Dot(Abs(a),half),hb=Vector3.Dot(Abs(b),half),hn=Vector3.Dot(Abs(normal),half);
            float[] xs={-ha,-ha+radius*.293f,-ha+radius,ha-radius,ha-radius*.293f,ha};
            float[] ys={-hb,-hb+radius*.293f,-hb+radius,hb-radius,hb-radius*.293f,hb};int start=vertices.Count;
            for(int y=0;y<6;y++)for(int x=0;x<6;x++)
            {
                var p=normal*hn+a*xs[x]+b*ys[y];var q=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));var n=(p-q).normalized;
                vertices.Add(q+n*radius);normals.Add(n);uv.Add(new Vector2(p.x/size.x+.5f,p.z/size.z+.5f));
                if(x<5&&y<5){int k=start+y*6+x;indices.AddRange(new[]{k,k+1,k+7,k,k+7,k+6});}
            }
        }
        mesh=new Mesh{name="Pedal_Rounded"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
