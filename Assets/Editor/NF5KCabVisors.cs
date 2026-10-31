using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabVisors
{
    public static void Build(Transform fittings,Transform body)
    {
        foreach(var t in body.GetComponentsInChildren<Transform>())
            if(t!=null&&t.name=="Driver_Sun_Visor_Reserved")Object.DestroyImmediate(t.gameObject);
        Material Finish(string name,Color color,float metal,float smooth)
        {
            string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        var cloth=Finish("Cab_Visor_Matte_Cloth",new Color(.19f,.26f,.23f),0,.12f);
        var rim=Finish("Cab_Visor_Bound_Edge",new Color(.095f,.13f,.115f),0,.18f);
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Service_Fitting_Steel.mat");
        var enamel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Service_Satin_Enamel.mat");
        GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool solid=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        void Rod(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.layer=30;g.transform.SetParent(parent,false);g.transform.localPosition=(a+b)*.5f;
            g.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        foreach(float x in new[]{-.555f,.555f})
        {
            var root=new GameObject(x>0?"Driver_Adjustable_Sun_Visor":"Assistant_Adjustable_Sun_Visor").transform;
            root.SetParent(fittings,false);root.localPosition=new Vector3(x,1.97f,-.47f);root.gameObject.layer=30;
            foreach(float side in new[]{-.37f,.37f})
            {
                Box(root,"Visor_Header_Anchor",new Vector3(side,.004f,-.071f),new Vector3(.052f,.045f,.016f),enamel);
                Rod(root,"Visor_StandOff",new Vector3(side,0,-.071f),new Vector3(side,0,0),.008f,steel);
                foreach(float y in new[]{-.009f,.018f})Box(root,"Visor_Captive_Anchor_Screw",new Vector3(side,y,-.061f),new Vector3(.009f,.009f,.005f),steel);
            }
            Rod(root,"Visor_Fixed_Axle",new Vector3(-.425f,0,0),new Vector3(.425f,0,0),.006f,steel);
            foreach(float side in new[]{-.417f,.417f})Rod(root,"Visor_Friction_End_Cap",new Vector3(side-.008f,0,0),new Vector3(side+.008f,0,0),.012f,rim);
            var leaf=new GameObject("Visor_Moving_Leaf").transform;leaf.SetParent(root,false);
            Box(leaf,"Visor_Core",new Vector3(0,-.09f,0),new Vector3(.80f,.18f,.012f),cloth,true);
            foreach(float side in new[]{-.397f,.397f})Box(leaf,"Visor_Bound_Side",new Vector3(side,-.09f,0),new Vector3(.006f,.18f,.015f),rim);
            Box(leaf,"Visor_Lower_Edge",new Vector3(0,-.177f,0),new Vector3(.80f,.006f,.015f),rim);
            Box(leaf,"Visor_Upper_Hinge_Sleeve",new Vector3(0,-.006f,0),new Vector3(.80f,.014f,.018f),rim);
            Box(leaf,"Visor_Adjustment_Tab",new Vector3(.30f,-.185f,.009f),new Vector3(.043f,.020f,.006f),rim);
            root.gameObject.AddComponent<CabSunVisor>().Configure(leaf,x>0);
        }
        Debug.Log("NF5K_CAB_VISORS_BUILT: two independently adjustable opaque visors with header anchors, axle and friction end caps");
    }
}
