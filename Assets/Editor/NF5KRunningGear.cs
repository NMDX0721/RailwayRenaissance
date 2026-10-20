using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KRunningGear
{
    static Transform root;
    static Material frame, tread, web, spring;
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes";
    public static void Build(Transform parent)
    {
        root=new GameObject("Running_Gear").transform;root.SetParent(parent,false);
        frame=Mat("Bogie_Cast_Steel",new Color(.095f,.105f,.115f),.65f,.24f);
        tread=Mat("Wheel_Machined_Tread",new Color(.31f,.33f,.35f),.90f,.28f);
        web=Mat("Wheel_Oxidized_Web",new Color(.15f,.13f,.115f),.70f,.19f);
        spring=Mat("Bogie_Spring_Steel",new Color(.19f,.20f,.21f),.75f,.30f);
        Box("Continuous_Underframe",new Vector3(0,-.19f,8.625f),new Vector3(2.45f,.25f,17.75f),frame,root);
        foreach(float z in new[]{2.5f,14.7f}) BuildBogie(z);
        foreach(float z in new[]{6.0f,8.0f,10.1f})
        {
            Box("Underfloor_Equipment_Cabinet",new Vector3(0,-.57f,z),new Vector3(1.9f,.52f,1.45f),frame,root);
            Box("Cabinet_Service_Cover",new Vector3(1.01f,-.55f,z),new Vector3(.06f,.39f,1.28f),spring,root);
        }
        Debug.Log("NF5K_RUNNING_GEAR_BUILD_OK: 2 powered two-axle bogies; 4 rigid wheelsets; assumed layout");
    }
    static void BuildBogie(float z)
    {
        var bogie=new GameObject("Powered_Bogie_"+z.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)).transform;
        bogie.SetParent(root,false);bogie.localPosition=new Vector3(0,0,z);
        foreach(float x in new[]{-1.12f,1.12f})
        {
            Box("Bogie_Side_Frame",new Vector3(x,-.65f,0),new Vector3(.18f,.20f,2.85f),frame,bogie);
            foreach(float axleZ in new[]{-1.10f,1.10f})
            {
                Box("Axlebox",new Vector3(x,-.90f,axleZ),new Vector3(.26f,.24f,.30f),frame,bogie);
                Cylinder("Axlebox_End_Cover",new Vector3(x+Mathf.Sign(x)*.15f,-.90f,axleZ),.18f,.025f,spring,bogie,true);
                foreach(float dz in new[]{-.23f,.23f}) Helix(new Vector3(x,-.62f,axleZ+dz),bogie);
                Box("Brake_Block",new Vector3(Mathf.Sign(x)*.79f,-.88f,axleZ+.40f),new Vector3(.12f,.22f,.075f),frame,bogie);
            }
        }
        Box("Bogie_Cross_Bolster",new Vector3(0,-.50f,0),new Vector3(2.25f,.22f,.42f),frame,bogie);
        Box("Body_Centre_Pivot",new Vector3(0,-.31f,0),new Vector3(.50f,.24f,.50f),spring,bogie);
        foreach(float axleZ in new[]{-1.10f,1.10f})
        {
            var axle=new GameObject("Driven_Wheelset").transform;axle.SetParent(bogie,false);axle.localPosition=new Vector3(0,-.90f,axleZ);
            Cylinder("Solid_Axle",Vector3.zero,.16f,2.26f,frame,axle,true);
            foreach(int side in new[]{-1,1})
            {
                var wheel=new GameObject("Steel_Wheel",typeof(MeshFilter),typeof(MeshRenderer));wheel.transform.SetParent(axle,false);
                wheel.transform.localPosition=new Vector3(side*.78f,0,0);
                wheel.transform.localRotation=side>0?Quaternion.identity:Quaternion.Euler(0,180,0);
                wheel.GetComponent<MeshFilter>().sharedMesh=WheelMesh();
                wheel.GetComponent<Renderer>().sharedMaterials=new[]{web,tread};
                Cylinder("Wheel_Hub",new Vector3(side*.88f,0,0),.22f,.10f,frame,axle,true);
            }
            Box("Axle_Gearcase",new Vector3(.25f,-.02f,-.04f),new Vector3(.33f,.36f,.38f),frame,axle);
            Cylinder("Traction_Motor_Envelope",new Vector3(-.19f,.10f,-.34f),.30f,.77f,frame,axle,true);
            Box("Motor_Nose_Support",new Vector3(0,-.64f,axleZ-.42f),new Vector3(.60f,.10f,.16f),spring,bogie);
        }
    }
    static Mesh WheelMesh()
    {
        string path=Folder+"/Profiled_Driving_Wheel.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
        var profile=new[]{new Vector2(-.11f,.10f),new Vector2(-.11f,.38f),new Vector2(-.10f,.46f),
            new Vector2(-.095f,.49f),new Vector2(-.065f,.49f),new Vector2(-.045f,.462f),
            new Vector2(.09f,.455f),new Vector2(.095f,.39f),new Vector2(.035f,.18f),new Vector2(.05f,.10f)};
        var v=new List<Vector3>();var dark=new List<int>();var bright=new List<int>();
        const int segments=64;
        for(int p=0;p<profile.Length;p++)for(int i=0;i<=segments;i++)
        {float a=2*Mathf.PI*i/segments;v.Add(new Vector3(profile[p].x,profile[p].y*Mathf.Cos(a),profile[p].y*Mathf.Sin(a)));}
        for(int p=0;p<profile.Length;p++)for(int i=0;i<segments;i++)
        {
            int a=p*(segments+1)+i,b=((p+1)%profile.Length)*(segments+1)+i;
            var t=p>=2&&p<=6?bright:dark;
            t.Add(a);t.Add(b+1);t.Add(b);t.Add(a);t.Add(a+1);t.Add(b+1);
        }
        var mesh=new Mesh{name="Profiled_Driving_Wheel"};mesh.SetVertices(v);mesh.subMeshCount=2;
        mesh.SetTriangles(dark,0);mesh.SetTriangles(bright,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void Helix(Vector3 position,Transform parent)
    {
        // Shared persistent tube mesh; coil is static and not a suspension solver.
        string path=Folder+"/Primary_Coil_Spring.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var v=new List<Vector3>();var t=new List<int>();const int turns=64,sides=6;
            for(int i=0;i<=turns;i++)for(int j=0;j<sides;j++)
            {
                float a=i/(float)turns*Mathf.PI*10,b=j/(float)sides*Mathf.PI*2;
                float r=.07f+.012f*Mathf.Cos(b);
                v.Add(new Vector3(r*Mathf.Cos(a),i/(float)turns*.21f+.012f*Mathf.Sin(b),r*Mathf.Sin(a)));
                if(i<turns){int n=i*sides+j,k=i*sides+(j+1)%sides;t.Add(n);t.Add(k+sides);t.Add(n+sides);t.Add(n);t.Add(k);t.Add(k+sides);}
            }
            mesh=new Mesh{name="Primary_Coil_Spring"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);
        }
        var obj=new GameObject("Primary_Coil_Spring",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.transform.localPosition=position;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=spring;
    }
    public static void ValidateReload()
    {
        var rootObj=GameObject.Find("Running_Gear");if(rootObj==null)throw new InvalidOperationException("Running gear absent");
        int wheels=0,axles=0,bogies=0;
        foreach(var t in rootObj.GetComponentsInChildren<Transform>())
        {
            if(t.name=="Steel_Wheel")wheels++;if(t.name=="Driven_Wheelset")axles++;if(t.name.StartsWith("Powered_Bogie_"))bogies++;
            if(t.name=="Steel_Wheel"&&Mathf.Abs(t.position.y+.90f)>.001f)throw new InvalidOperationException("Wheel centre misaligned");
        }
        if(wheels!=8||axles!=4||bogies!=2||GameObject.Find("Rear_Gangway_Door")==null)throw new InvalidOperationException("Incomplete car or wheelset count");
        Debug.Log("NF5K_FULL_CAR_RELOAD_OK: closed rear end, 2 bogies, 4 axles, 8 steel wheels; estimated 18.15m body");
    }
    static Material Mat(string name,Color color,float metallic,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(mat);return mat;
    }
    static void Box(string name,Vector3 position,Vector3 size,Material material,Transform parent)
    {var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;}
    static void Cylinder(string name,Vector3 position,float diameter,float length,Material material,Transform parent,bool transverse)
    {var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=new Vector3(diameter,length/2,diameter);obj.transform.localRotation=transverse?Quaternion.Euler(0,0,90):Quaternion.identity;obj.GetComponent<Renderer>().sharedMaterial=material;}
}
