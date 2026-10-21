using UnityEditor;
using UnityEngine;

public static class NF5KAccessLadder
{
    public static void Build(Transform parent)
    {
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_NF5_Hardware.mat");
        var frame=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Bogie_Cast_Steel.mat");
        foreach(int side in new[]{-1,1})BuildSide(parent,side<0?"Left":"Right",side,steel,frame);
    }
    public static void BuildSide(Transform parent,string name,int side,Material steel,Material frame)
    {
        frame=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Bogie_Cast_Steel.mat")??frame;
        var old=parent.Find(name+"_Fixed_Access_Ladder");if(old!=null)Object.DestroyImmediate(old.gameObject);
        foreach(var t in parent.GetComponentsInChildren<Transform>())
            if(t.name==name+"_Ladder_Stringer"||t.name.StartsWith(name+"_Access_Step_"))Object.DestroyImmediate(t.gameObject);
        var root=new GameObject(name+"_Fixed_Access_Ladder").transform;root.SetParent(parent,false);
        void Box(string id,Vector3 p,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name+"_"+id;obj.transform.SetParent(root,false);obj.transform.localPosition=p;
            obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;
        }
        void Beam(string id,Vector3 a,Vector3 b,float width,float height)
        {
            Box(id,(a+b)/2,new Vector3(width,height,Vector3.Distance(a,b)),frame);
            root.GetChild(root.childCount-1).localRotation=Quaternion.LookRotation(b-a,Vector3.forward);
        }
        foreach(float z in new[]{1.18f,1.80f})
        {
            Box("Underframe_Anchor_Plate",new Vector3(side*1.24f,-.19f,z),new Vector3(.04f,.23f,.18f),frame);
            Box("Ladder_Upper_Cantilever",new Vector3(side*1.39f,-.24f,z),new Vector3(.34f,.07f,.105f),frame);
            Beam("Ladder_Diagonal_Brace",new Vector3(side*1.25f,-.26f,z),new Vector3(side*1.58f,-.70f,z),.055f,.055f);
            Beam("Ladder_Stringer",new Vector3(side*1.478f,-.11f,z),new Vector3(side*1.59f,-.78f,z),.065f,.065f);
            foreach(float y in new[]{-.12f,-.26f})foreach(float dz in new[]{-.052f,.052f})
            {
                var bolt=GameObject.CreatePrimitive(PrimitiveType.Cylinder);bolt.name=name+"_Ladder_Anchor_Bolt";bolt.transform.SetParent(root,false);
                bolt.transform.localPosition=new Vector3(side*1.267f,y,z+dz);bolt.transform.localRotation=Quaternion.Euler(0,0,90);bolt.transform.localScale=new Vector3(.026f,.009f,.026f);bolt.GetComponent<Renderer>().sharedMaterial=steel;
                Object.DestroyImmediate(bolt.GetComponent<Collider>());
            }
        }
        for(int i=0;i<3;i++)
        {
            float x=1.50f+.04f*i,y=-.24f-.24f*i;
            Box("Access_Step_"+i,new Vector3(side*x,y,1.49f),new Vector3(.24f,.04f,.66f),steel);
            foreach(float z in new[]{1.20f,1.78f})Box("Tread_Angle_Cleat",new Vector3(side*x,y-.035f,z),new Vector3(.23f,.04f,.055f),frame);
            for(int ridge=0;ridge<4;ridge++)Box("Tread_Anti_Slip_Rib",new Vector3(side*(x-.075f+ridge*.05f),y+.023f,1.49f),new Vector3(.008f,.006f,.61f),frame);
            Box("Tread_Front_Return",new Vector3(side*(x+.112f),y-.026f,1.49f),new Vector3(.016f,.045f,.66f),frame);
        }
        NF5KIndustrialFinish.FinishRunningGear(root);
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=parent.gameObject.layer;
    }
}
