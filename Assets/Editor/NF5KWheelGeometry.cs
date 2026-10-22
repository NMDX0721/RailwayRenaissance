using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KWheelGeometry
{
    public const int Segments=192;
    public const string Path="Assets/Models/Cab/NF5KBodyMeshes/Forged_Wheel_Rounded_V3.asset";
    static List<Vector2> profile;
    public static List<Vector2> Profile()
    {
        if(profile!=null)return profile;
        var p=new[]{new Vector2(-.11f,.10f),new Vector2(-.11f,.16f),new Vector2(-.065f,.20f),new Vector2(-.06f,.34f),new Vector2(-.10f,.40f),new Vector2(-.10f,.46f),
            new Vector2(-.095f,.49f),new Vector2(-.065f,.49f),new Vector2(-.045f,.462f),new Vector2(.09f,.455f),new Vector2(.095f,.42f),new Vector2(.085f,.385f),
            new Vector2(.060f,.365f),new Vector2(.048f,.30f),new Vector2(.050f,.24f),new Vector2(.062f,.18f),new Vector2(.075f,.145f),new Vector2(.08f,.10f)};
        profile=new List<Vector2>();
        for(int i=0;i<p.Length;i++)
        {
            var previous=p[(i+p.Length-1)%p.Length];var next=p[(i+1)%p.Length];
            float trim=Mathf.Min(i>=11&&i<=16?.010f:.0035f,Mathf.Min(Vector2.Distance(previous,p[i]),Vector2.Distance(p[i],next))*.22f);
            var a=p[i]+(previous-p[i]).normalized*trim;var b=p[i]+(next-p[i]).normalized*trim;
            for(int j=0;j<=6;j++){float t=j/6f;profile.Add((1-t)*(1-t)*a+2*(1-t)*t*p[i]+t*t*b);}
        }
        return profile;
    }
    public static Vector2 At(float v)
    {
        var p=Profile();float total=0;
        for(int i=0;i<p.Count;i++)total+=Vector2.Distance(p[i],p[(i+1)%p.Count]);
        float remaining=v*total;
        for(int i=0;i<p.Count;i++)
        {
            var next=p[(i+1)%p.Count];float length=Vector2.Distance(p[i],next);
            if(remaining<=length)return Vector2.Lerp(p[i],next,remaining/length);
            remaining-=length;
        }
        return p[0];
    }
    public static Mesh Build()
    {
        var p=Profile();float total=0;for(int i=0;i<p.Count;i++)total+=Vector2.Distance(p[i],p[(i+1)%p.Count]);
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
        var plate=new List<int>();var rim=new List<int>();float distance=0;
        for(int k=0;k<p.Count;k++)
        {
            int next=(k+1)%p.Count;float length=Vector2.Distance(p[k],p[next]);int start=vertices.Count;
            for(int i=0;i<=Segments;i++)
            {
                float angle=i*2*Mathf.PI/Segments,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                for(int e=0;e<2;e++)
                {
                    int n=e==0?k:next;var point=p[n];
                    var tangent=(p[(n+1)%p.Count]-p[(n+p.Count-1)%p.Count]).normalized;
                    vertices.Add(new Vector3(point.x,point.y*c,point.y*s));
                    normals.Add(new Vector3(-tangent.y,tangent.x*c,tangent.x*s).normalized);
                    uv.Add(new Vector2(i/(float)Segments,(distance+e*length)/total));
                }
                if(i<Segments){int a=start+i*2;var triangles=(p[k].y+p[next].y)*.5f>.405f?rim:plate;triangles.AddRange(new[]{a,a+3,a+1,a,a+2,a+3});}
            }
            distance+=length;
        }
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Path);
        if(mesh==null){mesh=new Mesh{name="Forged_Wheel_Rounded_V3"};AssetDatabase.CreateAsset(mesh,Path);}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=2;
        mesh.SetTriangles(plate,0);mesh.SetTriangles(rim,1);mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
    }
}
