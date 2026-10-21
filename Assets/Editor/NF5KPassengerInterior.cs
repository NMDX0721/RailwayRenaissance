using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class NF5KPassengerInterior
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    static Transform root;
    static Material fabric,shell,steel,ivory,rubber,wood,bag;
    public static void Build(Transform parent)
    {
        root=new GameObject("Passenger_Interior_Fittings").transform;root.SetParent(parent,false);
        fabric=Mat("Passenger_Woven_Teal",new Color(.12f,.23f,.21f),0,.13f);
        shell=Mat("Passenger_Seat_Enamel",new Color(.30f,.34f,.31f),.15f,.23f);
        steel=Mat("Passenger_Satin_Steel",new Color(.43f,.45f,.43f),.78f,.31f);
        ivory=Mat("Passenger_Warm_Liner",new Color(.66f,.64f,.55f),0,.20f);
        rubber=Mat("Passenger_Rubber_Floor",new Color(.075f,.085f,.078f),0,.10f);
        wood=Mat("Passenger_Armrest_HPL",new Color(.25f,.15f,.095f),0,.21f);
        bag=Mat("Passenger_Canvas_Luggage",new Color(.31f,.24f,.15f),0,.12f);
        FabricWeave(fabric);FabricWeave(bag);
        foreach(int side in new[]{-1,1})
        {
            for(int module=0;module<4;module++)
                Box("Passenger_Lower_Liner",new Vector3(side*1.37f,.52f,3.74f+module*3.52f),new Vector3(.035f,1.04f,3.52f),ivory,.004f);
            Box("Passenger_Rear_End_Liner",new Vector3(side*1.37f,.52f,17.22f),new Vector3(.035f,1.04f,.56f),ivory,.004f);
            Box("Passenger_Window_Header",new Vector3(side*1.37f,2.02f,9.00f),new Vector3(.045f,.23f,14.12f),ivory,.005f);
            Box("Passenger_Front_Window_Infill",new Vector3(side*1.37f,1.50f,2.10f),new Vector3(.035f,.86f,.30f),ivory,.004f);
            Box("Passenger_Rear_Window_Infill",new Vector3(side*1.37f,1.50f,15.74f),new Vector3(.035f,.86f,.62f),ivory,.004f);
            for(int i=0;i<12;i++)
            {
                float z=2.8f+i*1.1f;
                WindowRing("Passenger_Inner_Window_Frame_"+side+"_"+i,side,z,1.075f,.86f,.075f, .96f,.79f,.05f,ivory,1.344f);
                WindowRing("Passenger_Inner_Window_Gasket_"+side+"_"+i,side,z,.96f,.79f,.05f,.925f,.745f,.045f,rubber,1.340f);
                WindowReturn(side,z,i);
                Box("Passenger_Window_Sill",new Vector3(side*1.325f,1.065f,z),new Vector3(.12f,.045f,1.075f),ivory,.012f);
                Box("Passenger_Window_Post",new Vector3(side*1.37f,1.52f,z+.55f),new Vector3(.045f,.93f,.085f),ivory,.006f);
                Box("Passenger_Heater_Cover",new Vector3(side*1.325f,.19f,z),new Vector3(.09f,.25f,.86f),shell,.007f);
                for(int vent=0;vent<4;vent++)
                    Box("Heater_Vent_Slot",new Vector3(side*1.275f,.115f+vent*.047f,z),new Vector3(.003f,.012f,.74f),rubber,.002f,false);
            }
            Rod("Passenger_Skirting_Rail",new Vector3(side*1.345f,.055f,1.98f),new Vector3(side*1.345f,.055f,16.03f),.012f,steel);
            for(int row=0;row<12;row++)SeatPair(side,row,2.85f+row*1.1f);
            Rack(side);
            foreach(float z in new[]{16.035f,16.965f})Box("Vestibule_Door_Reveal",new Vector3(side*1.365f,1.0f,z),new Vector3(.045f,1.98f,.035f),ivory,.004f);
            Rod("Vestibule_Grab_Vertical",new Vector3(side*1.20f,.75f,15.88f),new Vector3(side*1.20f,1.66f,15.88f),.017f,steel);
            foreach(float y in new[]{.75f,1.66f})Rod("Vestibule_Grab_Anchor",new Vector3(side*1.20f,y,15.88f),new Vector3(side*1.35f,y,15.88f),.013f,steel);
        }
        Box("Passenger_Aisle_Runner",new Vector3(0,.004f,9.0f),new Vector3(.60f,.008f,14.10f),rubber,.003f,false);
        Box("Rear_Vestibule_Mat",new Vector3(0,.005f,16.56f),new Vector3(2.60f,.01f,1.43f),rubber,.003f,false);
        foreach(float z in new[]{1.99f,15.81f,17.30f})Box("Threshold_Wear_Strip",new Vector3(0,.011f,z),new Vector3(.62f,.012f,.035f),steel,.003f,false);
        for(int i=0;i<3;i++)
        {
            int side=i%2==0?1:-1;float z=5.0f+i*3.5f;
            Box("Canvas_Travel_Bag",new Vector3(side*1.12f,1.98f,z),new Vector3(.30f,.22f,.45f),bag,.035f,false);
            foreach(float dz in new[]{-.14f,.14f})Box("Bag_Retaining_Straps",new Vector3(side*1.12f,2.094f,z+dz),new Vector3(.30f,.008f,.022f),wood,.003f,false);
            Rod("Bag_Handle",new Vector3(side*1.12f-.07f,2.10f,z),new Vector3(side*1.12f+.07f,2.10f,z),.008f,wood);
        }
        foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=30;
        Debug.Log("NF5K_PASSENGER_INTERIOR_BUILT: 48 individual seats, supported open luggage racks, window liners, heater covers, clear rear vestibule; candidate layout only");
    }
    static void SeatPair(int side,int row,float z)
    {
        Box("Passenger_Seat_Crossbeam",new Vector3(side*.855f,.34f,z+.06f),new Vector3(.98f,.065f,.065f),steel,.006f);
        foreach(float x in new[]{.58f,1.10f})
        {
            Box("Passenger_Seat_Leg",new Vector3(side*x,.175f,z+.075f),new Vector3(.055f,.29f,.085f),steel,.006f);
            Box("Passenger_Seat_Foot",new Vector3(side*x,.023f,z+.075f),new Vector3(.12f,.045f,.21f),steel,.005f);
            foreach(float dz in new[]{-.07f,.07f})Rod("Seat_Anchor_Bolt",new Vector3(side*x,.043f,z+.075f+dz),new Vector3(side*x,.057f,z+.075f+dz),.009f,steel);
        }
        foreach(float x in new[]{.60f,1.10f})
        {
            float sx=side*x;
            Box("Passenger_Seat_Pan",new Vector3(sx,.425f,z),new Vector3(.47f,.065f,.50f),shell,.023f);
            var cushion=Box("Passenger_Seat_Cushion",new Vector3(sx,.475f,z-.015f),new Vector3(.45f,.105f,.48f),fabric,.032f);
            cushion.transform.localRotation=Quaternion.Euler(-3,0,0);
            var back=Box("Passenger_Seat_Back",new Vector3(sx,.86f,z+.23f),new Vector3(.47f,.66f,.075f),shell,.027f);back.transform.localRotation=Quaternion.Euler(8,0,0);
            var pad=Box("Passenger_Back_Cushion",new Vector3(sx,.86f,z+.185f),new Vector3(.435f,.59f,.065f),fabric,.031f);pad.transform.localRotation=Quaternion.Euler(8,0,0);
            Box("Passenger_Headrest",new Vector3(sx,1.17f,z+.255f),new Vector3(.365f,.15f,.105f),fabric,.035f);
            Box("Passenger_Closed_Tray",new Vector3(sx,.84f,z+.294f),new Vector3(.34f,.26f,.018f),ivory,.014f);
            Box("Passenger_Tray_Latch",new Vector3(sx,.975f,z+.31f),new Vector3(.035f,.02f,.016f),steel,.003f);
            foreach(float dx in new[]{-.12f,.12f})Rod("Tray_Hinge",new Vector3(sx+dx-.022f,.705f,z+.31f),new Vector3(sx+dx+.022f,.705f,z+.31f),.008f,steel);
            Rod("Seat_Back_Support",new Vector3(sx,.34f,z+.16f),new Vector3(sx,1.11f,z+.30f),.016f,steel);
        }
        foreach(float x in new[]{.35f,.85f,1.34f})
        {
            Rod("Passenger_Arm_Support",new Vector3(side*x,.36f,z+.16f),new Vector3(side*x,.69f,z+.16f),.012f,steel);
            Box("Passenger_Armrest",new Vector3(side*x,.70f,z+.015f),new Vector3(.036f,.045f,.34f),wood,.014f);
        }
        Rod("Seatback_Grab_Top",new Vector3(side*.44f,1.22f,z+.32f),new Vector3(side*.73f,1.22f,z+.32f),.010f,steel);
        foreach(float x in new[]{.44f,.73f})Rod("Seatback_Grab_Anchor",new Vector3(side*x,1.16f,z+.28f),new Vector3(side*x,1.22f,z+.32f),.010f,steel);
    }
    static void Rack(int side)
    {
        Rod("Luggage_Rack_Lip",new Vector3(side*.855f,1.92f,2.25f),new Vector3(side*.855f,1.92f,15.45f),.014f,steel);
        for(int module=0;module<12;module++)
        {
            float z=2.25f+module*1.1f;
            foreach(float x in new[]{.90f,1.00f,1.10f,1.20f,1.30f})Rod("Luggage_Rack_Slat",new Vector3(side*x,1.855f,z),new Vector3(side*x,1.855f,z+1.08f),.012f,steel);
            Rod("Luggage_Rack_Transverse",new Vector3(side*.855f,1.855f,z),new Vector3(side*1.35f,1.855f,z),.014f,steel);
            Rod("Luggage_Rack_Wall_Strut",new Vector3(side*.855f,1.855f,z),new Vector3(side*1.35f,1.64f,z),.012f,steel);
            Box("Luggage_Rack_Anchor_Plate",new Vector3(side*1.348f,1.74f,z),new Vector3(.018f,.27f,.065f),steel,.004f);
            foreach(float y in new[]{1.64f,1.84f})Rod("Rack_Anchor_Bolt",new Vector3(side*1.33f,y,z),new Vector3(side*1.345f,y,z),.007f,steel);
        }
        Rod("Luggage_Rack_End_Strut",new Vector3(side*.855f,1.855f,15.45f),new Vector3(side*1.35f,1.64f,15.45f),.012f,steel);
        Box("Luggage_Rack_End_Anchor",new Vector3(side*1.348f,1.74f,15.45f),new Vector3(.018f,.27f,.065f),steel,.004f);
        foreach(float z in new[]{2.25f,15.45f})Rod("Luggage_Rack_End_Upstand",new Vector3(side*.855f,1.855f,z),new Vector3(side*.855f,1.92f,z),.014f,steel);
    }
    static void WindowRing(string name,int side,float z,float ow,float oh,float oradius,float iw,float ih,float iradius,Material mat,float x)
    {
        var outer=Rounded(ow,oh,oradius);var inner=Rounded(iw,ih,iradius);var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<outer.Count;i++){v.Add(new Vector3(side*x,1.50f+outer[i].y,z+outer[i].x));v.Add(new Vector3(side*x,1.50f+inner[i].y,z+inner[i].x));}
        for(int i=0;i<outer.Count;i++){int a=i*2,b=(i*2+2)%v.Count;t.AddRange(new[]{a,b,b+1,a,b+1,a+1});}
        MeshObject(name,v,t,mat);
    }
    static void WindowReturn(int side,float z,int index)
    {
        var inner=Rounded(.925f,.745f,.045f);var outer=Rounded(.905f,.725f,.0525f);var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<inner.Count;i++){v.Add(new Vector3(side*1.341f,1.50f+inner[i].y,z+inner[i].x));v.Add(new Vector3(side*1.411f,1.50f+outer[i].y,z+outer[i].x));}
        for(int i=0;i<inner.Count;i++){int a=i*2,b=(i*2+2)%v.Count;t.AddRange(new[]{a,b,b+1,a,b+1,a+1});}
        MeshObject("Passenger_Window_Return_"+side+"_"+index,v,t,ivory);
    }
    static List<Vector2> Rounded(float w,float h,float r)
    {
        var result=new List<Vector2>();for(int c=0;c<4;c++)for(int i=0;i<=8;i++){float a=(c*90+i*11.25f)*Mathf.Deg2Rad;result.Add(new Vector2((c==0||c==3?w/2-r:-w/2+r)+r*Mathf.Cos(a),(c<2?h/2-r:-h/2+r)+r*Mathf.Sin(a)));}return result;
    }
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,float radius,bool solid=true)
    {
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.transform.localPosition=p;
        obj.GetComponent<MeshFilter>().sharedMesh=RoundedBox(size,radius);obj.GetComponent<Renderer>().sharedMaterial=mat;
        if(solid)obj.AddComponent<BoxCollider>().size=size;return obj;
    }
    static Mesh RoundedBox(Vector3 size,float radius)
    {
        string path=Folder+"Passenger_Rounded_"+Hash128.Compute(size.ToString("F4")+radius.ToString("F4",System.Globalization.CultureInfo.InvariantCulture))+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();var half=size/2;radius=Mathf.Min(radius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.9f);var core=half-Vector3.one*radius;
        foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            var a=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;var b=Vector3.Cross(normal,a);
            float ha=Vector3.Dot(Abs(a),half),hb=Vector3.Dot(Abs(b),half),hn=Vector3.Dot(Abs(normal),half);
            float[] xs={-ha,-ha+radius*.293f,-ha+radius,ha-radius,ha-radius*.293f,ha};float[] ys={-hb,-hb+radius*.293f,-hb+radius,hb-radius,hb-radius*.293f,hb};int start=v.Count;
            for(int y=0;y<6;y++)for(int x=0;x<6;x++)
            {
                var p=normal*hn+a*xs[x]+b*ys[y];var q=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));var direction=(p-q).normalized;
                v.Add(q+direction*radius);n.Add(direction);uv.Add(new Vector2(xs[x]*4,ys[y]*4));
                if(x<5&&y<5){int k=start+y*6+x;t.AddRange(new[]{k,k+1,k+7,k,k+7,k+6});}
            }
        }
        mesh=new Mesh{name="Passenger_Rounded_Box"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Vector3 Abs(Vector3 p)=>new Vector3(Mathf.Abs(p.x),Mathf.Abs(p.y),Mathf.Abs(p.z));
    static void Rod(string name,Vector3 a,Vector3 b,float radius,Material mat)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=(a+b)/2;obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);obj.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2);obj.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    static void MeshObject(string name,List<Vector3> v,List<int> t,Material mat)
    {
        int vertexCount=v.Count,indexCount=t.Count;v.AddRange(v.ToArray());
        for(int i=0;i<indexCount;i+=3)t.AddRange(new[]{t[i]+vertexCount,t[i+2]+vertexCount,t[i+1]+vertexCount});
        string path=Folder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=mat;
    }
    static Material Mat(string name,Color color,float metal,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(mat);return mat;
    }
    static void FabricWeave(Material mat)
    {
        const string path=Folder+"Passenger_Fabric_Weave.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(tex==null)
        {
            const int size=128;tex=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="Passenger_Fabric_Weave",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};var colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){float level=.90f+.07f*Mathf.Sin(x*Mathf.PI/2)*Mathf.Sin(y*Mathf.PI/2)+.03f*Mathf.PerlinNoise(x*.6f,y*.6f);colors[y*size+x]=new Color(level,level,level,1);}
            tex.SetPixels(colors);tex.Apply();AssetDatabase.CreateAsset(tex,path);
        }
        mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.SetTexture("_BaseMap",tex);mat.SetTextureScale("_BaseMap",new Vector2(6,6));EditorUtility.SetDirty(mat);
    }
    public static void ValidateReload()
    {
        var root=GameObject.Find("Passenger_Interior_Fittings");if(root==null)throw new InvalidOperationException("Passenger interior missing on reload");
        int seats=0;float aisleHalf=10;
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())
        {
            if(renderer.name=="Passenger_Seat_Cushion")seats++;
            if(renderer.name=="Passenger_Armrest")aisleHalf=Mathf.Min(aisleHalf,Mathf.Abs(renderer.bounds.center.x)-renderer.bounds.extents.x);
        }
        if(seats!=48)throw new InvalidOperationException("Expected 48 individual seats, got "+seats);
        if(aisleHalf*2<.65f)throw new InvalidOperationException("Passenger aisle narrowed below candidate 650mm");
        foreach(var box in root.GetComponentsInChildren<BoxCollider>())
            if(box.name.StartsWith("Passenger_Seat_")||box.name=="Passenger_Armrest")
            {
                if(box.bounds.max.z>15.70f||box.bounds.min.z<2.40f)throw new InvalidOperationException("Seat extends into reserved vestibule");
                if(box.bounds.min.x<.32f&&box.bounds.max.x>-.32f)throw new InvalidOperationException("Seat obstructs center aisle");
            }
        Debug.Log("NF5K_PASSENGER_RELOAD_OK: 48 seats, aisle="+(aisleHalf*2)+"m, reserved vestibule clear; dimensions and loading are design candidates");
    }
}
