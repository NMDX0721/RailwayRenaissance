using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KRunningGear
{
    static Transform root;
    static Material frame, tread, web, spring, rubber, friction;
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes";
    public static void Build(Transform parent)
    {
        root=new GameObject("Running_Gear").transform;root.SetParent(parent,false);
        frame=Mat("Bogie_Cast_Steel",new Color(.095f,.105f,.115f),.65f,.24f);
        tread=Mat("Wheel_Machined_Tread",new Color(.66f,.69f,.72f),.86f,.40f);
        web=Mat("Wheel_Oxidized_Web",new Color(.39f,.405f,.42f),.38f,.28f);
        FinishWheelMaterials();
        spring=Mat("Bogie_Spring_Steel",new Color(.19f,.20f,.21f),.75f,.30f);
        rubber=Mat("Bogie_Elastomer",new Color(.035f,.039f,.038f),0,.12f);
        friction=Mat("Bogie_Brake_Friction",new Color(.21f,.20f,.18f),.08f,.14f);
        Box("Continuous_Underframe",new Vector3(0,-.19f,8.625f),new Vector3(2.45f,.25f,17.75f),frame,root);
        foreach(float z in new[]{2.5f,14.7f}) BuildBogie(z);
        foreach(float z in new[]{6.0f,8.0f,10.1f})
        {
            Box("Underfloor_Equipment_Cabinet",new Vector3(0,-.57f,z),new Vector3(1.9f,.52f,1.45f),frame,root);
            Box("Cabinet_Service_Cover",new Vector3(1.01f,-.55f,z),new Vector3(.06f,.39f,1.28f),spring,root);
        }
        NF5KIndustrialFinish.FinishRunningGear(root);
        foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=parent.gameObject.layer;
        Debug.Log("NF5K_RUNNING_GEAR_BUILD_OK: 2 powered two-axle bogies; 4 rigid wheelsets; assumed layout");
    }
    public static void FinishWheelMaterials()
    {
        var rolling=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Wheel_Machined_Tread.mat");
        var plate=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Wheel_Oxidized_Web.mat");
        if(rolling==null||plate==null)return;
        string path=Folder+"/Wheel_Subtle_Machining.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture==null)
        {
            texture=new Texture2D(256,256,TextureFormat.RGBA32,true,true){name="Wheel_Subtle_Machining",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float grain=Mathf.Repeat(Mathf.Sin(x*12.9898f+y*78.233f)*43758.5453f,1);
                float value=.955f+.017f*Mathf.Sin(y*2.5f)+.024f*(grain-.5f);
                pixels[y*256+x]=new Color(value,value,value,1);
            }
            texture.SetPixels(pixels);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,path);
        }
        rolling.SetColor("_BaseColor",new Color(.66f,.69f,.72f));rolling.SetFloat("_Metallic",.86f);rolling.SetFloat("_Smoothness",.40f);
        plate.SetColor("_BaseColor",new Color(.39f,.405f,.42f));plate.SetFloat("_Metallic",.38f);plate.SetFloat("_Smoothness",.28f);
        foreach(var mat in new[]{rolling,plate})
        {
            mat.SetTexture("_BaseMap",texture);mat.SetTextureScale("_BaseMap",Vector2.one);
            mat.SetTexture("_MetallicGlossMap",null);mat.DisableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(mat);
        }
    }
    static void BuildBogie(float z)
    {
        var bogie=new GameObject("Powered_Bogie_"+z.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)).transform;
        bogie.SetParent(root,false);bogie.localPosition=new Vector3(0,0,z);
        foreach(float x in new[]{-1.12f,1.12f})
        {
            Box("Bogie_Side_Frame",new Vector3(x,-.59f,0),new Vector3(.18f,.20f,1.06f),frame,bogie);
            foreach(int end in new[]{-1,1})
            {
                Box("Bogie_End_Spring_Beam",new Vector3(x,-.415f,end*1.08f),new Vector3(.20f,.13f,.83f),frame,bogie);
                Link("Side_Frame_Rising_Web",new Vector3(x,-.59f,end*.48f),new Vector3(x,-.415f,end*.75f),.18f,.16f,frame,bogie);
            }
            foreach(float axleZ in new[]{-1.10f,1.10f})
            {
                Box("Axlebox",new Vector3(x,-.90f,axleZ),new Vector3(.26f,.24f,.30f),frame,bogie);
                Cylinder("Axlebox_End_Cover",new Vector3(x+Mathf.Sign(x)*.15f,-.90f,axleZ),.18f,.025f,spring,bogie,true);
                foreach(float dz in new[]{-.23f,.23f})
                {
                    Helix(new Vector3(x,-.70f,axleZ+dz),bogie);
                    Box("Primary_Lower_Spring_Seat",new Vector3(x,-.73f,axleZ+dz),new Vector3(.21f,.036f,.20f),spring,bogie);
                    Box("Primary_Upper_Spring_Seat",new Vector3(x,-.46f,axleZ+dz),new Vector3(.21f,.036f,.20f),spring,bogie);
                    Box("Axlebox_Spring_Wing",new Vector3(x,-.765f,axleZ+dz*.75f),new Vector3(.24f,.05f,.24f),frame,bogie);
                }
                foreach(int direction in new[]{-1,1})
                    Box("Axlebox_Guide_Horn",new Vector3(x,-.72f,axleZ+direction*.181f),new Vector3(.22f,.39f,.045f),frame,bogie);
                for(int bolt=0;bolt<6;bolt++)
                {
                    float a=bolt*Mathf.PI/3;
                    Cylinder("Axlebox_Cover_Bolt",new Vector3(x+Mathf.Sign(x)*.169f,-.90f+Mathf.Cos(a)*.068f,axleZ+Mathf.Sin(a)*.068f),.016f,.012f,spring,bogie,true);
                }
                Brake(bogie,Mathf.Sign(x),axleZ);
            }
        }
        Box("Bogie_Cross_Bolster",new Vector3(0,-.50f,0),new Vector3(2.25f,.22f,.42f),frame,bogie);
        Box("Body_Centre_Pivot",new Vector3(0,-.31f,0),new Vector3(.50f,.24f,.50f),spring,bogie);
        foreach(int side in new[]{-1,1})
        {
            Box("Secondary_Lower_Plate",new Vector3(side*.91f,-.395f,0),new Vector3(.29f,.025f,.35f),spring,bogie);
            Box("Secondary_Elastomer_Pad",new Vector3(side*.91f,-.35f,0),new Vector3(.25f,.065f,.31f),rubber,bogie);
            Box("Secondary_Upper_Plate",new Vector3(side*.91f,-.308f,0),new Vector3(.29f,.02f,.35f),spring,bogie);
        }
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
            var equipment=new GameObject("Non_Rotating_Axle_Drive").transform;equipment.SetParent(bogie,false);equipment.localPosition=axle.localPosition;
            Box("Axle_Gearcase",new Vector3(.25f,-.02f,-.04f),new Vector3(.33f,.36f,.38f),frame,equipment);
            Cylinder("Traction_Motor_Envelope",new Vector3(-.19f,.10f,-.34f),.30f,.77f,frame,equipment,true);
            foreach(float x in new[]{-.55f,.17f})Cylinder("Motor_End_Shield",new Vector3(x,.10f,-.34f),.31f,.028f,spring,equipment,true);
            Box("Motor_Nose_Support",new Vector3(0,-.64f,axleZ-.42f),new Vector3(.60f,.10f,.16f),spring,bogie);
        }
    }
    static void Link(string name,Vector3 a,Vector3 b,float width,float height,Material material,Transform parent)
    {
        Box(name,(a+b)/2,new Vector3(width,height,Vector3.Distance(a,b)),material,parent);
        parent.GetChild(parent.childCount-1).localRotation=Quaternion.LookRotation(b-a,Vector3.up);
    }
    static void Brake(Transform parent,float side,float axleZ)
    {
        float direction=-Mathf.Sign(axleZ), x=side*.795f;
        var shoe=new GameObject("Curved_Tread_Brake_Shoe",typeof(MeshFilter),typeof(MeshRenderer));shoe.transform.SetParent(parent,false);
        shoe.transform.localPosition=new Vector3(x,-.90f,axleZ);shoe.transform.localRotation=Quaternion.Euler(0,direction>0?0:180,0);
        string path=Folder+"/Curved_Tread_Brake_Shoe_V2.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var v=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<12;i++)
            {
                float a=Mathf.Lerp(-18,18,i/12f)*Mathf.Deg2Rad,b=Mathf.Lerp(-18,18,(i+1)/12f)*Mathf.Deg2Rad;
                Vector3 P(float xx,float r,float angle)=>new Vector3(xx,Mathf.Sin(angle)*r,Mathf.Cos(angle)*r);
                var points=new[]{P(-.05f,.467f,a),P(.05f,.467f,a),P(.05f,.467f,b),P(-.05f,.467f,b),P(-.05f,.505f,a),P(.05f,.505f,a),P(.05f,.505f,b),P(-.05f,.505f,b)};
                void Quad(int aa,int bb,int cc,int dd)
                {int start=v.Count;v.AddRange(new[]{points[aa],points[bb],points[cc],points[dd]});triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}
                Quad(0,3,2,1);Quad(4,5,6,7);Quad(0,4,7,3);Quad(1,2,6,5);
                if(i==0)Quad(0,1,5,4);if(i==11)Quad(3,7,6,2);
            }
            mesh=new Mesh{name="Curved_Tread_Brake_Shoe"};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        shoe.GetComponent<MeshFilter>().sharedMesh=mesh;shoe.GetComponent<Renderer>().sharedMaterial=friction;
        Link("Brake_Shoe_Hanger",new Vector3(x,-.54f,axleZ+direction*.57f),new Vector3(x,-.90f,axleZ+direction*.53f),.045f,.04f,spring,parent);
        Cylinder("Brake_Hanger_Cross_Support",new Vector3(side*.97f,-.54f,axleZ+direction*.57f),.065f,.40f,frame,parent,true);
        Cylinder("Brake_Hanger_Pin",new Vector3(x,-.54f,axleZ+direction*.57f),.038f,.15f,spring,parent,true);
        Box("Brake_Shoe_Backing",new Vector3(x,-.90f,axleZ+direction*.515f),new Vector3(.13f,.16f,.04f),frame,parent);
        Box("Brake_Actuator_Bracket",new Vector3(side*.98f,-.71f,axleZ+direction*.72f),new Vector3(.31f,.26f,.08f),frame,parent);
        Cylinder("Tread_Brake_Cylinder",new Vector3(x,-.87f,axleZ+direction*.81f),.12f,.14f,frame,parent,false);
        parent.GetChild(parent.childCount-1).localRotation=Quaternion.Euler(90,0,0);
        Link("Brake_Push_Rod",new Vector3(x,-.87f,axleZ+direction*.53f),new Vector3(x,-.87f,axleZ+direction*.74f),.022f,.022f,spring,parent);
    }
    static Mesh WheelMesh()
    {
        string path=Folder+"/Profiled_Driving_Wheel.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null){WheelUV(existing);return existing;}
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
        WheelUV(mesh);AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void WheelUV(Mesh mesh)
    {
        var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];
        for(int i=0;i<vertices.Length;i++){var p=vertices[i];uv[i]=new Vector2(Mathf.Atan2(p.z,p.y)/(2*Mathf.PI),Mathf.Sqrt(p.y*p.y+p.z*p.z)*2);}
        mesh.uv=uv;EditorUtility.SetDirty(mesh);
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
        int wheels=0,axles=0,bogies=0,shoes=0,seats=0,drives=0;
        foreach(var t in rootObj.GetComponentsInChildren<Transform>())
        {
            if(t.name=="Steel_Wheel")wheels++;if(t.name=="Driven_Wheelset")axles++;if(t.name.StartsWith("Powered_Bogie_"))bogies++;
            if(t.name=="Steel_Wheel"&&Mathf.Abs(t.position.y+.90f)>.001f)throw new InvalidOperationException("Wheel centre misaligned");
            if(t.name=="Curved_Tread_Brake_Shoe")shoes++;
            if(t.name=="Primary_Lower_Spring_Seat"||t.name=="Primary_Upper_Spring_Seat")seats++;
            if(t.name=="Non_Rotating_Axle_Drive")drives++;
            if(t.name=="Driven_Wheelset"&&t.Find("Axle_Gearcase")!=null)throw new InvalidOperationException("Gearcase would rotate with wheels");
        }
        if(wheels!=8||axles!=4||bogies!=2||GameObject.Find("Rear_Gangway_Door")==null)throw new InvalidOperationException("Incomplete car or wheelset count");
        if(shoes!=8||seats!=32||drives!=4)throw new InvalidOperationException("Missing suspension/brake/drive details");
        Debug.Log("NF5K_BOGIE_DETAIL_OK: 8 curved shoes, 32 spring seats, 4 non-rotating drive groups; static visual candidate");
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
