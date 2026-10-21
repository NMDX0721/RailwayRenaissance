using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KFrontLightingSetup
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    public static void Build(Transform root,Material glass)
    {
        var lens=Mat("Headlamp_Optical_Glass",new Color(.77f,.83f,.86f,.12f),.05f,.94f,glass);
        var steel=Mat("Headlamp_Reflector_Silver",new Color(.67f,.69f,.71f),.95f,.74f);
        var rubber=Mat("Headlamp_Rubber",new Color(.018f,.021f,.024f),0,.12f);
        var emitter=Mat("Headlamp_Emitter",new Color(.24f,.22f,.17f),.15f,.4f);
        emitter.EnableKeyword("_EMISSION");emitter.SetColor("_EmissionColor",Color.white);emitter.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(emitter);
        var lights=new List<Light>();var surfaces=new List<Renderer>();var markers=new List<bool>();
        var optics=new List<MeshRenderer>();
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(r.name=="Roof_Headlamp_Lens"||r.name=="Front_Lamp_Lens")optics.Add(r);
        optics.Sort((a,b)=>b.transform.position.y.CompareTo(a.transform.position.y));
        int index=0;
        foreach(var original in optics)
        {
            bool roof=original.name=="Roof_Headlamp_Lens";float diameter=original.transform.lossyScale.x;
            bool marker=diameter<.1f;float radius=diameter*.5f;
            var assembly=new GameObject("Optical_Assembly_"+index).transform;assembly.SetParent(root,false);
            assembly.position=original.transform.position;
            // Axis is local -Z: bowl behind the opening, glass and light exit ahead of it.
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int steps=64,rings=8;
            for(int ring=0;ring<=rings;ring++)for(int i=0;i<=steps;i++)
            {
                float r=radius*(.12f+.80f*ring/rings),a=i*Mathf.PI*2/steps;
                vertices.Add(new Vector3(r*Mathf.Cos(a),r*Mathf.Sin(a),.028f*(1-Mathf.Pow(r/(radius*.92f),2))));
                if(ring<rings&&i<steps){int k=ring*(steps+1)+i;triangles.AddRange(new[]{k,k+steps+2,k+1,k,k+steps+1,k+steps+2});}
            }
            MeshObject("Parabolic_Reflector_"+index,vertices,triangles,steel,assembly);
            Ring("Seal_"+index,radius*1.04f,radius*.92f,-.001f,rubber,assembly);
            Ring("Retaining_Ring_"+index,radius*1.10f,radius*1.02f,-.003f,steel,assembly);
            // Reuse the transparent lens, remove primitive physics from decorative glass.
            original.sharedMaterial=lens;original.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(original.TryGetComponent<Collider>(out var collider))UnityEngine.Object.DestroyImmediate(collider);
            var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);bulb.name="Lamp_Emitter_"+index;bulb.transform.SetParent(assembly,false);
            bulb.transform.localPosition=new Vector3(0,0,-.016f);bulb.transform.localScale=new Vector3(diameter*.40f,diameter*.40f,.009f);
            bulb.GetComponent<Renderer>().sharedMaterial=emitter;bulb.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
            for(int screw=0;screw<4;screw++)
            {
                float a=(45+90*screw)*Mathf.Deg2Rad;
                var bolt=GameObject.CreatePrimitive(PrimitiveType.Sphere);bolt.name="Optics_Retainer_Screw";bolt.transform.SetParent(assembly,false);
                bolt.transform.localPosition=new Vector3(Mathf.Cos(a)*radius*1.06f,Mathf.Sin(a)*radius*1.06f,-.005f);
                bolt.transform.localScale=Vector3.one*(roof?.010f:.005f);bolt.GetComponent<Renderer>().sharedMaterial=steel;
                UnityEngine.Object.DestroyImmediate(bolt.GetComponent<Collider>());
            }
            var beam=new GameObject("Lamp_Beam_"+index).AddComponent<Light>();beam.transform.SetParent(assembly,false);
            beam.transform.localPosition=new Vector3(0,0,-.027f);beam.transform.rotation=Quaternion.Euler(roof?2:6,180,0);
            beam.type=LightType.Spot;beam.range=marker?4:roof?120:55;beam.spotAngle=marker?75:roof?22:40;
            beam.innerSpotAngle=marker?35:roof?12:22;beam.shadows=marker?LightShadows.None:LightShadows.Soft;
            beam.shadowBias=.005f;beam.shadowNormalBias=.015f;beam.cullingMask=~0;beam.renderMode=LightRenderMode.ForcePixel;
            lights.Add(beam);surfaces.Add(bulb.GetComponent<Renderer>());markers.Add(marker);index++;
        }
        foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("Optical_Assembly_")||t.GetComponentInParent<Light>()!=null)t.gameObject.layer=30;
        foreach(var light in lights)foreach(var t in light.transform.parent.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        var controller=root.gameObject.AddComponent<CabExteriorLighting>();
        controller.Configure(root.GetComponentInChildren<CabEngineState>(),lights.ToArray(),surfaces.ToArray(),markers.ToArray());
        Debug.Log("NF5K_OPTICS_BUILT: five separately controlled emitters and real spot lights");
    }
    static void Ring(string name,float outer,float inner,float z,Material material,Transform parent)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<=64;i++)
        {
            float a=i*Mathf.PI/32;v.Add(new Vector3(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer,z));v.Add(new Vector3(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner,z));
            if(i<64){int k=i*2;t.AddRange(new[]{k,k+3,k+1,k,k+2,k+3});}
        }
        MeshObject(name,v,t,material,parent);
    }
    static void MeshObject(string name,List<Vector3> vertices,List<int> triangles,Material material,Transform parent)
    {
        string path=Folder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        for(int i=0;i<triangles.Count;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    static Material Mat(string name,Color color,float metal,float smooth,Material source=null)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=source==null?new Material(Shader.Find("Universal Render Pipeline/Lit")):new Material(source);AssetDatabase.CreateAsset(mat,path);}
        else if(source!=null)mat.CopyPropertiesFromMaterial(source);
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(mat);return mat;
    }
}
