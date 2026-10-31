using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KParkingConsoleMount
{
    public const float Width=.126f,Depth=.090f,ButtonX=.030f;
    public static readonly Vector3 Anchor=new Vector3(1.16f,.89f,.30f);
    public static readonly Quaternion Tilt=Quaternion.Euler(30,0,0);
    sealed class Builder
    {
        public readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> indices=new List<int>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
        {
            if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-15f&&Vector3.Cross(c-a,d-a).sqrMagnitude<1e-15f)return;
            var points=new[]{a,b,c,d};if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0)Array.Reverse(points);
            int start=vertices.Count;foreach(var p in points){vertices.Add(p);normals.Add(normal);uv.Add(new Vector2(p.x,p.z)*10);}
            indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }
        public Mesh Save(string name)
        {
            string path="Assets/Resources/Cab/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);return mesh;
        }
    }
    public static GameObject MeshPart(Transform panel,string name,Mesh mesh,Material mat,bool collision=false)
    {
        var g=new GameObject(name);g.layer=30;g.transform.SetParent(panel,false);
        g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(collision)g.AddComponent<MeshCollider>().sharedMesh=mesh;return g;
    }
    public static Mesh PerforatedPlate(string name,float top,float thickness)
    {
        var mesh=new Builder();
        foreach(int side in new[]{-1,1})
        {
            float cx=side*ButtonX,cz=-.004f,x0=side<0?-Width*.5f:0,x1=side<0?0:Width*.5f,z0=-Depth*.5f,z1=Depth*.5f;
            var angles=Enumerable.Range(0,64).Select(i=>i*Mathf.PI*2/64).ToList();
            foreach(float x in new[]{x0,x1})foreach(float z in new[]{z0,z1}){float a=Mathf.Atan2(z-cz,x-cx);angles.Add(a<0?a+Mathf.PI*2:a);}
            angles=angles.Distinct().OrderBy(a=>a).ToList();
            Vector3 Outer(float a,float y)
            {
                float x=Mathf.Cos(a),z=Mathf.Sin(a);
                float r=Mathf.Min(Mathf.Abs(x)<.000001f?float.MaxValue:Mathf.Abs(((x>0?x1:x0)-cx)/x),Mathf.Abs(z)<.000001f?float.MaxValue:Mathf.Abs(((z>0?z1:z0)-cz)/z));
                return new Vector3(cx+x*r,y,cz+z*r);
            }
            Vector3 Inner(float a,float y)=>new Vector3(cx+Mathf.Cos(a)*.011f,y,cz+Mathf.Sin(a)*.011f);
            for(int i=0;i<angles.Count;i++)
            {
                float a=angles[i],b=angles[(i+1)%angles.Count];var oa=Outer(a,top);var ob=Outer(b,top);var ia=Inner(a,top);var ib=Inner(b,top);var down=Vector3.down*thickness;
                mesh.Quad(oa,ob,ib,ia,Vector3.up);mesh.Quad(oa+down,ia+down,ib+down,ob+down,Vector3.down);
                mesh.Quad(ia,ib,ib+down,ia+down,(new Vector3(cx,top,cz)-(ia+ib)*.5f).normalized);
                if(Mathf.Abs(oa.x)>.00001f||Mathf.Abs(ob.x)>.00001f)mesh.Quad(ob,oa,oa+down,ob+down,Vector3.Cross(ob-oa,Vector3.up).normalized);
            }
        }
        return mesh.Save(name);
    }
    // Closest points are on actual triangles, not the imported renderer/collider AABB.
    public static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
    {
        var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
        if(d1<=0&&d2<=0)return a;
        var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
        float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
        var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
        float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
        float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
        float denom=va+vb+vc;if(Mathf.Abs(denom)<1e-20f)return a;
        return a+ab*(vb/denom)+ac*(vc/denom);
    }
    public static Vector3 Nearest(MeshFilter deck,Vector3 world)
    {
        var vertices=deck.sharedMesh.vertices;var triangles=deck.sharedMesh.triangles;var p=deck.transform.InverseTransformPoint(world);float best=float.PositiveInfinity;var result=p;
        for(int i=0;i<triangles.Length;i+=3)
        {var q=Closest(p,vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]]);float d=(q-p).sqrMagnitude;if(d<best){best=d;result=q;}}
        return deck.transform.TransformPoint(result);
    }
    public static void Housing(Transform panel,MeshFilter deck,Material paint,Material rubber)
    {
        var housing=new Builder();var gasket=new Builder();const int count=128;
        var outer=new Vector3[count];var inner=new Vector3[count];var rear=new Vector3[count];var contact=new Vector3[count];
        for(int i=0;i<count;i++)
        {
            float t=i/(float)(count/4);int edge=Mathf.FloorToInt(t);float f=t-edge;
            var corners=new[]{new Vector3(-.071f,-.0045f,-.052f),new Vector3(.071f,-.0045f,-.052f),new Vector3(.071f,-.0045f,.052f),new Vector3(-.071f,-.0045f,.052f)};
            outer[i]=Vector3.Lerp(corners[edge],corners[(edge+1)%4],f);
            inner[i]=new Vector3(outer[i].x*.057f/.071f,-.0045f,outer[i].z*.038f/.052f);
            var world=panel.TransformPoint(outer[i]);var hit=Nearest(deck,world);
            if(Vector3.Distance(hit,world)>.13f)throw new InvalidOperationException("Parking mount too far from real desk surface");
            contact[i]=panel.InverseTransformPoint(hit);rear[i]=panel.InverseTransformPoint(hit+(world-hit).normalized*.0006f);
        }
        for(int i=0;i<count;i++)
        {
            int j=(i+1)%count;
            housing.Quad(outer[i],outer[j],inner[j],inner[i],Vector3.up);
            var n=Vector3.Cross(outer[j]-outer[i],rear[i]-outer[i]).normalized;
            housing.Quad(outer[i],rear[i],rear[j],outer[j],n);
            gasket.Quad(rear[i],contact[i],contact[j],rear[j],n);
        }
        // Close the base against the desk; retain the screwed top as the service access.
        var center=rear.Aggregate(Vector3.zero,(sum,v)=>sum+v)/count;
        for(int i=0;i<count;i++)
            housing.Quad(center,rear[(i+1)%count],rear[i],center,-panel.InverseTransformDirection(deck.transform.up));
        MeshPart(panel,"Parking_Console_Folded_Mount",housing.Save("Cab_Parking_Console_Mount_V7"),paint,true);
        MeshPart(panel,"Parking_Console_Contact_Gasket",gasket.Save("Cab_Parking_Console_Contact_V7"),rubber);
    }
}
