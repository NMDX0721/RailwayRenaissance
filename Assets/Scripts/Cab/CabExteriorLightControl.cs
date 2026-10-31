using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabExteriorLightControl : MonoBehaviour
    {
        [System.Serializable] sealed class Profile{public string[] headZh,headEn,markerZh,markerEn,modeZh,modeEn;}
        static Profile profile;
        [SerializeField] bool markerControl;
        [SerializeField] Transform selector;
        CabExteriorLighting lighting;
        CabExteriorLighting Lighting=>lighting!=null?lighting:lighting=GetComponentInParent<CabExteriorLighting>();
        public bool IsMarker=>markerControl;
        public int DetentCount=>markerControl?6:3;
        public Transform Selector=>selector;
        public int Position=>Lighting==null?0:markerControl?(int)Lighting.OperatingMode:(int)Lighting.HeadlightLevel;
        public string Label(int position)
        {
            if(profile==null)profile=JsonUtility.FromJson<Profile>(Resources.Load<TextAsset>("Cab/exterior_light_control_profile").text);
            string[] zh=markerControl?profile.modeZh:profile.headZh;
            string[] en=markerControl?profile.modeEn:profile.headEn;
            return CabLightingKey.English?en[position]:zh[position];
        }
        public string Hint=>Label(Position)+(Lighting!=null&&!Lighting.PowerAvailable?(CabLightingKey.English?" · No supply":" · 无供电"):"");
        public void Configure(bool isMarker,Transform knob){markerControl=isMarker;selector=knob;lighting=null;ApplyVisual();}
        public bool CanOperate()=>isActiveAndEnabled&&gameObject.activeInHierarchy&&Lighting!=null&&Camera.main!=null&&Vector3.Distance(Camera.main.transform.position,transform.position)<=2.4f;
        public string UnavailableReason(int position)=>!CanOperate()?(CabLightingKey.English?"Move closer to the selector":"请靠近灯光旋钮操作"):Position==position?CabLightingKey.Labels.already.Get(CabLightingKey.English):null;
        public bool Select(int position)
        {
            if(position<0||position>=DetentCount||!CanOperate()||Position==position)return false;
            if(markerControl)Lighting.SetOperatingMode((ExteriorOperatingMode)position);else Lighting.SetHeadlightLevel((ExteriorHeadlightLevel)position);
            ApplyVisual();return true;
        }
        public bool SetManualMarker(int position)
        {
            if(!markerControl||!CanOperate()||Lighting.OperatingMode!=ExteriorOperatingMode.Manual||position<0||position>2)return false;
            Lighting.SetMarkerAspect((ExteriorMarkerAspect)position);ApplyVisual();return true;
        }
        public void ApplyVisual(){if(selector!=null)selector.localRotation=Quaternion.Euler(0,0,markerControl?-100+Position*40:-60+Position*60);}
        void LateUpdate(){ApplyVisual();}
    }
}
