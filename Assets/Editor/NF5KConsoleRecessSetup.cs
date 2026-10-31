using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KConsoleRecessSetup
{
    struct Vertex
    {
        public Vector3 p,n;public Vector2 uv;
        public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};
    }
    public static void Apply(Transform body)
    {
        var controls=body.GetComponentInChildren<CabStartupControls>();if(controls==null)throw new InvalidOperationException("Startup panel missing");
        var deck=body.GetComponentsInChildren<MeshFilter>().Single(t=>t.name=="Contoured_Control_Deck");
        var face=controls.GetComponentsInChildren<Renderer>().Single(t=>t.name=="Removable_Panel");
        var housing=controls.GetComponentsInChildren<Renderer>().Single(t=>t.name=="Rear_Terminal_Enclosure");
        float deskTop=deck.GetComponent<Renderer>().bounds.max.y;
        var switchMount=body.GetComponentsInChildren<Transform>().Single(t=>t.name=="Switches_Mount");
        var panelPosition=controls.transform.position;panelPosition.z=switchMount.position.z-.195f;controls.transform.position=panelPosition;
        controls.transform.position+=Vector3.up*(deskTop+.0015f-face.bounds.max.y);
        foreach(var t in body.GetComponentsInChildren<Transform>().Where(t=>t.name=="Startup_Panel_Isolation_Mount").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        Bounds inner=housing.bounds;inner.Expand(new Vector3(.002f,0,.002f));
        Bounds cut=face.bounds;cut.Expand(new Vector3(.002f,0,.002f));
        const string folder="Assets/Models/Cab/NF5KBodyMeshes/";
        string originalPath=folder+"Console_Deck_Before_Startup_Recess.asset",path=folder+"Console_Deck_Startup_Recess.asset";
        var original=AssetDatabase.LoadAssetAtPath<Mesh>(originalPath);
        if(deck.sharedMesh.name!="Console_Deck_Startup_Recess")
        {
            if(original==null){original=UnityEngine.Object.Instantiate(deck.sharedMesh);AssetDatabase.CreateAsset(original,originalPath);}
            else EditorUtility.CopySerialized(deck.sharedMesh,original);
            EditorUtility.SetDirty(original);
        }
        if(original==null)throw new InvalidOperationException("Original console deck mesh unavailable");
        var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        var vertices=original.vertices;var ns=original.normals;var tex=original.uv;
        void Add(List<Vertex> polygon)
        {
            if(polygon.Count<3)return;int start=positions.Count;
            foreach(var v in polygon){positions.Add(deck.transform.InverseTransformPoint(v.p));normals.Add(v.n);uv.Add(v.uv);}
            for(int i=1;i<polygon.Count-1;i++){indices.Add(start);indices.Add(start+i);indices.Add(start+i+1);}
        }
        float Distance(Vertex v,int side)=>side==0?v.p.x-cut.min.x:side==1?cut.max.x-v.p.x:side==2?v.p.z-cut.min.z:cut.max.z-v.p.z;
        List<Vertex> Clip(List<Vertex> polygon,int side,bool inside)
        {
            var result=new List<Vertex>();if(polygon.Count==0)return result;
            for(int i=0;i<polygon.Count;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Count];float da=Distance(a,side),db=Distance(b,side);
                bool takeA=inside?da>=0:da<=0,takeB=inside?db>=0:db<=0;
                if(takeA)result.Add(a);if(takeA!=takeB)result.Add(Vertex.Lerp(a,b,da/(da-db)));
            }
            return result;
        }
        var triangles=original.triangles;
        for(int i=0;i<triangles.Length;i+=3)
        {
            var polygon=new List<Vertex>();for(int j=0;j<3;j++){int k=triangles[i+j];polygon.Add(new Vertex{p=deck.transform.TransformPoint(vertices[k]),n=ns[k],uv=tex.Length==vertices.Length?tex[k]:Vector2.zero});}
            for(int side=0;side<4&&polygon.Count>=3;side++){Add(Clip(polygon,side,false));polygon=Clip(polygon,side,true);}
        }
        void Quad(Vector3[] points,Vector3 worldNormal)
        {
            Vector3 normal=deck.transform.localToWorldMatrix.transpose.MultiplyVector(worldNormal).normalized;
            var polygon=points.Select(p=>new Vertex{p=p,n=normal,uv=Vector2.zero}).ToList();
            Vector3 cross=Vector3.Cross(deck.transform.InverseTransformPoint(points[1])-deck.transform.InverseTransformPoint(points[0]),deck.transform.InverseTransformPoint(points[2])-deck.transform.InverseTransformPoint(points[0]));
            if(Vector3.Dot(cross,normal)<0)polygon.Reverse();Add(polygon);
        }
        // A stepped rebate supports the flange instead of allowing it to intersect the worktop.
        float bottom=deck.GetComponent<Renderer>().bounds.min.y;
        float ledge=face.bounds.min.y;
        void Walls(Bounds bounds,float low,float high)
        {
            var corners=new[]{new Vector3(bounds.min.x,0,bounds.min.z),new Vector3(bounds.max.x,0,bounds.min.z),new Vector3(bounds.max.x,0,bounds.max.z),new Vector3(bounds.min.x,0,bounds.max.z)};
            for(int i=0;i<4;i++)
            {
                var a=corners[i];var b=corners[(i+1)%4];
                Quad(new[]{new Vector3(a.x,low,a.z),new Vector3(b.x,low,b.z),new Vector3(b.x,high,b.z),new Vector3(a.x,high,a.z)},Vector3.Scale(bounds.center-(a+b)*.5f,new Vector3(1,0,1)).normalized);
            }
        }
        Walls(cut,ledge,deskTop);Walls(inner,bottom,ledge);
        void Ring(float y,Vector3 normal)
        {
            void Strip(float x0,float x1,float z0,float z1)=>Quad(new[]{new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1)},normal);
            Strip(cut.min.x,inner.min.x,cut.min.z,cut.max.z);Strip(inner.max.x,cut.max.x,cut.min.z,cut.max.z);
            Strip(inner.min.x,inner.max.x,cut.min.z,inner.min.z);Strip(inner.min.x,inner.max.x,inner.max.z,cut.max.z);
        }
        Ring(ledge,Vector3.up);Ring(bottom,Vector3.down);
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.name="Console_Deck_Startup_Recess";mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);deck.sharedMesh=mesh;
        foreach(var collider in deck.GetComponents<MeshCollider>())collider.sharedMesh=mesh;
        var test=new GameObject("Temporary_Recess_Contact_Review");test.transform.SetParent(deck.transform,false);
        try
        {
            var collider=test.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
            if(collider.Raycast(new Ray(new Vector3(inner.center.x,deskTop+.1f,inner.center.z),Vector3.down),out _,.3f))throw new InvalidOperationException("Terminal opening is blocked by worktop mesh");
            float x=(cut.min.x+inner.min.x)*.5f;
            if(!collider.Raycast(new Ray(new Vector3(x,deskTop+.1f,inner.center.z),Vector3.down),out var support,.3f)||Mathf.Abs(support.point.y-ledge)>.0002f)throw new InvalidOperationException("Panel flange rebate has no supporting ledge");
        }
        finally{UnityEngine.Object.DestroyImmediate(test);}
        if(Mathf.Abs(face.bounds.max.y-deskTop-.0015f)>.0001f||housing.bounds.min.y>=deskTop)throw new InvalidOperationException("Startup panel is not recessed");
        Debug.Log("NF5K_STARTUP_RECESS_OK: face proud 1.5mm; enclosure below deck; ray-verified through-opening and flange-supporting stepped rebate");
    }
}
