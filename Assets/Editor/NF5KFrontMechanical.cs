using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NF5KFrontMechanical
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    static Transform root;
    static Material cast,contact,rubber,metal,blue,cream,red;
    public static void Build(Transform parent,Material body,Material light,Material stripe,Material seals,Material steel)
    {
        root=new GameObject("Front_Mechanical_Assembly").transform;root.SetParent(parent,false);
        blue=body;cream=light;red=stripe;metal=steel;
        cast=Mat("Coupler_Cast_Steel",new Color(.19f,.18f,.16f),.72f,.24f);
        contact=Mat("Coupler_Contact_Steel",new Color(.43f,.44f,.43f),.92f,.44f);
        rubber=Mat("Brake_Hose_Rubber",new Color(.024f,.027f,.026f),0,.12f);
        // Visual load path: shank -> draft yoke -> longitudinal sill, independent of the cosmetic skirt.
        Box("Draft_Sill",new Vector3(0,-.41f,.04f),new Vector3(.36f,.30f,1.12f),cast);
        foreach(int side in new[]{-1,1})
        {
            Box("Draft_Yoke_"+side,new Vector3(side*.135f,-.40f,-.51f),new Vector3(.065f,.23f,.60f),cast);
            Box("Opening_Return_"+side,new Vector3(side*.28f,-.40f,-.54f),new Vector3(.035f,.43f,.18f),blue);
            Box("Carrier_Bracket_"+side,new Vector3(side*.225f,-.54f,-.68f),new Vector3(.065f,.20f,.18f),cast);
        }
        Box("Coupler_Carrier",new Vector3(0,-.61f,-.72f),new Vector3(.54f,.045f,.23f),metal);
        Box("Carrier_Wear_Pad",new Vector3(0,-.575f,-.74f),new Vector3(.33f,.025f,.18f),contact);
        Box("Coupler_Shank",new Vector3(0,-.42f,-.76f),new Vector3(.17f,.19f,.46f),cast);
        Box("Draft_Gear_Envelope",new Vector3(0,-.41f,-.12f),new Vector3(.24f,.22f,.43f),rubber);
        var head=new[]{new Vector2(-.11f,-.90f),new Vector2(.12f,-.90f),new Vector2(.23f,-1.00f),new Vector2(.22f,-1.10f),new Vector2(.10f,-1.11f),new Vector2(.03f,-1.03f),new Vector2(-.065f,-1.045f),new Vector2(-.095f,-1.16f),new Vector2(-.02f,-1.20f),new Vector2(-.02f,-1.28f),new Vector2(-.16f,-1.25f),new Vector2(-.235f,-1.15f),new Vector2(-.23f,-1.01f)};
        Extrude("Coupler_Cast_Head",head,-.545f,-.30f,cast);
        var knuckle=new[]{new Vector2(.21f,-.99f),new Vector2(.10f,-1.01f),new Vector2(.045f,-1.12f),new Vector2(.07f,-1.23f),new Vector2(-.015f,-1.25f),new Vector2(-.015f,-1.32f),new Vector2(.13f,-1.32f),new Vector2(.225f,-1.22f),new Vector2(.24f,-1.09f)};
        Extrude("Coupler_Knuckle",knuckle,-.53f,-.315f,cast);
        Rod("Knuckle_Vertical_Pin",new Vector3(.16f,-.57f,-1.05f),new Vector3(.16f,-.275f,-1.05f),.025f,contact);
        Box("Knuckle_Contact_Face",new Vector3(.055f,-.421f,-1.325f),new Vector3(.13f,.16f,.006f),contact);
        Rod("Lock_Lift_Pin",new Vector3(-.06f,-.29f,-.99f),new Vector3(-.06f,-.20f,-.99f),.012f,metal);
        Tube("Uncoupling_Rod",new[]{new Vector3(-.06f,-.20f,-.99f),new Vector3(-.06f,-.10f,-.86f),new Vector3(-.53f,-.10f,-.83f),new Vector3(-.65f,-.20f,-.83f)},.009f,metal);
        foreach(float x in new[]{-.51f,-.20f})Box("Uncoupling_Rod_Bearing",new Vector3(x,-.11f,-.79f),new Vector3(.04f,.06f,.08f),metal);
        Tube("Front_Nose_Grab_Rail",new[]{new Vector3(-.48f,.11f,-.70f),new Vector3(-.43f,.18f,-.76f),new Vector3(.43f,.18f,-.76f),new Vector3(.48f,.11f,-.70f)},.016f,metal);
        foreach(int side in new[]{-1,1})Rod("Grab_Rail_Anchor",new Vector3(side*.48f,.11f,-.65f),new Vector3(side*.48f,.11f,-.70f),.018f,metal);
        Hose();SkirtDetails();
        Debug.Log("NF5K_FRONT_MECHANICAL_BUILT: cast head, independent knuckle and pin, carrier, draft sill, isolated parked brake hose and service skirt");
    }
    static void Hose()
    {
        Rod("Brake_Pipe",new Vector3(.49f,.015f,-.66f),new Vector3(.49f,-.19f,-.73f),.018f,metal);
        Rod("Angle_Cock_Body",new Vector3(.49f,-.19f,-.73f),new Vector3(.49f,-.29f,-.82f),.034f,cast);
        Rod("Cock_Spindle",new Vector3(.49f,-.23f,-.79f),new Vector3(.55f,-.23f,-.79f),.011f,contact);
        // Handle across the flow direction: unconnected leading-end hose isolated, not venting the brake pipe.
        Box("Angle_Cock_Closed_Handle",new Vector3(.55f,-.23f,-.79f),new Vector3(.025f,.025f,.15f),red).transform.localRotation=Quaternion.FromToRotation(Vector3.forward,new Vector3(0,.09f,-.10f));
        Box("Cock_Handle_Stop",new Vector3(.535f,-.23f,-.72f),new Vector3(.025f,.045f,.025f),metal);
        var points=new List<Vector3>();
        var a=new Vector3(.49f,-.29f,-.82f);var b=new Vector3(.65f,-1.04f,-1.01f);var c=new Vector3(.02f,-1.01f,-1.10f);var d=new Vector3(.14f,-.63f,-1.07f);
        for(int i=0;i<=40;i++){float u=i/40f;points.Add(Mathf.Pow(1-u,3)*a+3*Mathf.Pow(1-u,2)*u*b+3*(1-u)*u*u*c+u*u*u*d);}
        Tube("Parked_Brake_Hose",points,.022f,rubber);
        Rod("Hose_Upper_Ferrule",a,a+(b-a).normalized*.075f,.028f,metal);
        Rod("Hose_Lower_Ferrule",d-(d-c).normalized*.06f,d,.028f,metal);
        Rod("Hose_Connector_Neck",d,d+new Vector3(0,.055f,-.015f),.023f,cast);
        Ring("Brake_Gladhand_Body",d+new Vector3(0,.07f,-.029f),.048f,.020f,metal);
        Ring("Brake_Gladhand_Seal",d+new Vector3(0,.07f,-.035f),.039f,.021f,rubber);
        Box("Gladhand_Locking_Lug",d+new Vector3(.04f,.07f,-.02f),new Vector3(.045f,.025f,.028f),metal);
        Rod("Hose_Parking_Bracket",new Vector3(.14f,-.56f,-.59f),d+new Vector3(0,.08f,0),.009f,metal);
        foreach(float y in new[]{-.05f,-.15f})Box("Brake_Pipe_Clamp",new Vector3(.49f,y,-.68f),new Vector3(.075f,.022f,.045f),metal);
    }
    static void SkirtDetails()
    {
        foreach(int side in new[]{-1,1})
        {
            var normal=new Vector3(0,-.38f,-.925f).normalized;
            foreach(float x in new[]{.57f,1.08f})
                Rod("Skirt_Service_Seam",new Vector3(side*x,-.22f,-.615f),new Vector3(side*x*.92f,-.56f,-.485f),.0025f,cast);
            for(int row=0;row<3;row++)
            {
                float y=-.24f-row*.12f,z=-.61f+row*.04f;
                Box("Vent_Internal_Baffle",new Vector3(side*.83f,y,z+.022f),new Vector3(.36f,.022f,.022f),cast);
                foreach(float x in new[]{.60f,1.06f})
                    Rod("Skirt_Captive_Fastener",new Vector3(side*x,y,z)-normal*.004f,new Vector3(side*x,y,z)+normal*.005f,.006f,metal);
            }
            Rod("Skirt_Lower_Return",new Vector3(side*.34f,-.605f,-.47f),new Vector3(side*1.13f,-.605f,-.47f),.012f,cast);
        }
        Box("Coupler_Opening_Top_Return",new Vector3(0,-.185f,-.60f),new Vector3(.58f,.025f,.15f),blue);
    }
    static GameObject Box(string name,Vector3 position,Vector3 size,Material material)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
    }
    static void Rod(string name,Vector3 a,Vector3 b,float radius,Material material)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=(a+b)/2;obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);obj.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2);obj.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    static void Ring(string name,Vector3 position,float outer,float inner,Material material)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<=48;i++){float a=i*Mathf.PI/24;v.Add(position+new Vector3(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer,0));v.Add(position+new Vector3(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner,0));if(i<48){int k=i*2;t.AddRange(new[]{k,k+1,k+3,k,k+3,k+2});}}
        MeshObject(name,v,t,material);
    }
    static void Tube(string name,IList<Vector3> points,float radius,Material material)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<points.Count;i++)
        {
            var tangent=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
            var axis=Vector3.Cross(tangent,Vector3.forward).normalized;if(axis.sqrMagnitude<.5f)axis=Vector3.Cross(tangent,Vector3.up).normalized;
            var second=Vector3.Cross(tangent,axis).normalized;
            for(int j=0;j<=16;j++){float a=j*Mathf.PI/8;v.Add(points[i]+radius*(Mathf.Cos(a)*axis+Mathf.Sin(a)*second));if(i<points.Count-1&&j<16){int k=i*17+j;t.AddRange(new[]{k,k+1,k+18,k,k+18,k+17});}}
        }
        MeshObject(name,v,t,material);
    }
    static void Extrude(string name,Vector2[] polygon,float bottom,float top,Material material)
    {
        // XZ polygons; side faces and ear-clipped caps preserve a genuine throat opening.
        var rounded=new List<Vector2>();
        for(int i=0;i<polygon.Length;i++)
        {
            var point=polygon[i];var prev=polygon[(i+polygon.Length-1)%polygon.Length];var next=polygon[(i+1)%polygon.Length];
            float cut=Mathf.Min(.012f,Mathf.Min(Vector2.Distance(point,prev),Vector2.Distance(point,next))*.22f);
            var a=Vector2.MoveTowards(point,prev,cut);var b=Vector2.MoveTowards(point,next,cut);
            for(int j=0;j<=3;j++){float u=j/3f;rounded.Add((1-u)*(1-u)*a+2*(1-u)*u*point+u*u*b);}
        }
        polygon=rounded.ToArray();
        float area=0;for(int i=0;i<polygon.Length;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Length]);
        if(area<0)Array.Reverse(polygon);
        var v=new List<Vector3>();var t=new List<int>();int count=polygon.Length;
        Vector2 center=Vector2.zero;foreach(var point in polygon)center+=point;center/=count;
        for(int ring=0;ring<4;ring++)foreach(var point in polygon)
        {
            var q=ring==0||ring==3?Vector2.Lerp(point,center,.025f):point;
            float y=ring==0?bottom:ring==1?bottom+.006f:ring==2?top-.006f:top;
            v.Add(new Vector3(q.x,y,q.y));
        }
        for(int ring=0;ring<3;ring++)for(int i=0;i<count;i++){int j=(i+1)%count,k=ring*count;t.AddRange(new[]{k+i,k+j,k+j+count,k+i,k+j+count,k+i+count});}
        var remaining=new List<int>();for(int i=0;i<count;i++)remaining.Add(i);
        while(remaining.Count>2)
        {
            bool found=false;
            for(int j=0;j<remaining.Count;j++)
            {
                int ia=remaining[(j+remaining.Count-1)%remaining.Count],ib=remaining[j],ic=remaining[(j+1)%remaining.Count];var a=polygon[ia];var b=polygon[ib];var c=polygon[ic];
                if(Cross(b-a,c-b)<=0)continue;bool inside=false;
                foreach(int k in remaining)if(k!=ia&&k!=ib&&k!=ic&&Cross(b-a,polygon[k]-a)>=0&&Cross(c-b,polygon[k]-b)>=0&&Cross(a-c,polygon[k]-c)>=0){inside=true;break;}
                if(inside)continue;t.AddRange(new[]{ia,ib,ic,ia+3*count,ic+3*count,ib+3*count});remaining.RemoveAt(j);found=true;break;
            }
            if(!found)throw new InvalidOperationException("Invalid cast coupler polygon");
        }
        // Duplicate each triangle for crisp cast-part planes; avoid rounding the silhouette into a toy.
        var flat=new List<Vector3>();var indices=new List<int>();foreach(int index in t){indices.Add(flat.Count);flat.Add(v[index]);}
        MeshObject(name,flat,indices,material);
    }
    static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
    static void MeshObject(string name,List<Vector3> v,List<int> t,Material material)
    {
        string path=Folder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    static Material Mat(string name,Color color,float metallic,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}if(name.StartsWith("Coupler_"))
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Bogie_Cast_Steel.mat");
            if(source!=null){material.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));material.SetTextureScale("_BaseMap",new Vector2(4,4));material.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));material.SetFloat("_BumpScale",.22f);material.EnableKeyword("_NORMALMAP");}
        }
        material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(material);return material;
    }
    public static void ValidateReload()
    {
        if(GameObject.Find("Coupler_Head_Reserved")!=null||GameObject.Find("Coupler_Shank_Reserved")!=null)throw new InvalidOperationException("Placeholder coupler survived");
        foreach(string name in new[]{"Coupler_Cast_Head","Coupler_Knuckle","Knuckle_Vertical_Pin","Coupler_Carrier","Draft_Sill","Parked_Brake_Hose","Angle_Cock_Closed_Handle","Brake_Gladhand_Seal"})if(GameObject.Find(name)==null)throw new InvalidOperationException("Missing mechanical fitting: "+name);
        var hose=GameObject.Find("Parked_Brake_Hose").GetComponent<MeshFilter>().sharedMesh;
        if(hose.bounds.min.y< -1.15f)throw new InvalidOperationException("Brake hose too close to nominal rail plane");
        Debug.Log("NF5K_FRONT_MECHANICAL_RELOAD_OK: independent cast parts, support path, isolated hose and nominal hose clearance; visual model only");
    }
}
