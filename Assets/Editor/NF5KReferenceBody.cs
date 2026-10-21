using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

// Reference artwork determines silhouette and fittings, not measured engineering dimensions.
public static class NF5KReferenceBody
{
    const string MeshFolder = "Assets/Models/Cab/NF5KBodyMeshes";
    static Transform root;
    static Material blue, cream, red, trim, glass, steel;

    public static void Build(Transform parent, Material body, Material light, Material stripe,
        Material seals, Material glazing, Material metal)
    {
        root = parent; blue = body; cream = light; red = stripe;
        trim = seals; glass = glazing; steel = metal;
        if (!AssetDatabase.IsValidFolder(MeshFolder))
            AssetDatabase.CreateFolder("Assets/Models/Cab", "NF5KBodyMeshes");
        var outline = new List<Vector2>();
        foreach(float z in new[]{17.5f,16.94f,16.06f,1.835f,1.145f,-.15f})outline.Add(new Vector2(-1.45f,z));
        for (int i = 1; i <= 12; i++)
        {
            float a = Mathf.PI + i * Mathf.PI / 24;
            outline.Add(new Vector2(-1.10f + .35f * Mathf.Cos(a), -.15f + .50f * Mathf.Sin(a)));
        }
        for(int i=1;i<=44;i++)
            outline.Add(new Vector2(-1.10f+2.20f*i/44,-.65f));
        for (int i = 1; i <= 12; i++)
        {
            float a = -Mathf.PI / 2 + i * Mathf.PI / 24;
            outline.Add(new Vector2(1.10f + .35f * Mathf.Cos(a), -.15f + .50f * Mathf.Sin(a)));
        }
        foreach(float z in new[]{1.145f,1.835f,16.06f,16.94f,17.5f})outline.Add(new Vector2(1.45f,z));
        Band("Lower_Blue_Shell", outline, .02f, .28f, blue);
        Band("Cream_Waist_Lower", outline, .28f, .35f, cream);
        Band("Red_Waist_Line", outline, .35f, .38f, red);
        Band("Cream_Waist_Upper", outline, .38f, .42f, cream);
        Band("Blue_Belt_Shell", outline, .42f, 1.08f, blue);
        Band("Window_Sill", outline, 1.08f, 1.14f, blue);
        Band("Blue_Cant_Rail", outline, 1.92f, 2.03f, blue);
        Band("Cream_Roof_Edge", outline, 2.03f, 2.10f, cream);
        var frontContour = outline.FindAll(p=>p.y<=-.149f);
        FrontChevron("Front_Cream_Chevron", frontContour, .071f, cream, -.009f);
        FrontChevron("Front_Red_Chevron", frontContour, .033f, red, -.012f);
        BuildNoseDetails(frontContour);

        // Facets are confined to the rounded shoulders; the main face remains broad and flat.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * .555f;
            RoundedPanel("Front_Glass_" + side, new Vector3(x, 1.53f, -.582f),
                1.045f, .72f, .065f, Quaternion.Euler(8, 0, 0), glass, trim);
            Cube("Front_Wiper_" + side, new Vector3(x + .13f, 1.52f, -.625f),
                new Vector3(.018f, .66f, .018f), steel, Quaternion.Euler(0, 0, -9));
            Cube("Front_Wiper_Pivot_" + side, new Vector3(x + .18f, 1.19f, -.64f),
                new Vector3(.065f, .035f, .035f), steel);
            Cube("Front_Grab_" + side, new Vector3(side * .77f, 1.10f, -.685f),
                new Vector3(.46f, .025f, .025f), cream);
            RoundedPanel("Front_Lamp_Box_" + side, new Vector3(side * .87f, .72f, -.68f),
                .40f, .24f, .025f, Quaternion.identity, trim, steel);
            foreach (float dx in new[] {-.09f, .09f})
            {
                var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lens.name = "Front_Lamp_Lens"; lens.transform.SetParent(root, false);
                lens.transform.position = new Vector3(side * .87f + dx, .72f, -.71f);
                lens.transform.rotation = Quaternion.Euler(90, 0, 0);
                float diameter = dx * side < 0 ? .085f : .145f;
                lens.transform.localScale = new Vector3(diameter, .012f, diameter);
                lens.GetComponent<Renderer>().sharedMaterial = cream;
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 24;
                var p = new Vector3(side * (1.10f + .35f * Mathf.Sin(a)), 1.53f,
                    -.15f - .50f * Mathf.Cos(a));
                var q = new Vector3(side * (1.10f + .35f * Mathf.Sin(a + Mathf.PI / 24)), 1.53f,
                    -.15f - .50f * Mathf.Cos(a + Mathf.PI / 24));
                Edge("Rounded_Cab_Shoulder", p, q, .04f, .79f, blue);
            }
            string name = side > 0 ? "Left" : "Right";
            foreach(var range in new[]{new Vector2(-.15f,1.145f),new Vector2(1.835f,16.06f),new Vector2(16.94f,17.5f)})
                Cube(name+"_Window_Sill_Pinstripe",new Vector3(side*1.453f,1.11f,(range.x+range.y)/2),new Vector3(.004f,.035f,range.y-range.x),cream);
            Quaternion sideFacing = Quaternion.Euler(0, 90, 0);
            RoundedPanel(name + "_Cab_Side_Window", new Vector3(side * 1.405f, 1.53f, .44f),
                1.03f, .72f, .075f, sideFacing, glass, trim);
            Cube(name + "_Window_Divider", new Vector3(side * 1.417f, 1.53f, .48f),
                new Vector3(.035f, .74f, .025f), blue);
            Cube(name + "_Door_Forward_Pillar", new Vector3(side * 1.42f, 1.53f, 1.025f),
                new Vector3(.07f, .80f, .15f), blue);
            Cube(name + "_Door_Rear_Pillar", new Vector3(side * 1.42f, 1.53f, 1.91f),
                new Vector3(.07f, .80f, .12f), blue);
            BuildDoor(side, name);
            // Outside skin below passenger windows; interior walls remain separate.
            for (int i = 0; i < 12; i++)
                RoundedPanel(name + "_Passenger_Window_" + i,
                    new Vector3(side * 1.442f, 1.50f, 2.8f + i * 1.1f),
                    .93f, .75f, .065f, sideFacing, glass, trim);
            Cube(name+"_Rear_Vestibule_Front_Panel",new Vector3(side*1.45f,1.53f,15.76f),new Vector3(.08f,.80f,.74f),blue);
            Cube(name+"_Rear_Vestibule_End_Panel",new Vector3(side*1.45f,1.53f,17.23f),new Vector3(.08f,.80f,.54f),blue);
            var rearDoor=new GameObject(name+"_Rear_Entry_Pivot");rearDoor.transform.SetParent(root,false);rearDoor.transform.position=new Vector3(side*1.47f,0,16.94f);
            foreach(float z in new[]{16.12f,16.88f})
                Cube(name+"_Rear_Entry_Stile",new Vector3(side*1.47f,1.53f,z),new Vector3(.06f,.80f,.09f),blue,Quaternion.identity,rearDoor.transform);
            Cube(name+"_Rear_Entry_Lower",new Vector3(side*1.47f,.58f,16.50f),new Vector3(.06f,1.10f,.85f),blue,Quaternion.identity,rearDoor.transform);
            Cube(name+"_Rear_Entry_Header",new Vector3(side*1.47f,1.94f,16.50f),new Vector3(.06f,.10f,.85f),blue,Quaternion.identity,rearDoor.transform);
            RoundedPanel(name+"_Rear_Entry_Window",new Vector3(side*1.477f,1.50f,16.5f),.65f,.68f,.055f,sideFacing,glass,trim,rearDoor.transform);
            PaintDoor(rearDoor.transform,side,16.50f,.82f);
            Cube(name+"_Rear_Entry_Inner_Lining",new Vector3(side*1.438f,.58f,16.50f),new Vector3(.003f,1.06f,.81f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_Blockout_Wall.mat"),Quaternion.identity,rearDoor.transform);
            Cube(name+"_Rear_Entry_Handle",new Vector3(side*1.52f,1.0f,16.18f),new Vector3(.025f,.035f,.12f),cream,Quaternion.identity,rearDoor.transform);
            rearDoor.AddComponent<CabSideDoor>().Configure(side*86);
            Cube(name+"_Rear_Entry_Threshold",new Vector3(side*1.56f,0,16.50f),new Vector3(.23f,.05f,.90f),steel);
        }
        Cube("Front_Center_Mullion", new Vector3(0, 1.53f, -.59f), new Vector3(.065f, .82f, .065f), blue);
        var roof = new GameObject("Roof"); roof.transform.SetParent(root, false);
        var verts = new List<Vector3>(); var triangles = new List<int>();
        var brow = new List<Vector3>(); var browTriangles = new List<int>();
        const int sections = 32;
        for (int i = 0; i <= sections; i++)
        {
            float a = -Mathf.PI / 2 + Mathf.PI * i / sections;
            float x = 1.45f * Mathf.Sin(a), y = 2.10f + .24f * Mathf.Cos(a);
            float shoulder = Mathf.Clamp01((Mathf.Abs(x) - 1.10f) / .35f);
            float lowerFront = -.15f - .50f * Mathf.Sqrt(1 - shoulder * shoulder);
            float front = lowerFront + .12f;
            verts.Add(new Vector3(x, y, front)); verts.Add(new Vector3(x, y, 17.5f));
            brow.Add(new Vector3(x, 2.08f, lowerFront)); brow.Add(new Vector3(x, y, front));
            if (i < sections)
            {
                Quad(triangles, i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);
                Quad(browTriangles, i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);
            }
        }
        MeshObject("Barrel_Roof", verts, triangles, cream, roof.transform);
        MeshObject("Curved_Front_Roof_Brow", brow, browTriangles, cream, roof.transform);
        Cylinder("Headlamp_Housing",new Vector3(0,2.43f,-.55f),.42f,.36f,blue,roof.transform);
        Cylinder("Headlamp_Bezel",new Vector3(0,2.43f,-.747f),.38f,.035f,steel,roof.transform);
        var lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder); lamp.name = "Roof_Headlamp_Lens";
        lamp.transform.SetParent(roof.transform); lamp.transform.position = new Vector3(0, 2.43f, -.775f);
        lamp.transform.rotation = Quaternion.Euler(90, 0, 0); lamp.transform.localScale = new Vector3(.31f, .016f, .31f);
        lamp.GetComponent<Renderer>().sharedMaterial = cream;
        NF5KRunningGear.Build(root);
        NF5KLivery.Build(root,blue,cream,red);
        BuildRearEnd();
        Debug.Log("NF5K_REFERENCE_BODY_OK: rounded shoulders, barrel roof, framed steel doors, grab rails and access steps");
    }

    static void PaintDoor(Transform door,int side,float z,float width)
    {
        float surface=z>10?1.502f:1.478f;
        Cube("Door_Cream_Lower_Band",new Vector3(side*surface,.315f,z),new Vector3(.002f,.07f,width),cream,Quaternion.identity,door);
        Cube("Door_Red_Band",new Vector3(side*surface,.365f,z),new Vector3(.002f,.03f,width),red,Quaternion.identity,door);
        Cube("Door_Cream_Upper_Band",new Vector3(side*surface,.40f,z),new Vector3(.002f,.04f,width),cream,Quaternion.identity,door);
        Cube("Door_Sill_Pinstripe",new Vector3(side*surface,1.11f,z),new Vector3(.002f,.035f,width),cream,Quaternion.identity,door);
    }

    static void BuildRearEnd()
    {
        foreach(float x in new[]{-.97f,.97f})
            Cube("Rear_End_Wall",new Vector3(x,1.06f,17.5f),new Vector3(.96f,2.12f,.09f),blue);
        var capVertices=new List<Vector3>();var capTriangles=new List<int>();
        for(int i=0;i<=32;i++)
        {
            float a=-Mathf.PI/2+Mathf.PI*i/32,x=1.45f*Mathf.Sin(a);
            capVertices.Add(new Vector3(x,2.03f,17.5f));capVertices.Add(new Vector3(x,2.10f+.24f*Mathf.Cos(a),17.5f));
            if(i<32)Quad(capTriangles,i*2,i*2+1,i*2+3,i*2+2);
        }
        MeshObject("Rear_Curved_Roof_Cap",capVertices,capTriangles,cream,root);
        Cube("Rear_Gangway_Door",new Vector3(0,.56f,17.51f),new Vector3(.88f,1.12f,.055f),blue);
        foreach(float x in new[]{-.36f,.36f})
            Cube("Rear_Door_Window_Stile",new Vector3(x,1.50f,17.51f),new Vector3(.16f,.76f,.055f),blue);
        Cube("Rear_Door_Window_Header",new Vector3(0,1.82f,17.51f),new Vector3(.88f,.12f,.055f),blue);
        RoundedPanel("Rear_Door_Window",new Vector3(0,1.43f,17.55f),.55f,.58f,.055f,Quaternion.identity,glass,trim);
        foreach(float x in new[]{-.51f,.51f})
            Cube("Rear_Gangway_Bellows",new Vector3(x,1.05f,17.67f),new Vector3(.12f,2.1f,.25f),trim);
        Cube("Rear_Gangway_Bellows_Top",new Vector3(0,2.10f,17.67f),new Vector3(1.14f,.12f,.25f),trim);
        Cube("Rear_Gangway_Plate",new Vector3(0,.01f,17.70f),new Vector3(1.05f,.055f,.35f),steel);
    }

    static void BuildNoseDetails(List<Vector2> contour)
    {
        var v=new List<Vector3>();var t=new List<int>();
        foreach(var p in contour)
        {
            v.Add(new Vector3(p.x,.025f,p.y));
            v.Add(new Vector3(p.x*.96f,-.18f,p.y+.045f));
            v.Add(new Vector3(p.x*.87f,-.61f,p.y+.19f));
        }
        for(int i=0;i<contour.Count-1;i++)for(int row=0;row<2;row++)
            Quad(t,i*3+row,(i+1)*3+row,(i+1)*3+row+1,i*3+row+1);
        MeshObject("Tapered_Nose_Skirt",v,t,blue,root);
        var apron=new List<Vector3>();var apronTriangles=new List<int>();
        var pinstripe=new List<Vector3>();var pinstripeTriangles=new List<int>();
        foreach(var p in contour)
        {
            apron.Add(new Vector3(p.x,.025f,p.y-.004f));
            apron.Add(new Vector3(p.x*.96f,-.18f,p.y+.041f));
            pinstripe.Add(new Vector3(p.x*.96f,-.18f,p.y+.037f));
            pinstripe.Add(new Vector3(p.x*.953f,-.21f,p.y+.047f));
        }
        for(int i=0;i<contour.Count-1;i++)
        {
            Quad(apronTriangles,i*2,i*2+1,i*2+3,i*2+2);
            Quad(pinstripeTriangles,i*2,i*2+1,i*2+3,i*2+2);
        }
        MeshObject("Cream_Lower_Apron",apron,apronTriangles,cream,root);
        MeshObject("Red_Apron_Pinstripe",pinstripe,pinstripeTriangles,red,root);
        foreach(int side in new[]{-1,1})
        {
            for(int i=0;i<3;i++)
                RoundedPanel("Skirt_Vent_"+side+"_"+i,new Vector3(side*.83f,-.24f-i*.12f,-.61f+i*.04f),
                    .43f,.042f,.018f,Quaternion.Euler(-18,0,0),trim,steel);
            Cube("Front_Buffer_Bracket_"+side,new Vector3(side*.52f,-.06f,-.72f),new Vector3(.11f,.13f,.09f),steel);
            Cube("Nose_Grab_Stanchion_"+side,new Vector3(side*.48f,.13f,-.72f),new Vector3(.025f,.11f,.035f),steel);
        }
        Cube("Nose_Grab_Bar",new Vector3(0,.18f,-.74f),new Vector3(.98f,.032f,.032f),steel);
        Cube("Coupler_Mount_Pad",new Vector3(0,-.12f,-.70f),new Vector3(.37f,.32f,.08f),steel);
        Cube("Coupler_Shank_Reserved",new Vector3(0,-.13f,-.87f),new Vector3(.14f,.14f,.28f),trim);
        // Coupler is a reserved envelope, not a functional knuckle mechanism.
        Cube("Coupler_Head_Reserved",new Vector3(0,-.12f,-1.03f),new Vector3(.24f,.23f,.14f),steel);
        RoundedPanel("Front_Number_Plate",new Vector3(0,.53f,-.682f),.56f,.20f,.018f,Quaternion.identity,trim,cream);
        RoundedPanel("Front_Fleet_Plate",new Vector3(0,.26f,-.682f),.32f,.12f,.012f,Quaternion.identity,trim,cream);
        Cylinder("Wing_Badge_Base",new Vector3(0,.92f,-.68f),.24f,.018f,cream);
        Cylinder("Wing_Badge_Field",new Vector3(0,.92f,-.695f),.20f,.012f,blue);
        var star=new List<Vector3>{new Vector3(0,.92f,-.706f)};var starTriangles=new List<int>();
        for(int i=0;i<10;i++)
        {
            float angle=(90+i*36)*Mathf.Deg2Rad,r=i%2==0?.087f:.039f;
            star.Add(new Vector3(Mathf.Cos(angle)*r,.92f+Mathf.Sin(angle)*r,-.706f));
        }
        for(int i=0;i<10;i++){starTriangles.Add(0);starTriangles.Add(i+1);starTriangles.Add((i+1)%10+1);}
        MeshObject("Red_Star_Badge",star,starTriangles,red,root);
        foreach(int side in new[]{-1,1})for(int row=0;row<3;row++)
            Cube("Badge_Wing",new Vector3(side*(.20f-row*.018f),.97f-row*.044f,-.69f),new Vector3(.18f-row*.03f,.022f,.012f),cream);
    }

    static GameObject Cylinder(string name,Vector3 position,float diameter,float depth,Material material,Transform parent=null)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(parent??root,false);
        obj.transform.position=position;obj.transform.rotation=Quaternion.Euler(90,0,0);
        obj.transform.localScale=new Vector3(diameter,depth/2,diameter);obj.GetComponent<Renderer>().sharedMaterial=material;
        return obj;
    }

    static void BuildDoor(int side, string name)
    {
        foreach(float z in new[]{1.125f,1.855f})
            Cube(name+"_Fixed_Door_Jamb",new Vector3(side*1.454f,.99f,z),new Vector3(.085f,1.94f,.035f),steel);
        Cube(name+"_Fixed_Door_Header",new Vector3(side*1.454f,1.96f,1.49f),new Vector3(.085f,.035f,.765f),steel);
        var assembly = new GameObject(name + "_Door_Pivot"); assembly.transform.SetParent(root, false);
        assembly.transform.position = new Vector3(side * 1.447f, 0, 1.84f);
        Quaternion facing = Quaternion.Euler(0, 90, 0);
        // Narrow steel leaf, rounded upper window, independent hinge and external access hardware.
        RoundedPanel(name + "_Door_Leaf", new Vector3(side * 1.448f, .98f, 1.49f),
            .67f, 1.89f, .055f, facing, blue, trim, assembly.transform);
        // The leaf mesh is built as a lower panel plus window surround, leaving a real opening.
        var leaf = assembly.transform.Find(name + "_Door_Leaf_Fill");
        if (leaf != null) Object.DestroyImmediate(leaf.gameObject);
        Cube(name + "_Door_Lower_Steel", new Vector3(side * 1.448f, .59f, 1.49f),
            new Vector3(.055f, 1.08f, .64f), blue, Quaternion.identity, assembly.transform);
        foreach (float z in new[] {1.205f, 1.775f})
            Cube(name + "_Door_Window_Stile", new Vector3(side * 1.448f, 1.50f, z),
                new Vector3(.055f, .77f, .09f), blue, Quaternion.identity, assembly.transform);
        Cube(name + "_Door_Top_Rail", new Vector3(side * 1.448f, 1.875f, 1.49f),
            new Vector3(.055f, .09f, .64f), blue, Quaternion.identity, assembly.transform);
        RoundedPanel(name + "_Door_Window", new Vector3(side * 1.452f, 1.49f, 1.49f),
            .47f, .70f, .065f, facing, glass, trim, assembly.transform);
        foreach (float y in new[] {.30f, 1.55f})
            Cube(name + "_Door_Hinge", new Vector3(side * 1.49f, y, 1.84f),
                new Vector3(.035f, .12f, .045f), steel);
        Cube(name + "_Door_Latch", new Vector3(side * 1.49f, 1.0f, 1.22f),
            new Vector3(.025f, .035f, .12f), cream, Quaternion.identity, assembly.transform);
        PaintDoor(assembly.transform,side,1.49f,.63f);
        var inner=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Interior_Blockout_Wall.mat");
        Cube(name+"_Door_Inner_Lining",new Vector3(side*1.417f,.59f,1.49f),new Vector3(.003f,1.04f,.60f),inner,Quaternion.identity,assembly.transform);
        Cube(name+"_Door_Inner_Handle",new Vector3(side*1.40f,.99f,1.24f),new Vector3(.024f,.025f,.10f),cream,Quaternion.identity,assembly.transform);
        Cube(name+"_Door_Lock_Case",new Vector3(side*1.414f,.97f,1.22f),new Vector3(.02f,.12f,.06f),steel,Quaternion.identity,assembly.transform);
        var bolt=Cube(name+"_Door_Lock_Bolt",new Vector3(side*1.448f,.97f,1.153f),new Vector3(.022f,.025f,.045f),steel,Quaternion.identity,assembly.transform);
        assembly.AddComponent<CabSideDoor>().Configure(side*86,bolt.transform);
        foreach (float z in new[] {1.04f, 1.94f})
        {
            Cube(name + "_Long_Grab_Rail", new Vector3(side * 1.53f, .99f, z),
                new Vector3(.027f, 1.57f, .027f), cream);
            foreach (float y in new[] {.23f, 1.75f})
                Cube(name + "_Grab_Rail_Bracket", new Vector3(side * 1.485f, y, z),
                    new Vector3(.09f, .025f, .025f), steel);
        }
        Cube(name + "_Door_Threshold", new Vector3(side * 1.51f, .025f, 1.49f),
            new Vector3(.22f, .05f, .73f), steel);
        for (int i = 0; i < 3; i++)
            Cube(name + "_Access_Step_" + i, new Vector3(side * (1.50f + .04f * i), -.24f - .24f * i, 1.49f),
                new Vector3(.24f, .04f, .66f), steel);
        foreach (float z in new[] {1.18f, 1.80f})
            Cube(name + "_Ladder_Stringer", new Vector3(side * 1.52f, -.37f, z),
                new Vector3(.05f, .78f, .04f), trim);
    }

    static void Band(string name, List<Vector2> contour, float bottom, float top, Material material)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        foreach (var p in contour)
        {
            v.Add(new Vector3(p.x, bottom, p.y));
            v.Add(new Vector3(p.x, top, p.y));
        }
        for (int i = 0; i < contour.Count - 1; i++)
        {
            float z=(contour[i].y+contour[i+1].y)/2;
            bool side=Mathf.Abs(contour[i].x)>1.449f&&Mathf.Abs(contour[i+1].x)>1.449f&&Mathf.Sign(contour[i].x)==Mathf.Sign(contour[i+1].x);
            bool opening=z>1.145f&&z<1.835f||z>16.06f&&z<16.94f;
            if(side&&opening&&bottom<1.92f)continue;
            Quad(t,i*2,i*2+2,i*2+3,i*2+1);
        }
        MeshObject(name, v, t, material, root);
    }
    static void FrontChevron(string name, List<Vector2> contour, float halfWidth, Material material, float offset)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        foreach (var p in contour)
        {
            float center = .57f + .47f * Mathf.Pow(Mathf.Sin(Mathf.Abs(p.x) / 1.45f * Mathf.PI / 2), .85f);
            v.Add(new Vector3(p.x, center - halfWidth, p.y + offset));
            v.Add(new Vector3(p.x, center + halfWidth, p.y + offset));
        }
        for (int i = 0; i < contour.Count - 1; i++) Quad(t, i * 2, i * 2 + 2, i * 2 + 3, i * 2 + 1);
        MeshObject(name, v, t, material, root);
    }
    static List<Vector3> Rounded(float width, float height, float radius)
    {
        var v = new List<Vector3>();
        for (int c = 0; c < 4; c++)
        {
            float cx = c == 0 || c == 3 ? width / 2 - radius : -width / 2 + radius;
            float cy = c < 2 ? height / 2 - radius : -height / 2 + radius;
            for (int i = 0; i <= 6; i++)
            {
                float a = (c * 90 + i * 15) * Mathf.Deg2Rad;
                v.Add(new Vector3(cx + radius * Mathf.Cos(a), cy + radius * Mathf.Sin(a), 0));
            }
        }
        return v;
    }
    static void RoundedPanel(string name, Vector3 position, float width, float height, float radius,
        Quaternion rotation, Material fill, Material border, Transform parent = null)
    {
        var inner = Rounded(width - .035f, height - .035f, Mathf.Max(.012f, radius - .0175f));
        var outer = Rounded(width + .035f, height + .035f, radius + .0175f);
        var verts = new List<Vector3>(); var tris = new List<int>();
        for (int i = 0; i < inner.Count; i++) { verts.Add(outer[i]); verts.Add(inner[i]); }
        for (int i = 0; i < inner.Count; i++) Quad(tris, i * 2, (i * 2 + 2) % verts.Count, (i * 2 + 3) % verts.Count, i * 2 + 1);
        var frame = MeshObject(name + "_Frame", verts, tris, border, parent ?? root);
        frame.transform.position = position; frame.transform.rotation = rotation;
        tris = new List<int>(); for (int i = 1; i < inner.Count - 1; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
        var pane = MeshObject(name + "_Fill", inner, tris, fill, parent ?? root);
        pane.transform.position = position; pane.transform.rotation = rotation;
    }
    static GameObject MeshObject(string name, List<Vector3> vertices, List<int> indices, Material material, Transform parent)
    {
        // Explicit reverse faces keep shell interiors visible without changing shared URP culling.
        vertices = new List<Vector3>(vertices);
        int count = indices.Count, offset = vertices.Count;
        vertices.AddRange(vertices.ToArray());
        for (int i = 0; i < count; i += 3) { indices.Add(indices[i] + offset); indices.Add(indices[i + 2] + offset); indices.Add(indices[i + 1] + offset); }
        string path = MeshFolder + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); } else mesh.Clear();
        mesh.name = name; mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }
    static void Quad(List<int> t, int a, int b, int c, int d) { t.Add(a); t.Add(b); t.Add(c); t.Add(a); t.Add(c); t.Add(d); }
    static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Quaternion rotation = default, Transform parent = null)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent ?? root, true);
        obj.transform.position = position; obj.transform.localScale = size;
        obj.transform.rotation = rotation == default ? Quaternion.identity : rotation;
        obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
    }
    static void Edge(string name, Vector3 a, Vector3 b, float depth, float height, Material material)
    {
        Cube(name, (a + b) / 2, new Vector3(Vector3.Distance(a, b) + .005f, height, depth), material,
            Quaternion.Euler(0, -Mathf.Atan2(b.z - a.z, b.x - a.x) * Mathf.Rad2Deg, 0));
    }
}
