using UnityEngine;

namespace RailwayRenaissance.Cab
{
    [DefaultExecutionOrder(200)]
    public sealed class CabRailReviewViews : MonoBehaviour
    {
        [SerializeField] CabRailMotion motion;
        [SerializeField] Camera reviewCamera;
        int view=2;
        Font font;
        public int View=>view;
        public void SetView(int value){view=Mathf.Clamp(value,0,2);LateUpdate();}
        public bool BlocksPointer(Vector2 screenPoint)=>isActiveAndEnabled&&new Rect(Screen.width-310,Screen.height-132,294,116).Contains(new Vector2(screenPoint.x,Screen.height-screenPoint.y));
        public void Configure(CabRailMotion source,Camera camera){motion=source;reviewCamera=camera;}
        void LateUpdate()
        {
            if(motion==null||reviewCamera==null)return;
            var body=motion.Vehicle;
            if(view==0)
            {
                reviewCamera.orthographic=false;reviewCamera.fieldOfView=65;
                reviewCamera.transform.SetPositionAndRotation(body.TransformPoint(new Vector3(.65f,1.38f,.78f)),body.rotation*Quaternion.LookRotation(new Vector3(-.05f,-.16f,-1.58f)));
            }
            else
            {
                var point=body.TransformPoint(view==1?new Vector3(0,-.65f,2.5f):new Vector3(0,.4f,8.6f));
                reviewCamera.orthographic=true;reviewCamera.orthographicSize=view==1?2.2f:11;
                reviewCamera.transform.position=point+body.rotation*(view==1?new Vector3(5,1.2f,-3):new Vector3(19,9,-17));reviewCamera.transform.LookAt(point);
            }
        }
        void OnGUI()
        {
            if(motion==null)return;
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");var previous=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            GUILayout.BeginArea(new Rect(Screen.width-310,Screen.height-132,294,116),GUI.skin.box);
            GUILayout.Label("走行部检查 · 轨道约束模型");
            GUILayout.Label($"位置 {motion.Station:F1} m · 车体侧倾 {motion.RollDegrees:F2}°");
            GUILayout.BeginHorizontal();if(GUILayout.Button("驾驶位"))view=0;if(GUILayout.Button("走行部"))view=1;if(GUILayout.Button("整车"))view=2;GUILayout.EndHorizontal();
            GUILayout.Label("F2 显示既有启动/制动检查面板");GUILayout.EndArea();GUI.skin.font=previous;
        }
    }
}
