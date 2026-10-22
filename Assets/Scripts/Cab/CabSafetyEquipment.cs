using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabSafetyEquipment : MonoBehaviour
    {
        [SerializeField] Transform item;
        [SerializeField] bool extinguisher;
        Transform dockParent;
        Vector3 dockPosition, dockScale;
        Quaternion dockRotation;
        bool held;
        public bool Held => held;
        public string Hint => extinguisher ? "灭火器 · " + (held ? "已取下" : "在架") : "安全锤 · " + (held ? "已取下" : "在架");
        public string Description => extinguisher
            ? "车载灭火器：查看瓶身说明与固定状态。当前支持取放检查，尚未实现喷射与火灾模拟。"
            : "安全锤：用于指定应急窗。当前支持取放检查，尚未实现玻璃破碎。";
        public void Configure(Transform model,bool isExtinguisher) { item=model;extinguisher=isExtinguisher; }
        public bool Toggle()
        {
            var camera=Camera.main;
            if(item==null||camera==null||Vector3.Distance(camera.transform.position,transform.position)>2.4f)return false;
            if(held){Restore();return true;}
            foreach(var equipment in FindObjectsByType<CabSafetyEquipment>(FindObjectsSortMode.None))
                if(equipment!=this&&equipment.Held)return false;
            dockParent=item.parent;dockPosition=item.localPosition;dockRotation=item.localRotation;dockScale=item.localScale;
            item.SetParent(camera.transform,true);item.localPosition=new Vector3(.28f,-.32f,.60f);item.localRotation=Quaternion.identity;
            held=true;return true;
        }
        void Restore()
        {
            if(item!=null&&dockParent!=null){item.SetParent(dockParent,false);item.localPosition=dockPosition;item.localRotation=dockRotation;item.localScale=dockScale;}
            held=false;
        }
        void OnDisable(){if(held)Restore();}
    }
}
