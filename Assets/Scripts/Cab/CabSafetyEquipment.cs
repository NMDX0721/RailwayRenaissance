using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabSafetyEquipment : MonoBehaviour
    {
        [SerializeField] Transform item;
        [SerializeField] bool extinguisher;
        [SerializeField] bool manual;
        public bool IsManual => manual;
        public void ConfigureManual(){manual=true;}
        [SerializeField] Transform[] retainingBands;
        [SerializeField] Quaternion[] closedBandRotations;
        Transform dockParent;
        Vector3 dockPosition, dockScale;
        Quaternion dockRotation;
        bool held;
        public bool Held => held;
        public string Hint => manual?"消防器材手册 / FIRE EQUIPMENT MANUAL":extinguisher ? "灭火器 / EXTINGUISHER · " + (held ? "已取下 / REMOVED" : "在架 / STOWED") : "安全锤 / SAFETY HAMMER · " + (held ? "已取下 / REMOVED" : "在架 / STOWED");
        public string Description => manual
            ? "随车消防器材使用与维护手册\nONBOARD FIRE EQUIPMENT — OPERATION & MAINTENANCE\n\n"+CabSafetyCatalog.Extinguisher.Information
            : extinguisher
            ? CabSafetyCatalog.Extinguisher.Information
            : "应急破窗锤 / EMERGENCY SAFETY HAMMER\n\n安装位置 / LOCATION\n客舱后端门区 / Rear passenger vestibule\n\n用途 / APPLICATION\n指定应急窗的紧急破窗器具。\nEmergency escape tool for designated breakable escape windows.\n\n日常检查 / ROUTINE INSPECTION\n确认锤头、握柄与固定座完好；保持标识可见、取用通道畅通。\nCheck the head, grip and holder. Keep the sign visible and access clear.\n\n设备履历 / SERVICE RECORDS\n检查及更换记录见随车检修册。\nSee the onboard service log for inspection and replacement records.";
        public void Configure(Transform model,bool isExtinguisher,Transform[] bands=null)
        {
            item=model;extinguisher=isExtinguisher;retainingBands=bands;
            closedBandRotations=new Quaternion[bands?.Length??0];
            for(int i=0;i<closedBandRotations.Length;i++)closedBandRotations[i]=bands[i].localRotation;
        }
        public bool Toggle()
        {
            if(manual)return false;
            var camera=Camera.main;
            if(item==null||camera==null||Vector3.Distance(camera.transform.position,transform.position)>2.4f)return false;
            if(held){Restore();return true;}
            foreach(var equipment in FindObjectsByType<CabSafetyEquipment>(FindObjectsSortMode.None))
                if(equipment!=this&&equipment.Held)return false;
            dockParent=item.parent;dockPosition=item.localPosition;dockRotation=item.localRotation;dockScale=item.localScale;
            SetBands(true);
            item.SetParent(camera.transform,true);item.localPosition=new Vector3(.28f,-.32f,.60f);item.localRotation=extinguisher?Quaternion.Euler(0,180,0):Quaternion.identity;
            held=true;return true;
        }
        void Restore()
        {
            if(item!=null&&dockParent!=null){item.SetParent(dockParent,false);item.localPosition=dockPosition;item.localRotation=dockRotation;item.localScale=dockScale;}
            SetBands(false);
            held=false;
        }
        void SetBands(bool open)
        {
            for(int i=0;i<(retainingBands?.Length??0);i++)if(retainingBands[i]!=null)
                retainingBands[i].localRotation=closedBandRotations[i]*Quaternion.Euler(0,open?-105:0,0);
        }
        void OnDisable(){if(held)Restore();}
    }
}
