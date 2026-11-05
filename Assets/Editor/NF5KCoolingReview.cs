using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailwayRenaissance.Cab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class NF5KCoolingReview
{
    const string ScenePath = "Assets/Scenes/CabCoolingReview.unity";
    const string Output = "tools/cab_reference/cooling_detail/";

    [Serializable]
    sealed class Report
    {
        public int poses, meshes;
        public Vector3 sizeM;
        public string[] conflicts;
    }

    [MenuItem("Railway/NF-5K/Build Cooling Review")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        var source = EditorSceneManager.OpenScene("Assets/Scenes/CabFuelTankReview.unity");
        if (!EditorSceneManager.SaveScene(source, ScenePath, true))
            throw new InvalidOperationException("Could not create the cooling review scene.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var body = GameObject.Find("CabInterior_Blockout").transform;
        var root = GameObject.Find("Cooling_Pack").transform;
        Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(root.GetComponent<MeshFilter>());
        var proxy = root.GetComponent<BoxCollider>();
        root.localScale = Vector3.one;
        proxy.size = new Vector3(1.80f, .58f, 1f);
        GameObject.Find("Cooling_Pack_Service_Direction").SetActive(false);
        var driver = Object.FindAnyObjectByType<CabSimulationDriver>();
        if (driver == null)
            throw new InvalidOperationException("Cooling review requires the vehicle simulation driver.");
        BuildModule(root, driver);
        var motion = Object.FindAnyObjectByType<CabRailMotion>();
        var obstacles = body.GetComponentsInChildren<Collider>()
            .Where(c => !c.isTrigger && c != proxy).ToArray();
        var conflicts = new HashSet<string>();
        int poses = 0;
        foreach (double station in new[] { 0d, 35, 60, 100, 145, 170 })
        foreach (double speed in new[] { -3d, 0, 3 })
        {
            motion.Apply(station, speed, .3, .5, 0, 20261021, true);
            Physics.SyncTransforms();
            foreach (var obstacle in obstacles)
                if (proxy.bounds.Intersects(obstacle.bounds) &&
                    Physics.ComputePenetration(proxy, proxy.transform.position, proxy.transform.rotation,
                        obstacle, obstacle.transform.position, obstacle.transform.rotation, out _, out float depth) &&
                    depth > .002f)
                    conflicts.Add($"{station}m/{speed}m/s {obstacle.name}: {depth:F4}m");
            poses++;
        }
        motion.Apply(0, 0, 0, 0, 0, 20261021, true);
        var filters = root.GetComponentsInChildren<MeshFilter>();
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var filter in filters)
        {
            var meshBounds = filter.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                var sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                bounds.Encapsulate(root.InverseTransformPoint(filter.transform.TransformPoint(
                    meshBounds.center + Vector3.Scale(meshBounds.extents, sign))));
            }
        }
        File.WriteAllText(Output + "acceptance.json", JsonUtility.ToJson(new Report
        {
            poses = poses, meshes = filters.Length, sizeM = bounds.size, conflicts = conflicts.OrderBy(s => s).ToArray()
        }, true));
        if (conflicts.Count != 0 ||
            Mathf.Max(Mathf.Abs(bounds.min.x), bounds.max.x) > .9f ||
            Mathf.Max(Mathf.Abs(bounds.min.y), bounds.max.y) > .29f ||
            Mathf.Max(Mathf.Abs(bounds.min.z), bounds.max.z) > .5f)
            throw new InvalidOperationException("Cooling geometry exceeds the reserved envelope or the envelope intersects vehicle hardware.");

        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save cooling geometry.");
        scene = EditorSceneManager.OpenScene(ScenePath);
        root = GameObject.Find("Cooling_Pack").transform;
        if (root.GetComponentsInChildren<MeshFilter>().Length != filters.Length ||
            root.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh == null))
            throw new InvalidOperationException("Cooling mesh references did not survive reload.");
        var duct = root.Find("Removable_Plenum_Panels");
        CaptureReview(root, duct);
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log($"NF5K_COOLING_REVIEW_OK: {poses} stowed poses, {filters.Length} meshes, {conflicts.Count} external conflicts.");
    }

    public static void Batch()
    {
        try
        {
            Build();
            SessionState.SetBool(PlayCheckKey, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    const string PlayCheckKey = "NF5KCoolingReview.PlayCheck";

    [InitializeOnLoadMethod]
    static void RegisterPlayCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PlayCheckKey, false))
                return;

            SessionState.EraseBool(PlayCheckKey);
            try
            {
                var driver = Object.FindAnyObjectByType<CabSimulationDriver>();
                driver.SimulationPaused = true;
                driver.EnsureInitialized();
                var fans = Object.FindObjectsByType<CabCoolingFanRotor>();
                var rotors = fans.Select(f => f.transform).ToArray();
                var model = driver.Model;
                void SetCoolant(double temperature)
                {
                    model.State.CoolantC = temperature;
                    model.Step(driver.Input);
                }
                void ExpectRpm(double expected)
                {
                    if (driver.CoolingFanTargetRpm != expected)
                        throw new InvalidOperationException($"Expected {expected} rpm, got {driver.CoolingFanTargetRpm}.");
                }

                const double rated = RailwayRenaissance.Cab.Simulation.CabSimulation.CoolingFanRatedRpm;
                SetCoolant(25);
                ExpectRpm(0);
                SetCoolant(81);
                ExpectRpm(0);
                SetCoolant(85);
                ExpectRpm(rated);
                SetCoolant(81);
                ExpectRpm(rated);
                SetCoolant(75);
                ExpectRpm(0);
                SetCoolant(85);
                ExpectRpm(rated);
                if (rotors.Length != 2 || rotors.Any(r => r.childCount != 9))
                    throw new InvalidOperationException("Expected two rotor assemblies, each with a hub, cap and seven blades.");

                var guards = rotors.Select(r => r.parent.Find("Concentric_Guard")).ToArray();
                var initial = rotors.Select(r => r.localRotation).ToArray();
                var fixedAngles = guards.Select(g => g.localRotation).ToArray();
                double started = EditorApplication.timeSinceStartup;
                bool faultInjected = false;
                EditorApplication.CallbackFunction poll = null;
                poll = () =>
                {
                    try
                    {
                        if (EditorApplication.timeSinceStartup - started > 15)
                            throw new InvalidOperationException("Cooling fan Play check timed out.");
                        if (!faultInjected)
                        {
                            if (fans.Any(f => f.VisualRpm < rated))
                                return;

                            bool moved = rotors.Select((r, i) =>
                                Quaternion.Angle(initial[i], r.localRotation) > 1f &&
                                Vector3.Angle(r.localRotation * Vector3.right, Vector3.right) < .01f &&
                                Quaternion.Angle(fixedAngles[i], guards[i].localRotation) < .01f).All(ok => ok);
                            if (!moved)
                                throw new InvalidOperationException("A rotor did not turn about its axis, or a guard moved.");

                            driver.Input.CoolingFailure = true;
                            SetCoolant(85);
                            ExpectRpm(0);
                            faultInjected = true;
                            return;
                        }

                        if (fans.Any(f => f.VisualRpm > 0f))
                            return;
                        EditorApplication.update -= poll;
                        Debug.Log("NF5K_COOLING_FAN_PLAY_OK: cold/off, hot/1450 rpm, hysteresis, two rotating rotors, fixed guards, fault/off.");
                        EditorApplication.Exit(0);
                    }
                    catch (Exception e)
                    {
                        EditorApplication.update -= poll;
                        Debug.LogException(e);
                        EditorApplication.Exit(1);
                    }
                };
                EditorApplication.update += poll;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        };
    }

    static void BuildModule(Transform root, CabSimulationDriver driver)
    {
        var frame = MaterialFor("Frame", new Color(.30f, .32f, .32f), 0f, .31f);
        var alloy = MaterialFor("Alloy", new Color(.56f, .57f, .57f), .72f, .30f);
        var core = MaterialFor("Core", new Color(.41f, .42f, .42f), .63f, .25f);
        var black = MaterialFor("Elastomer", new Color(.075f, .073f, .071f), 0f, .12f);
        var fan = MaterialFor("Fan", new Color(.14f, .14f, .145f), 0f, .29f);
        var brass = MaterialFor("Service", new Color(.47f, .36f, .23f), .65f, .29f);
        var guardCoat = MaterialFor("Guard_Coat", new Color(.095f, .096f, .097f), 0f, .36f);
        frame.mainTexture = FinishGrain("Powder_Coat", .045f, false);
        alloy.mainTexture = FinishGrain("Brushed_Alloy", .085f, true);
        var duct = Group(root, "Removable_Plenum_Panels");
        var radiator = Group(root, "Radiator_And_Headers");
        var intake = Group(root, "Left_Intake_Louvers");

        foreach (int sign in new[] { -1, 1 })
        {
            Box(duct, "Duct_Top_Bottom", new Vector3(-.04f, sign * .225f, 0), new Vector3(1.62f, .012f, .89f), frame);
            Box(duct, "Folded_Duct_End", new Vector3(-.04f, 0, sign * .444f), new Vector3(1.62f, .45f, .012f), frame);
            foreach (float y in new[] { -.190f, .190f })
            {
                Box(duct, "Folded_Panel_Return", new Vector3(-.05f, y, sign * .451f),
                    new Vector3(1.60f, .016f, .012f), frame, .003f);
                foreach (float x in new[] { -.74f, -.36f, .04f, .42f, .70f })
                    Pin(duct, "Panel_Rivet", new Vector3(x, y, sign * .458f), Vector3.forward, .010f, .003f, alloy);
            }
            Box(root, "Hanger_Rail", new Vector3(0, .239f, sign * .417f), new Vector3(1.64f, .024f, .054f), frame);
            foreach (float x in new[] { -.66f, .64f })
            {
                Box(root, "Isolation_Bush", new Vector3(x, .260f, sign * .417f), new Vector3(.077f, .018f, .054f), black);
                Box(root, "Mount_Foot", new Vector3(x, .275f, sign * .417f), new Vector3(.106f, .012f, .070f), alloy);
                Pin(root, "Mount_Bolt", new Vector3(x, .283f, sign * .417f), Vector3.up, .016f, .009f, alloy);
            }
            Box(radiator, "Folded_Core_Rail", new Vector3(-.56f, sign * .209f, 0), new Vector3(.117f, .024f, .77f), alloy);
            Box(radiator, "Coolant_Header", new Vector3(-.56f, 0, sign * .398f), new Vector3(.132f, .424f, .075f), alloy, .018f);
            Box(intake, "Louver_Edge_Channel", new Vector3(-.856f, 0, sign * .422f), new Vector3(.053f, .455f, .040f), frame);
        }

        for (int i = 0; i < 12; i++)
        {
            var louver = Box(intake, "Angled_Louver", new Vector3(-.856f, (i - 5.5f) * .035f, 0),
                new Vector3(.047f, .006f, .813f), alloy);
            louver.localRotation = Quaternion.Euler(0, 0, -28);
        }

        // Dense repeated fin geometry shares a single renderer and leaves open air passages.
        var finSize = new Vector3(.086f, .0018f, .754f);
        var finMesh = NF5KPassengerInterior.RoundedBox(finSize, .0005f);
        var fins = Enumerable.Range(0, 51).Select(i => new CombineInstance
        {
            mesh = finMesh,
            transform = Matrix4x4.Translate(new Vector3(-.56f, (i - 25) * .0078f, 0))
        }).ToArray();
        var combined = new Mesh();
        combined.CombineMeshes(fins);
        AddMesh(radiator, "Stacked_Heat_Exchange_Fins", Persist(combined, "Core_Fins"), core);
        for (int i = 0; i < 19; i++)
        {
            Box(radiator, "Coolant_Tube", new Vector3(-.603f, (i - 9) * .021f, 0), new Vector3(.018f, .0035f, .758f), alloy);
        }

        foreach (float y in new[] { -.126f, .136f })
        {
            var port = new Vector3(-.56f, y, -.451f);
            Pin(radiator, "Flanged_Header_Union", port, Vector3.forward, .084f, .023f, alloy);
            Pin(radiator, "Coolant_Coupler", port + Vector3.back * .021f, Vector3.forward, .054f, .021f, brass);
            Pin(radiator, "Hose_Interface_Seal", port + Vector3.back * .034f, Vector3.forward, .048f, .010f, black);
        }
        Pin(radiator, "Bleed_Screw", new Vector3(-.56f, .220f, .395f), Vector3.up, .022f, .017f, brass);
        Pin(radiator, "Drain_Plug", new Vector3(-.56f, -.228f, -.394f), Vector3.up, .025f, .018f, brass);

        var shroud = Persist(Ring(.194f, .220f, .029f, true), "Fan_Shroud");
        var guard = Persist(Ring(.181f, .186f, .004f, false), "Guard_Wire");
        var blade = Persist(Blade(), "Swept_Blade");
        foreach (float z in new[] { -.221f, .221f })
        {
            var pod = Group(root, z < 0 ? "Forward_Extractor" : "Aft_Extractor");
            pod.localPosition = new Vector3(.753f, 0, z);
            AddMesh(pod, "Flanged_Square_Shroud", shroud, frame);
            Pin(pod, "Electric_Motor", new Vector3(-.104f, 0, 0), Vector3.right, .095f, .10f, frame);
            foreach (float angle in new[] { 45f, 165f, 285f })
            {
                var arm = Box(pod, "Motor_Support_Arm", new Vector3(-.114f, 0, 0),
                    new Vector3(.028f, .175f, .020f), alloy);
                arm.localRotation = Quaternion.Euler(angle, 0, 0);
                var radial = arm.localRotation * Vector3.up;
                arm.localPosition += radial * .129f;
                var foot = Box(pod, "Motor_Bracket_Return", new Vector3(-.061f, 0, 0) + radial * .207f,
                    new Vector3(.106f, .018f, .024f), alloy);
                foot.localRotation = arm.localRotation;
            }
            Box(pod, "Motor_Terminal_Box", new Vector3(-.118f, .052f, 0), new Vector3(.050f, .029f, .043f), frame);
            var rotor = Group(pod, "Rotor");
            rotor.localRotation = Quaternion.Euler(z < 0 ? 0f : 21f, 0f, 0f);
            rotor.gameObject.AddComponent<CabCoolingFanRotor>().Bind(driver);
            Pin(rotor, "Rotor_Hub", new Vector3(-.015f, 0, 0), Vector3.right, .119f, .044f, alloy);
            Pin(rotor, "Hub_Cap", new Vector3(.012f, 0, 0), Vector3.right, .071f, .014f, black);
            for (int i = 0; i < 7; i++)
                AddMesh(rotor, "Swept_Axial_Blade", blade, fan).localRotation = Quaternion.Euler(i * 360f / 7f, 0, 0);
            for (int i = 1; i <= 4; i++)
            {
                var wire = AddMesh(pod, "Concentric_Guard", guard, guardCoat);
                wire.localPosition = Vector3.right * .045f;
                wire.localScale = new Vector3(1, i * .25f, i * .25f);
            }
            for (int i = 0; i < 4; i++)
            {
                var wire = Box(pod, "Guard_Spoke", new Vector3(.045f, 0, 0), new Vector3(.004f, .005f, .373f), guardCoat);
                wire.localRotation = Quaternion.Euler(i * 45, 0, 0);
            }
            foreach (int y in new[] { -1, 1 })
            foreach (int s in new[] { -1, 1 })
                Pin(pod, "Guard_Fastener", new Vector3(.023f, y * .198f, s * .198f), Vector3.right, .014f, .010f, alloy);
        }
    }

    static Mesh Blade()
    {
        // Three blade stations describe sweep and progressively reduced pitch toward the tip.
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var stations = new[] { new Vector4(.047f, .001f, .035f, .020f),
            new Vector4(.106f, .018f, .061f, .018f), new Vector4(.180f, .048f, .037f, .009f) };
        foreach (var s in stations)
        {
            vertices.Add(new Vector3(-s.w, s.x, s.y - s.z * .5f));
            vertices.Add(new Vector3(s.w, s.x, s.y + s.z * .5f));
            vertices.Add(new Vector3(s.w + .004f, s.x, s.y + s.z * .5f));
            vertices.Add(new Vector3(-s.w + .004f, s.x, s.y - s.z * .5f));
        }
        for (int row = 0; row < 2; row++)
        for (int edge = 0; edge < 4; edge++)
        {
            int a = row * 4 + edge, b = row * 4 + (edge + 1) % 4;
            Quad(triangles, a, b, b + 4, a + 4);
        }
        Quad(triangles, 3, 2, 1, 0);
        Quad(triangles, 8, 9, 10, 11);
        return MakeMesh(vertices, triangles);
    }

    static Mesh Ring(float inner, float outer, float depth, bool square)
    {
        const int segments = 48;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            float extent = square ? outer / Mathf.Max(Mathf.Abs(c), Mathf.Abs(s)) : outer;
            var profile = new[]
            {
                new Vector3(depth * .5f, c * inner, s * inner),
                new Vector3(depth * .5f, c * extent, s * extent),
                new Vector3(-depth * .5f, c * extent, s * extent),
                new Vector3(-depth * .5f, c * inner, s * inner)
            };
            int next = (i + 1) % segments * 8;
            for (int j = 0; j < 4; j++)
            {
                int edge = i * 8 + j * 2;
                vertices.Add(profile[j]);
                vertices.Add(profile[(j + 1) % 4]);
                float midAngle = angle + Mathf.PI / segments;
                Vector3 radial = new Vector3(0, c, s);
                Vector3 outerNormal = Mathf.Abs(Mathf.Cos(midAngle)) > Mathf.Abs(Mathf.Sin(midAngle))
                    ? Vector3.up * Mathf.Sign(c) : Vector3.forward * Mathf.Sign(s);
                Vector3 normal = j == 0 ? Vector3.right : j == 2 ? Vector3.left : j == 3 ? -radial : square ? outerNormal : radial;
                normals.Add(normal);
                normals.Add(normal);
                Quad(triangles, edge, edge + 1, next + j * 2 + 1, next + j * 2);
            }
        }
        var mesh = MakeMesh(vertices, triangles);

        mesh.SetNormals(normals);
        mesh.SetUVs(0, vertices.Select(v => new Vector2(v.y * 4f, v.z * 4f)).ToList());
        return mesh;
    }

    static void Quad(List<int> indices, int a, int b, int c, int d) =>
        indices.AddRange(new[] { a, b, c, a, c, d });

    static Mesh MakeMesh(List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh Persist(Mesh mesh, string name)
    {
        string path = $"Assets/Models/Cab/NF5KBodyMeshes/Cooling_{name}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (asset == null)
        {
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        mesh.name = name;
        EditorUtility.CopySerialized(mesh, asset);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static Material MaterialFor(string name, Color color, float metal, float smooth)
    {
        string path = $"Assets/Models/Cab/NF5KBodyMeshes/Cooling_{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetFloat("_Metallic", metal);
        mat.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D FinishGrain(string name, float contrast, bool brushed)
    {
        const int size = 128;
        string path = $"Assets/Models/Cab/NF5KBodyMeshes/Cooling_{name}.asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            texture.name = name;
            AssetDatabase.CreateAsset(texture, path);
        }
        var random = new System.Random(1747);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float row = (float)random.NextDouble();
            for (int x = 0; x < size; x++)
            {
                float noise = (float)random.NextDouble();
                float grain = brushed ? Mathf.Lerp(noise, row, .82f) : noise;
                byte value = (byte)Mathf.RoundToInt(255f * (1f - contrast * grain));
                pixels[y * size + x] = new Color32(value, value, value, 255);
            }
        }
        texture.SetPixels32(pixels);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 4;
        texture.Apply();
        EditorUtility.SetDirty(texture);
        return texture;
    }

    static Transform Group(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.gameObject.layer = parent.gameObject.layer;
        return child;
    }

    static Transform AddMesh(Transform parent, string name, Mesh mesh, Material mat)
    {
        var part = Group(parent, name);
        part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return part;
    }

    static Transform Box(Transform parent, string name, Vector3 point, Vector3 size, Material mat, float radius = .002f)
    {
        var part = AddMesh(parent, name, NF5KPassengerInterior.RoundedBox(size, radius), mat);
        part.localPosition = point;
        return part;
    }

    static void Pin(Transform parent, string name, Vector3 point, Vector3 axis, float diameter, float length, Material mat)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name;
        part.layer = parent.gameObject.layer;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = point;
        part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
        part.transform.localScale = new Vector3(diameter, length * .5f, diameter);
        part.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    static void CaptureReview(Transform root, Transform duct)
    {
        var camera = Camera.main;
        camera.orthographic = true;
        camera.farClipPlane = 100;
        var key = new GameObject("Cooling_Review_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.45f;
        var fill = new GameObject("Cooling_Review_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = .65f;
        fill.transform.rotation = Quaternion.Euler(30, 145, 0);
        foreach (var r in Object.FindObjectsByType<Renderer>())
            if (r is LineRenderer)
                r.enabled = false;
        Capture(camera, key, root, new Vector3(4, -.65f, -.75f), .93f, "on_train.png");
        foreach (var r in Object.FindObjectsByType<Renderer>())
            if (!r.transform.IsChildOf(root))
                r.enabled = false;
        Capture(camera, key, root, new Vector3(2.2f, .54f, -.91f), .62f, "fan_side.png");
        Capture(camera, key, root, new Vector3(-2.2f, .46f, -.94f), .62f, "intake.png");
        duct.gameObject.SetActive(false);
        Capture(camera, key, root, new Vector3(-1.18f, 1.45f, -1.68f), .77f, "cutaway.png");
    }

    static void Capture(Camera camera, Light key, Transform root, Vector3 view, float size, string filename)
    {
        camera.transform.position = root.TransformPoint(view);
        camera.transform.LookAt(root.position);
        camera.orthographicSize = size;
        key.transform.rotation = Quaternion.LookRotation(root.position - camera.transform.position + Vector3.down);
        bool async = ShaderUtil.allowAsyncCompilation;
        var rt = RenderTexture.GetTemporary(1600, 1050, 24);
        var previous = RenderTexture.active;
        var oldTarget = camera.targetTexture;
        var image = new Texture2D(1600, 1050, TextureFormat.RGB24, false);
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            camera.targetTexture = rt;
            camera.Render();
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 1050), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + filename, image.EncodeToPNG());
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation = async;
            camera.targetTexture = oldTarget;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(image);
        }
    }
}
