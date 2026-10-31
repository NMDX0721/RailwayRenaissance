using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KLightingKeysSetup
{
    public static void Build(Transform fittings,Transform body)
    {
        var pivots=body.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("KeyPivot_",StringComparison.Ordinal)).OrderByDescending(t=>t.position.x).ToArray();
        if(pivots.Length!=10)throw new InvalidOperationException("Expected original ten individual key pivots");
        foreach(var previous in body.GetComponentsInChildren<CabLightingKey>())UnityEngine.Object.DestroyImmediate(previous);
        var root=new GameObject("NF5K_Lighting_Key_Plates").transform;root.SetParent(fittings,false);
        var rails=body.GetComponentsInChildren<MeshRenderer>().Where(t=>t.name.StartsWith("Frame_Long_",StringComparison.Ordinal)).ToArray();
        var back=rails.OrderBy(t=>t.bounds.center.z).First();var front=rails.OrderByDescending(t=>t.bounds.center.z).First();
        var ends=body.GetComponentsInChildren<MeshRenderer>().Where(t=>t.name.StartsWith("Frame_End_",StringComparison.Ordinal)).OrderBy(t=>t.bounds.center.x).ToArray();
        float top=back.bounds.max.y;
        float x0=back.bounds.min.x,x1=back.bounds.max.x,z0=back.bounds.min.z-.030f,z1=front.bounds.max.z;
        float ix0=ends[0].bounds.max.x,ix1=ends[1].bounds.min.x,iz0=back.bounds.max.z,iz1=front.bounds.min.z;
        var bezel=new GameObject("Lighting_Keys_Continuous_Bezel");bezel.layer=30;bezel.transform.SetParent(root,false);
        var mesh=ContinuousBezel(root,x0,x1,z0,z1,ix0,ix1,iz0,iz1,top);
        bezel.AddComponent<MeshFilter>().sharedMesh=mesh;bezel.AddComponent<MeshRenderer>().sharedMaterial=back.sharedMaterial;
        foreach(var r in body.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Frame_Long_",StringComparison.Ordinal)||r.name.StartsWith("Frame_End_",StringComparison.Ordinal)||r.name.StartsWith("Subtle_Edge_",StringComparison.Ordinal)))r.enabled=false;
        var basePosition=root.InverseTransformPoint(new Vector3(back.bounds.center.x,top,back.bounds.min.z-.0148f));
        var startup=body.GetComponentInChildren<CabStartupControls>().GetComponentsInChildren<Renderer>().Single(r=>r.name=="Removable_Panel");
        float separation=z0-startup.bounds.max.z;
        if(separation<.045f)throw new InvalidOperationException("Lighting and startup panels have insufficient candidate separation");
        Debug.Log("NF5K_LIGHTING_LAYOUT_OK: unified aperture frame; startup/lighting edge separation="+separation.ToString("F4")+"m (authored layout, not a regulatory limit)");
        for(int i=0;i<10;i++)
        {
            var key=pivots[i].GetComponentsInChildren<MeshFilter>().Single(t=>t.name.StartsWith("Rocker_Key_",StringComparison.Ordinal));
            var hit=key.GetComponent<BoxCollider>();if(hit==null)hit=key.gameObject.AddComponent<BoxCollider>();hit.center=key.sharedMesh.bounds.center;hit.size=key.sharedMesh.bounds.size;
            key.gameObject.layer=30;
            key.gameObject.AddComponent<CabLightingKey>().Configure(i,pivots[i]);
            Vector3 p=root.InverseTransformPoint(pivots[i].position);p.y=basePosition.y+.0001f;p.z=basePosition.z+.009f;
            string name="Lighting_Key_L"+(i+1).ToString("00")+"_Legend";
            NF5KEquipmentPrint.Add(root,name,"Cab_Lighting_Key_L"+(i+1).ToString("00")+"_Integrated_V2",p,new Vector2(.0186f,.027f));
            root.Find(name).localRotation=Quaternion.Euler(-90,0,0);
            var printRenderer=root.Find(name).GetComponent<Renderer>();printRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;printRenderer.receiveShadows=false;
            var ink=printRenderer.sharedMaterial;
            ConfigureInk(ink);
        }
    }
    public static void ConfigureInk(Material ink)
    {
        ink.SetFloat("_Surface",1);ink.SetFloat("_AlphaClip",0);ink.DisableKeyword("_ALPHATEST_ON");ink.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ink.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);ink.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ink.SetFloat("_SrcBlendAlpha",1);ink.SetFloat("_DstBlendAlpha",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);ink.SetFloat("_ZWrite",0);
        ink.SetOverrideTag("RenderType","Transparent");ink.renderQueue=3000;
        ink.SetFloat("_Smoothness",.08f);ink.SetFloat("_SpecularHighlights",0);ink.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");EditorUtility.SetDirty(ink);
    }
    static Mesh ContinuousBezel(Transform root,float x0,float x1,float z0,float z1,float ix0,float ix1,float iz0,float iz1,float top)
    {
        var positions=new System.Collections.Generic.List<Vector3>();var normals=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
        Vector3[] Ring(float a,float b,float c,float d,float y)=>new[]{new Vector3(a,y,c),new Vector3(b,y,c),new Vector3(b,y,d),new Vector3(a,y,d)};
        var outer=Ring(x0,x1,z0,z1,top);var inner=Ring(ix0,ix1,iz0,iz1,top);
        var lowerOuter=Ring(x0,x1,z0,z1,top-.0045f);var lowerInner=Ring(ix0,ix1,iz0-.0033f,iz1+.0033f,top-.0045f);
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n)
        {
            int start=positions.Count;var points=new[]{a,b,c,d};
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)System.Array.Reverse(points);
            foreach(var p in points){positions.Add(root.InverseTransformPoint(p));normals.Add(root.localToWorldMatrix.transpose.MultiplyVector(n).normalized);}
            triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }
        var center=(outer[0]+outer[2])*.5f;var holeCenter=(inner[0]+inner[2])*.5f;
        for(int i=0;i<4;i++)
        {
            int j=(i+1)%4;
            Quad(outer[i],outer[j],inner[j],inner[i],Vector3.up);
            Quad(lowerOuter[i],lowerOuter[j],lowerInner[j],lowerInner[i],Vector3.down);
            Quad(outer[i],outer[j],lowerOuter[j],lowerOuter[i],Vector3.Scale((outer[i]+outer[j])*.5f-center,new Vector3(1,0,1)).normalized);
            Quad(inner[i],inner[j],lowerInner[j],lowerInner[i],Vector3.Scale(holeCenter-(inner[i]+inner[j])*.5f,new Vector3(1,0,1)).normalized);
        }
        string path="Assets/Resources/Cab/Cab_Lighting_Continuous_Bezel_V7.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.name="Lighting_Continuous_Bezel_V7";mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,new System.Collections.Generic.List<Vector2>(positions.Select(p=>Vector2.zero)));mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);return mesh;
    }
}
