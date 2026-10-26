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
        var edge=NF5KRunningGearFinish.Paint("Ladder_Muted_Ochre_Edge",new Color(.66f,.51f,.22f));
        Material Reflector(string id,Color color)
        {
            string path="Assets/Resources/Cab/"+id+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("RailwayRenaissance/RetroreflectiveSheet");
            if(shader==null)throw new System.InvalidOperationException("Reflective sheet shader missing");
            if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;mat.SetColor("_BaseColor",color);mat.SetFloat("_ReturnGain",2);EditorUtility.SetDirty(mat);return mat;
        }
        var white=Reflector("Ladder_Reflective_White",new Color(.75f,.78f,.73f));
        var amber=Reflector("Ladder_Reflective_Amber",new Color(.85f,.57f,.12f));
        var old=parent.Find(name+"_Fixed_Access_Ladder");if(old!=null)Object.DestroyImmediate(old.gameObject);
        foreach(var t in parent.GetComponentsInChildren<Transform>())
            if(t.name==name+"_Ladder_Stringer"||t.name.StartsWith(name+"_Access_Step_")
                ||t.name=="Worn_Step_Leading_Edge"&&Mathf.Sign(t.localPosition.x)==side)Object.DestroyImmediate(t.gameObject);
        var root=new GameObject(name+"_Fixed_Access_Ladder").transform;root.SetParent(parent,false);
        foreach(var t in parent.GetComponentsInChildren<Transform>())
        {
            if(t.name==name+"_Long_Grab_Rail")
            {
                var p=t.localPosition;p.x=side*1.59f;t.localPosition=p;t.localScale=new Vector3(.032f,.785f,.032f);
                var primitive=GameObject.CreatePrimitive(PrimitiveType.Cylinder);t.GetComponent<MeshFilter>().sharedMesh=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
                if(t.TryGetComponent<BoxCollider>(out var box))box.size=new Vector3(1,2,1);
            }
            if(t.name==name+"_Grab_Rail_Bracket")
            {var p=t.localPosition;p.x=side*1.505f;t.localPosition=p;var scale=t.localScale;scale.x=.18f;t.localScale=scale;}
        }
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
            Beam("Ladder_Diagonal_Brace",new Vector3(side*1.25f,-.26f,z),new Vector3(side*1.59f,-1.08f,z),.055f,.055f);
            Beam("Ladder_Stringer",new Vector3(side*1.50f,-.29f,z),new Vector3(side*1.59f,-1.13f,z),.045f,.055f);
            foreach(float y in new[]{-.12f,-.26f})foreach(float dz in new[]{-.052f,.052f})
            {
                var bolt=GameObject.CreatePrimitive(PrimitiveType.Cylinder);bolt.name=name+"_Ladder_Anchor_Bolt";bolt.transform.SetParent(root,false);
                bolt.transform.localPosition=new Vector3(side*1.275f,y,z+dz);bolt.transform.localRotation=Quaternion.Euler(0,0,90);bolt.transform.localScale=new Vector3(.03f,.018f,.03f);bolt.GetComponent<Renderer>().sharedMaterial=steel;
                bolt.GetComponent<MeshFilter>().sharedMesh=HexHead();
                Object.DestroyImmediate(bolt.GetComponent<Collider>());
                var washer=GameObject.CreatePrimitive(PrimitiveType.Cylinder);washer.name=name+"_Ladder_Anchor_Washer";washer.transform.SetParent(root,false);
                washer.transform.localPosition=new Vector3(side*1.264f,y,z+dz);washer.transform.localRotation=Quaternion.Euler(0,0,90);washer.transform.localScale=new Vector3(.041f,.002f,.041f);washer.GetComponent<Renderer>().sharedMaterial=steel;Object.DestroyImmediate(washer.GetComponent<Collider>());
            }
        }
        for(int i=0;i<4;i++)
        {
            float x=1.50f+.03f*i,y=-.26f-.28f*i;
            var step=new GameObject(name+"_Access_Step_"+i);step.transform.SetParent(root,false);step.transform.localPosition=new Vector3(side*x,y,1.49f);
            step.AddComponent<BoxCollider>().size=new Vector3(.24f,.04f,.66f);
            foreach(float z in new[]{1.169f,1.811f})Box("Tread_Perimeter",new Vector3(side*x,y,z),new Vector3(.24f,.035f,.018f),steel);
            foreach(float dx in new[]{-.111f,.111f})Box("Tread_Perimeter",new Vector3(side*(x+dx),y,1.49f),new Vector3(.018f,.035f,.66f),steel);
            for(int bar=0;bar<7;bar++)Box("Open_Grating_Bar",new Vector3(side*(x-.09f+bar*.03f),y,1.49f),new Vector3(.012f,.032f,.624f),steel);
            Box("Grating_Mid_Tie",new Vector3(side*x,y-.005f,1.49f),new Vector3(.21f,.02f,.012f),frame);
            foreach(float z in new[]{1.20f,1.78f})Box("Tread_Angle_Cleat",new Vector3(side*x,y-.035f,z),new Vector3(.23f,.04f,.055f),frame);
            Box("Tread_Worn_Safety_Edge",new Vector3(side*(x+.115f),y+.019f,1.49f),new Vector3(.010f,.003f,.61f),edge);
            foreach(float z in new[]{1.25f,1.73f})
            {
                Box("Passive_Reflective_Strip",new Vector3(side*(x+.122f),y-.023f,z),new Vector3(.002f,.025f,.10f),z<1.49f?white:amber);
                var strip=root.GetChild(root.childCount-1);Object.DestroyImmediate(strip.GetComponent<Collider>());
                strip.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Box("Tread_Front_Return",new Vector3(side*(x+.112f),y-.026f,1.49f),new Vector3(.016f,.045f,.66f),frame);
        }
        NF5KIndustrialFinish.FinishRunningGear(root);
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=parent.gameObject.layer;
    }
    static Mesh HexHead()
    {
        string path="Assets/Models/Cab/NF5KBodyMeshes/Ladder_Hex_Fastener.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
            var aa=new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f);var bb=new Vector3(Mathf.Cos(b)*.5f,-.5f,Mathf.Sin(b)*.5f);
            int k=v.Count;v.AddRange(new[]{aa,aa+Vector3.up,bb+Vector3.up,bb});t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            k=v.Count;v.AddRange(new[]{Vector3.up*.5f,bb+Vector3.up,aa+Vector3.up});t.AddRange(new[]{k,k+1,k+2});
            k=v.Count;v.AddRange(new[]{Vector3.down*.5f,aa,bb});t.AddRange(new[]{k,k+1,k+2});
        }
        mesh=new Mesh{name="Ladder_Hex_Fastener"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
