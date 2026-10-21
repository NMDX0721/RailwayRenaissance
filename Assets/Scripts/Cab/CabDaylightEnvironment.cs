using UnityEngine;
using UnityEngine.Rendering;

namespace RailwayRenaissance.Cab
{
    [ExecuteAlways]
    public sealed class CabDaylightEnvironment : MonoBehaviour
    {
        public enum DayPreset { Clear, Overcast, LowSun }
        [SerializeField] Light sun;
        [SerializeField] Camera reviewCamera;
        [SerializeField] Material sky;
        [SerializeField] DayPreset preset;
        Font font;
        public void Configure(Light source, Camera camera, Material skyMaterial)
        { sun=source; reviewCamera=camera; sky=skyMaterial; Apply(DayPreset.Clear); }
        void OnEnable() { if(sun!=null)Apply(preset); }
        public void Apply(DayPreset value)
        {
            preset=value;
            bool cloudy=value==DayPreset.Overcast, low=value==DayPreset.LowSun;
            sun.enabled=true; sun.type=LightType.Directional;
            sun.intensity=cloudy?.28f:low?1.15f:1.4f;
            sun.color=low?new Color(1,.82f,.63f):new Color(1,.97f,.91f);
            sun.transform.rotation=Quaternion.Euler(low?16:cloudy?55:48,-38,0);
            sun.shadows=LightShadows.Soft; sun.shadowStrength=cloudy?.45f:.85f;
            RenderSettings.sun=sun; RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=cloudy?new Color(.42f,.46f,.50f):new Color(.36f,.44f,.54f);
            RenderSettings.ambientEquatorColor=cloudy?new Color(.25f,.27f,.28f):new Color(.20f,.24f,.27f);
            RenderSettings.ambientGroundColor=new Color(.075f,.085f,.065f);
            reviewCamera.clearFlags=CameraClearFlags.Skybox; reviewCamera.farClipPlane=350;
            Object.FindFirstObjectByType<CabDiffuseFill>()?.Refresh();
            Object.FindFirstObjectByType<CabInteriorReflections>()?.RefreshIfChanged();
        }
        [ContextMenu("Daylight/Clear")] void Clear() { Apply(DayPreset.Clear); }
        [ContextMenu("Daylight/Overcast")] void Overcast() { Apply(DayPreset.Overcast); }
        [ContextMenu("Daylight/Low sun")] void LowSun() { Apply(DayPreset.LowSun); }
        public void SetView(int view)
        {
            reviewCamera.orthographic=false;
            if(view==0){reviewCamera.transform.position=new Vector3(.65f,1.38f,.78f);reviewCamera.transform.LookAt(new Vector3(.60f,1.22f,-.8f));reviewCamera.fieldOfView=65;}
            else if(view==1){reviewCamera.transform.position=new Vector3(0,1.58f,2.18f);reviewCamera.transform.LookAt(new Vector3(0,1.28f,11));reviewCamera.fieldOfView=60;}
            else {reviewCamera.transform.position=new Vector3(11,6,-9);reviewCamera.transform.LookAt(new Vector3(0,.65f,7));reviewCamera.fieldOfView=55;}
        }
        [ContextMenu("View/Passenger")] void PassengerView() { SetView(1); }
        [ContextMenu("View/Driver")] void DriverView() { SetView(0); }
        [ContextMenu("View/Exterior")] void ExteriorView() { SetView(2); }
        void OnGUI()
        {
            if(!Application.isPlaying)return;
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var old=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            GUILayout.BeginArea(new Rect(Mathf.Max(0,(Screen.width-400)/2),Screen.height-108,400,98),GUI.skin.box);
            GUILayout.Label("白天光照检查 · 车内灯可独立开关");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("晴天"))Apply(DayPreset.Clear);
            if(GUILayout.Button("阴天"))Apply(DayPreset.Overcast);
            if(GUILayout.Button("低角度日光"))Apply(DayPreset.LowSun);
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            if(GUILayout.Button("驾驶位"))SetView(0);
            if(GUILayout.Button("客舱"))SetView(1);
            if(GUILayout.Button("车外"))SetView(2);
            GUILayout.EndHorizontal();GUILayout.EndArea();GUI.skin.font=old;
        }
    }
}
