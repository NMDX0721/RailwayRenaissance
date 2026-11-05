using UnityEditor;
using UnityEngine;

public static class NF5KPassageDoorFinish
{
    const string Folder="Assets/Resources/Cab/";
    public static void Apply(Transform pivot)
    {
        var body=pivot.root;
        foreach(var t in body.GetComponentsInChildren<Transform>(true))if(t.name=="Door_Stop_Seal")Object.DestroyImmediate(t.gameObject);
        foreach(var t in body.GetComponentsInChildren<Transform>(true))
            if(t.name=="Rear_Passenger_Partition") {
                var p=body.InverseTransformPoint(t.position);p.x=Mathf.Sign(p.x)*.90f;
                t.position=body.TransformPoint(p);t.localScale=new Vector3(1.0f,2.1f,.08f);
            }
        var header=body.Find("Rear_Assembly/Rear_Passenger_Door_Header");
        if(header!=null)header.localScale=new Vector3(.80f,.20f,.08f);
        var leaf=Material("Passage_Enamel",new Color(.29f,.38f,.36f),.12f,.24f);
        var frame=Material("Passage_Frame",new Color(.47f,.52f,.47f),.25f,.28f);
        var rubber=Material("Passage_Seal",new Color(.055f,.065f,.06f),0,.10f);
        var metal=Material("Passage_Hardware",new Color(.48f,.49f,.45f),.65f,.32f);
        var original=pivot.Find("Passenger_Passage_Door_Glass").GetComponent<Renderer>().sharedMaterial;
        string path=Folder+"Passage_Glass.mat";
        var glass=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(glass==null){glass=new Material(original);AssetDatabase.CreateAsset(glass,path);}
        glass.color=new Color(.63f,.73f,.70f,.16f);
        glass.SetFloat("_Smoothness",.08f);
        glass.SetFloat("_SpecularHighlights",0);glass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        glass.SetFloat("_EnvironmentReflections",0);glass.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        EditorUtility.SetDirty(glass);
        Transform details=pivot.Find("Passage_Finish");
        if(details!=null)Object.DestroyImmediate(details.gameObject);
        details=new GameObject("Passage_Finish").transform;details.SetParent(pivot,false);
        void Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
            obj.transform.position=body.TransformPoint(p);obj.transform.rotation=body.rotation;obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=mat;obj.layer=pivot.gameObject.layer;
            obj.GetComponent<MeshFilter>().sharedMesh=NF5KPassengerInterior.RoundedBox(size,.002f);
            obj.transform.localScale=Vector3.one;obj.GetComponent<BoxCollider>().size=size;
        }
        void Existing(string name,Vector3 p,Vector3 size,Material mat)
        {
            var t=pivot.Find(name);t.position=body.TransformPoint(p);t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=mat;
            if(name!="Passenger_Passage_Door_Glass"){
                t.GetComponent<MeshFilter>().sharedMesh=NF5KPassengerInterior.RoundedBox(size,.003f);
                t.localScale=Vector3.one;t.GetComponent<BoxCollider>().size=size;
            }
        }
        Existing("Passenger_Passage_Door_Lower",new Vector3(0,.525f,1.91f),new Vector3(.776f,.99f,.045f),leaf);
        Existing("Passenger_Passage_Door_Glass",new Vector3(0,1.40f,1.91f),new Vector3(.59f,.70f,.009f),glass);
        var oldHandle=pivot.Find("Passenger_Door_Handle");if(oldHandle!=null)Object.DestroyImmediate(oldHandle.gameObject);
        foreach(int side in new[]{-1,1})
        {
            Box("Leaf_Stile",new Vector3(side*.346f,1.445f,1.91f),new Vector3(.084f,.85f,.045f),leaf,details);
            Box("Window_Seal",new Vector3(side*.300f,1.40f,1.91f),new Vector3(.015f,.72f,.021f),rubber,details);
            Box("Window_Bead",new Vector3(side*.311f,1.40f,1.91f),new Vector3(.010f,.75f,.029f),frame,details);
        }
        Box("Leaf_Head",new Vector3(0,1.81f,1.91f),new Vector3(.776f,.12f,.045f),leaf,details);
        Box("Leaf_Window_Sill",new Vector3(0,1.035f,1.91f),new Vector3(.776f,.03f,.045f),leaf,details);
        foreach(float y in new[]{1.0475f,1.7525f})
            Box("Window_Seal",new Vector3(0,y,1.91f),new Vector3(.61f,.015f,.021f),rubber,details);
        foreach(float z in new[]{1.881f,1.939f})
        {
            Box("Kick_Plate",new Vector3(0,.17f,z),new Vector3(.66f,.19f,.006f),metal,details);
        }
        void Rod(string name,Vector3 a,Vector3 b,float diameter,Transform parent)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(parent,false);
            obj.transform.position=body.TransformPoint((a+b)*.5f);
            obj.transform.rotation=body.rotation*Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            obj.transform.localScale=new Vector3(diameter,(b-a).magnitude*.5f,diameter);
            Object.DestroyImmediate(obj.GetComponent<Collider>());obj.AddComponent<BoxCollider>();
            obj.GetComponent<Renderer>().sharedMaterial=metal;obj.layer=pivot.gameObject.layer;
        }
        foreach(int face in new[]{-1,1})
        {
            Rod("Handle_Rose",new Vector3(.29f,.965f,1.91f+face*.023f),new Vector3(.29f,.965f,1.91f+face*.030f),.050f,details);
            Rod("Lock_Rose",new Vector3(.29f,.885f,1.91f+face*.023f),new Vector3(.29f,.885f,1.91f+face*.027f),.034f,details);
            Box("Key_Slot",new Vector3(.29f,.885f,1.91f+face*.028f),new Vector3(.004f,.016f,.001f),rubber,details);
            MakeLever(details,body,face,metal);
        }
        foreach(float y in new[]{.36f,1.02f,1.69f})
        {
            Box("Hinge_Leaf",new Vector3(-.367f,y,1.883f),new Vector3(.037f,.10f,.008f),metal,details);
            Rod("Hinge_Knuckle",new Vector3(-.385f,y-.044f,1.876f),new Vector3(-.385f,y+.044f,1.876f),.024f,details);
        }
        // A rear stop seals the visible reveal without entering the inward leaf sweep.
        foreach(int side in new[]{-1,1})
            Box("Door_Stop_Seal",new Vector3(side*.394f,.96f,1.946f),new Vector3(.012f,1.87f,.012f),rubber,body);

        var oldFrame=body.Find("Passage_Fixed_Frame");if(oldFrame!=null)Object.DestroyImmediate(oldFrame.gameObject);
        var fixedFrame=new GameObject("Passage_Fixed_Frame").transform;fixedFrame.SetParent(body,false);
        foreach(int side in new[]{-1,1})
            Box("Fixed_Jamb",new Vector3(side*.423f,.96f,1.91f),new Vector3(.046f,1.88f,.095f),frame,fixedFrame);
        Box("Fixed_Header",new Vector3(0,1.924f,1.91f),new Vector3(.892f,.044f,.095f),frame,fixedFrame);
        foreach(float y in new[]{.36f,1.02f,1.69f})
            Box("Hinge_Fixed_Leaf",new Vector3(-.430f,y,1.91f),new Vector3(.045f,.08f,.012f),metal,fixedFrame);
    }
    static void MakeLever(Transform parent,Transform body,int face,Material material)
    {
        // Continuous swept elbow; the short return ends towards the door rather than sticking out.
        var points=new System.Collections.Generic.List<Vector3>();
        points.Add(new Vector3(.29f,1,1.91f+face*.028f));
        points.Add(new Vector3(.29f,1,1.91f+face*.040f));
        for(int i=0;i<=10;i++){
            float a=i*Mathf.PI/20;points.Add(new Vector3(.270f+.020f*Mathf.Cos(a),1,1.91f+face*(.040f+.020f*Mathf.Sin(a))));
        }
        points.Add(new Vector3(.235f,1,1.91f+face*.060f));points.Add(new Vector3(.190f,1,1.91f+face*.060f));
        for(int i=1;i<=8;i++){
            float a=i*Mathf.PI/16;points.Add(new Vector3(.190f-.010f*Mathf.Sin(a),1,1.91f+face*(.050f+.010f*Mathf.Cos(a))));
        }
        for(int i=0;i<points.Count;i++)points[i]+=Vector3.down*.035f;
        var v=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
        Vector3 origin=points[0];const int sides=16;
        for(int i=0;i<points.Count;i++){
            var tangent=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
            var right=Vector3.Cross(tangent,Vector3.up).normalized;
            float radius=i==points.Count-1?.003f:.008f;
            for(int j=0;j<sides;j++){
                float a=j*Mathf.PI*2/sides;v.Add(points[i]-origin+radius*(right*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a)));
                if(i>0){int p=i*sides+j,q=i*sides+(j+1)%sides;triangles.AddRange(new[]{p,q,p-sides,q,q-sides,p-sides});}
            }
        }
        for(int j=1;j<sides-1;j++){triangles.AddRange(new[]{0,j+1,j});int p=(points.Count-1)*sides;triangles.AddRange(new[]{p,p+j,p+j+1});}
        string path="Assets/Models/Cab/NF5KBodyMeshes/Passage_Continuous_Lever_"+face+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var obj=new GameObject("Continuous_Lever",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.transform.position=body.TransformPoint(origin);obj.transform.rotation=body.rotation;obj.layer=parent.parent.gameObject.layer;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;
        var collider=obj.AddComponent<BoxCollider>();collider.center=mesh.bounds.center;collider.size=mesh.bounds.size;
    }
    static Material Material(string name,Color color,float metallic,float smoothness)
    {
        string path=Folder+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=color;mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(mat);return mat;
    }
}
