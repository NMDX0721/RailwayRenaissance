using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KPassageDoorDetail
{
    public const float AxisX=-.44f, AxisZ=1.85f, OpeningWidth=.90f, LeafWidth=.86f, LeafHeight=1.87f;
    public const float HandleHeight=.90f, HandlePlateTop=.98f, HandleX=.38f;
    public static void Build(Transform fittings,Transform body,Material enamel,Material steel,Material rubber)
    {
        var door=body.GetComponentsInChildren<Transform>().First(t=>t.name=="Passenger_Passage_Door_Pivot");
        var prior=door.GetComponent<CabPassageDoor>();if(prior!=null)Object.DestroyImmediate(prior);
        foreach(var t in door.Cast<Transform>().Where(t=>t.name.StartsWith("Passage_Detail_")).ToArray())Object.DestroyImmediate(t.gameObject);
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
        void Resize(Transform t,Vector3 p,Vector3 size)
        {
            t.position=body.TransformPoint(p);t.rotation=body.rotation;t.localScale=size;
            t.GetComponent<MeshFilter>().sharedMesh=cube;var hit=t.GetComponent<BoxCollider>();if(hit!=null)hit.size=Vector3.one;
        }
        foreach(var wall in body.GetComponentsInChildren<Transform>().Where(t=>t.name=="Rear_Passenger_Partition"))
            Resize(wall,new Vector3(body.InverseTransformPoint(wall.position).x<0?-.925f:.925f,1.05f,1.91f),new Vector3(.95f,2.1f,.08f));
        Resize(body.GetComponentsInChildren<Transform>().First(t=>t.name=="Rear_Passenger_Door_Header"),new Vector3(0,2,1.91f),new Vector3(OpeningWidth,.20f,.08f));
        Resize(door.Find("Passenger_Passage_Door_Lower"),new Vector3(0,.51f,1.91f),new Vector3(LeafWidth,.98f,.045f));
        Resize(door.Find("Passenger_Passage_Door_Glass"),new Vector3(0,1.425f,1.91f),new Vector3(.68f,.75f,.009f));
        Resize(door.Find("Passenger_Door_Handle"),new Vector3(.31f,HandleHeight,1.8315f),new Vector3(.14f,.026f,.026f));
        Material Finish(string name,Color color,float metallic,float smoothness)
        {
            string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(m);return m;
        }
        var paint=Finish("Passage_Door_Satin_Enamel",new Color(.40f,.46f,.43f),.18f,.23f);
        var hardware=Finish("Passage_Door_Brushed_Steel",new Color(.48f,.50f,.49f),.9f,.30f);
        var plate=Finish("Passage_Action_Brushed_Aluminium",new Color(.67f,.70f,.67f),.65f,.38f);
        door.Find("Passenger_Passage_Door_Lower").GetComponent<Renderer>().sharedMaterial=paint;
        door.Find("Passenger_Door_Handle").GetComponent<Renderer>().sharedMaterial=hardware;
        GameObject Part(string name,Vector3 p,Vector3 size,Material material,bool moves=false,bool solid=false,PrimitiveType type=PrimitiveType.Cube)
        {
            var g=GameObject.CreatePrimitive(type);g.name="Passage_Detail_"+name;g.layer=30;g.transform.SetParent(body,false);g.transform.localPosition=p;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(moves?door:fittings,true);return g;
        }
        foreach(float x in new[]{-.3825f,.3825f})Part("Leaf_Stile",new Vector3(x,.955f,1.91f),new Vector3(.095f,LeafHeight,.045f),paint,true,true);
        Part("Leaf_Head_Rail",new Vector3(0,1.845f,1.91f),new Vector3(.67f,.09f,.045f),paint,true,true);
        Part("Leaf_Window_Sill",new Vector3(0,1.025f,1.91f),new Vector3(.67f,.05f,.045f),paint,true,true);
        foreach(float z in new[]{1.884f,1.936f})
        {
            foreach(float x in new[]{-.337f,.337f})Part("Window_Glazing_Bead",new Vector3(x,1.425f,z),new Vector3(.014f,.77f,.008f),rubber,true);
            foreach(float y in new[]{1.047f,1.803f})Part("Window_Glazing_Bead",new Vector3(0,y,z),new Vector3(.68f,.014f,.008f),rubber,true);
        }
        var glass=door.Find("Passenger_Passage_Door_Glass");
        var glassHit=glass.GetComponent<BoxCollider>();if(glassHit==null)glassHit=glass.gameObject.AddComponent<BoxCollider>();
        // Existing glass is a scaled primitive; its local collider must retain unit dimensions.
        glassHit.size=glass.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        foreach(float x in new[]{-.444f,.444f})
        {
            Part("Fixed_Jamb",new Vector3(x,.955f,1.91f),new Vector3(.012f,1.89f,.09f),hardware);
            Part("Jamb_Seal",new Vector3(x,.955f,1.932f),new Vector3(.010f,1.87f,.010f),rubber);
            foreach(float y in new[]{.16f,.92f,1.76f})Part("Jamb_Captive_Fastener",new Vector3(x,y,1.862f),new Vector3(.006f,.006f,.004f),hardware);
        }
        Part("Fixed_Head",new Vector3(0,1.901f,1.91f),new Vector3(OpeningWidth,.018f,.09f),hardware);
        foreach(float y in new[]{.27f,.94f,1.59f})
        {
            Part("Hinge_Fixed_Leaf",new Vector3(AxisX-.027f,y,1.86f),new Vector3(.052f,.085f,.02f),hardware);
            Part("Hinge_Moving_Leaf",new Vector3(AxisX+.026f,y,1.875f),new Vector3(.052f,.085f,.05f),hardware,true);
            Part("Hinge_Barrel",new Vector3(AxisX,y,AxisZ),new Vector3(.017f,.05f,.017f),hardware,false,false,PrimitiveType.Cylinder);
            foreach(float dy in new[]{-.026f,.026f})
            {
                Part("Hinge_Fixed_Fastener",new Vector3(AxisX-.037f,y+dy,1.848f),new Vector3(.008f,.008f,.004f),hardware);
                Part("Hinge_Moving_Fastener",new Vector3(AxisX+.039f,y+dy,1.848f),new Vector3(.008f,.008f,.004f),hardware,true);
            }
        }
        foreach(float z in new[]{1.883f,1.937f})
        {
            Part("Latch_Escutcheon",new Vector3(HandleX,.91f,z),new Vector3(.05f,.14f,.008f),plate,true);
            foreach(float y in new[]{.85f,.97f})Part("Handle_Plate_Fastener",new Vector3(HandleX,y,z+(z>1.91f?.005f:-.005f)),new Vector3(.006f,.006f,.002f),hardware,true);
            Part("Bottom_Kick_Plate",new Vector3(0,.15f,z),new Vector3(.74f,.20f,.006f),hardware,true);
        }
        Part("Rear_Handle",new Vector3(.31f,HandleHeight,1.9885f),new Vector3(.14f,.026f,.026f),hardware,true,true);
        foreach(float z in new[]{1.861f,1.959f})
        {
            var hub=Part("Lever_Rosette",new Vector3(HandleX,HandleHeight,z),new Vector3(.042f,.024f,.042f),hardware,true,false,PrimitiveType.Cylinder);
            hub.transform.rotation=body.rotation*Quaternion.Euler(90,0,0);
            Part("Lever_Return_End",new Vector3(.24f,HandleHeight,z<1.91f?1.8575f:1.9625f),new Vector3(.026f,.026f,.052f),hardware,true,true);
        }
        Part("Handle_Spindle",new Vector3(HandleX,HandleHeight,1.91f),new Vector3(.008f,.008f,.13f),hardware,true);
        Part("Hinge_Limit_Collar",new Vector3(AxisX,.21f,AxisZ),new Vector3(.025f,.008f,.025f),hardware,false,false,PrimitiveType.Cylinder);
        Part("Strike_Plate",new Vector3(.444f,HandleHeight,1.87f),new Vector3(.018f,.15f,.008f),hardware);
        Part("Latch_Recess",new Vector3(.433f,HandleHeight,1.909f),new Vector3(.005f,.025f,.013f),rubber);
        foreach(float x in new[]{-.375f,.375f})Part("Window_Bar_Standoff",new Vector3(x,1.29f,1.956f),new Vector3(.026f,.025f,.05f),hardware,true);
        Part("Window_Horizontal_Bar",new Vector3(0,1.29f,1.982f),new Vector3(.78f,.014f,.018f),hardware,true,true);
        foreach(float x in new[]{-.29f,-.10f,.25f})Part("Kick_Plate_Service_Scuff",new Vector3(x,.08f,1.941f),new Vector3(.027f,.0015f,.001f),rubber,true);
        void Print(string name,string image,Vector3 p,Vector2 size,bool front,bool transparent=false)
        {
            string full="Passage_Detail_"+name;
            NF5KEquipmentPrint.Add(door,full,image,door.InverseTransformPoint(body.TransformPoint(p)),size);
            var face=door.Find(full);face.localRotation=front?Quaternion.Euler(0,180,0):Quaternion.identity;
            var mat=face.GetComponent<Renderer>().sharedMaterial;
            mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            if(transparent)
            {
                mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.4f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;mat.SetOverrideTag("RenderType","TransparentCutout");
                face.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            EditorUtility.SetDirty(mat);
        }
        Print("Glass_Cab_Identity","Passage_Glass_Lettering_V1",new Vector3(0,1.57f,1.9152f),new Vector2(.29f,.091f),false,true);
        foreach(bool front in new[]{false,true})
        {
            float z=front?1.880f:1.941f;
            Part("Notice_Plate_Back",new Vector3(0,.87f,front?1.885f:1.936f),new Vector3(.346f,.116f,.008f),hardware,true);
            Print(front?"Clear_Notice":"Crew_Notice",front?"Passage_Clear_Plate_V1":"Passage_Crew_Plate_V1",new Vector3(0,.87f,z),new Vector2(.34f,.106f),front);
            // Ink belongs to the extended upper half of the handle backplate, on the solid stile.
            Print(front?"Pull_Instruction":"Push_Instruction",front?"Passage_Pull_Integrated_V4":"Passage_Push_Integrated_V4",new Vector3(HandleX,.9475f,front?1.8787f:1.9413f),new Vector2(.044f,.045f),front,true);
            foreach(float x in new[]{-.163f,.163f})foreach(float y in new[]{.822f,.918f})Part("Notice_Rivet",new Vector3(x,y,z+(front?-.001f:.001f)),new Vector3(.004f,.004f,.002f),hardware,true);
        }
        door.gameObject.AddComponent<CabPassageDoor>().Configure(body);
        NF5KIndustrialFinish.FinishRunningGear(door);
        Debug.Log("NF5K_PASSAGE_DETAIL_BUILT: framed leaf, glazing beads, hinge brackets, connected handles, kick plate and contextual swing; not a certified exit");
    }
}
