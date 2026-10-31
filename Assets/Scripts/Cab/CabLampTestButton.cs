using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabLampTestButton : MonoBehaviour
    {
        [SerializeField] CabAnnunciatorPanel panel;
        [SerializeField] Transform cap;
        [SerializeField] Vector3 rest;
        float pressedUntil;
        public Transform Cap=>cap;
        public string Hint=>CabLightingKey.English?"Annunciator lamp test":"报警灯窗试灯";
        public string ActionLabel=>CabLightingKey.English?"Test all lamps · 2 s":"检查全部灯窗 · 2 秒";
        public void Configure(CabAnnunciatorPanel owner,Transform movingCap){panel=owner;cap=movingCap;rest=cap.localPosition;}
        public bool CanReach()=>isActiveAndEnabled&&panel!=null&&panel.isActiveAndEnabled&&Camera.main!=null&&Vector3.Distance(Camera.main.transform.position,transform.position)<=2.4f;
        public string UnavailableReason=>!CanReach()?(CabLightingKey.English?"Move closer to the test button":"请靠近试灯按钮"):!panel.SupplyAvailable?(CabLightingKey.English?"Control supply unavailable":"主控未供电"):panel.IsTesting?(CabLightingKey.English?"Lamp test in progress":"正在试灯"):null;
        public bool Press()
        {
            if(UnavailableReason!=null||!panel.RequestLampTest())return false;
            pressedUntil=Time.unscaledTime+.16f;ApplyVisual();return true;
        }
        public void ApplyVisual(){if(cap!=null)cap.localPosition=rest-(Time.unscaledTime<pressedUntil&&panel!=null&&panel.IsTesting?Vector3.forward*.0015f:Vector3.zero);}
        void LateUpdate(){ApplyVisual();}
        void OnDisable(){pressedUntil=0;if(panel!=null){panel.CancelLampTest();panel.Refresh();}ApplyVisual();}
    }
}
