using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
        outline.Add(new Vector2(-1.45f, 6.1f));
        outline.Add(new Vector2(-1.45f, -.15f));
        for (int i = 1; i <= 12; i++)
        {
            float a = Mathf.PI + i * Mathf.PI / 24;
            outline.Add(new Vector2(-1.10f + .35f * Mathf.Cos(a), -.15f + .50f * Mathf.Sin(a)));
        }
        foreach (float x in new[] {-.8f, -.4f, 0f, .4f, .8f, 1.10f})
            outline.Add(new Vector2(x, -.65f));
        for (int i = 1; i <= 12; i++)
        {
            float a = -Mathf.PI / 2 + i * Mathf.PI / 24;
            outline.Add(new Vector2(1.10f + .35f * Mathf.Cos(a), -.15f + .50f * Mathf.Sin(a)));
        }
        outline.Add(new Vector2(1.45f, 6.1f));
        Band("Lower_Blue_Shell", outline, .02f, .28f, blue);
        Band("Cream_Waist_Lower", outline, .28f, .35f, cream);
        Band("Red_Waist_Line", outline, .35f, .38f, red);
        Band("Cream_Waist_Upper", outline, .38f, .42f, cream);
        Band("Blue_Belt_Shell", outline, .42f, 1.08f, blue);
        Band("Window_Sill", outline, 1.08f, 1.14f, cream);
        Band("Blue_Cant_Rail", outline, 1.92f, 2.03f, blue);
        Band("Cream_Roof_Edge", outline, 2.03f, 2.10f, cream);
        var frontContour = outline.GetRange(1, outline.Count - 2);
        FrontChevron("Front_Cream_Chevron", frontContour, .071f, cream, -.009f);
        FrontChevron("Front_Red_Chevron", frontContour, .033f, red, -.012f);

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
            Cube("Front_Grab_" + side, new Vector3(side * .77f, 1.01f, -.685f),
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
            for (int i = 0; i < 3; i++)
                RoundedPanel(name + "_Passenger_Window_" + i,
                    new Vector3(side * 1.442f, 1.50f, 2.8f + i * 1.1f),
                    .93f, .75f, .065f, sideFacing, glass, trim);
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
            verts.Add(new Vector3(x, y, front)); verts.Add(new Vector3(x, y, 6.1f));
            brow.Add(new Vector3(x, 2.08f, lowerFront)); brow.Add(new Vector3(x, y, front));
            if (i < sections)
            {
                Quad(triangles, i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);
                Quad(browTriangles, i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);
            }
        }
        MeshObject("Barrel_Roof", verts, triangles, cream, roof.transform);
        MeshObject("Curved_Front_Roof_Brow", brow, browTriangles, cream, roof.transform);
        Cube("Headlamp_Housing", new Vector3(0, 2.35f, -.54f), new Vector3(.38f, .27f, .30f), blue, Quaternion.identity, roof.transform);
        var lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder); lamp.name = "Roof_Headlamp_Lens";
        lamp.transform.SetParent(roof.transform); lamp.transform.position = new Vector3(0, 2.37f, -.70f);
        lamp.transform.rotation = Quaternion.Euler(90, 0, 0); lamp.transform.localScale = new Vector3(.22f, .016f, .22f);
        lamp.GetComponent<Renderer>().sharedMaterial = cream;
        Debug.Log("NF5K_REFERENCE_BODY_OK: rounded shoulders, barrel roof, framed steel doors, grab rails and access steps");
    }

    static void BuildDoor(int side, string name)
    {
        var assembly = new GameObject(name + "_Door_Pivot"); assembly.transform.SetParent(root, false);
        assembly.transform.position = new Vector3(side * 1.447f, 0, 1.11f);
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
            Cube(name + "_Door_Hinge", new Vector3(side * 1.49f, y, 1.13f),
                new Vector3(.035f, .12f, .045f), steel);
        Cube(name + "_Door_Latch", new Vector3(side * 1.49f, 1.0f, 1.73f),
            new Vector3(.025f, .035f, .12f), cream, Quaternion.identity, assembly.transform);
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
        for (int i = 0; i < contour.Count - 1; i++) Quad(t, i * 2, i * 2 + 2, i * 2 + 3, i * 2 + 1);
        MeshObject(name, v, t, material, root);
    }
    static void FrontChevron(string name, List<Vector2> contour, float halfWidth, Material material, float offset)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        foreach (var p in contour)
        {
            float center = .79f + .23f * Mathf.Pow(Mathf.Abs(p.x) / 1.45f, .75f);
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
