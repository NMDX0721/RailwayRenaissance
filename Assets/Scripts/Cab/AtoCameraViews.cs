using UnityEngine;
using UnityEngine.InputSystem;

namespace RailwayRenaissance.Cab
{
    // Review/demo camera modes for the ATO line scenes. The scene's driver camera sits at the world origin, so every mode carries
    // the camera with the vehicle body (same frame as CabRailMotion.Vehicle). Keys 1-5 select, Tab cycles.
    public sealed class AtoCameraViews : MonoBehaviour
    {
        struct View { public string name; public Vector3 position, look; }
        View[] views;
        int current;
        Camera cam;
        CabRailMotion motion;
        Font font;
        float savedFar;

        void Start()
        {
            motion = FindAnyObjectByType<CabRailMotion>();
            cam = Camera.main;
            if (cam == null || motion == null || motion.Vehicle == null) { enabled = false; return; }
            float rail = -(float)CabReviewTrack.RailDatum;                 // rail top in the vehicle frame
            // Vehicle frame: nose toward -Z, origin at the nose end, body centre at z = 8.6, driver eye at (.65, 1.38, .78).
            views = new[]
            {
                new View{name="驾驶室",     position=new Vector3(.65f,1.38f,.78f),        look=new Vector3(.60f,1.22f,-.8f)},
                new View{name="车外追尾",   position=new Vector3(4.6f,2.0f,8.6f+19f),     look=new Vector3(-.4f,1.5f,8.6f-28f)},
                new View{name="车头迎面",   position=new Vector3(2.6f,rail+1.6f,-18f),    look=new Vector3(0f,rail+1.5f,-1f)},
                new View{name="轨道特写",   position=new Vector3(1.95f,rail+.45f,2.5f),   look=new Vector3(.75f,rail,-2.5f)},
                new View{name="高空俯瞰",   position=new Vector3(8f,22f,8.6f+26f),        look=new Vector3(0f,0f,8.6f-8f)},
            };
            savedFar = cam.farClipPlane;
            if (cam.farClipPlane < 3000f) cam.farClipPlane = 3000f;
            Apply(0);
        }

        void OnDisable() { if (cam != null && savedFar > 0f) cam.farClipPlane = savedFar; }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null || views == null) return;
            if (k.digit1Key.wasPressedThisFrame) Apply(0);
            else if (k.digit2Key.wasPressedThisFrame) Apply(1);
            else if (k.digit3Key.wasPressedThisFrame) Apply(2);
            else if (k.digit4Key.wasPressedThisFrame) Apply(3);
            else if (k.digit5Key.wasPressedThisFrame) Apply(4);
            else if (k.tabKey.wasPressedThisFrame) Apply((current + 1) % views.Length);
        }

        void Apply(int index)
        {
            current = index;
            var v = views[index];
            var t = cam.transform;
            t.SetParent(motion.Vehicle, false);
            t.localPosition = v.position;
            t.localRotation = Quaternion.LookRotation(v.look - v.position, Vector3.up);
        }

        void OnGUI()
        {
            if (views == null) return;
            if (font == null) font = Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var previous = GUI.skin.font;
            if (font != null) GUI.skin.font = font;
            string line = "视角：";
            for (int i = 0; i < views.Length; i++) line += (i == current ? "【" : "[") + (i + 1) + " " + views[i].name + (i == current ? "】" : "]") + " ";
            GUI.Box(new Rect(16, Screen.height - 44, 560, 28), line + "· Tab 切换");
            GUI.skin.font = previous;
        }
    }
}
