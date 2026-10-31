using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabStorageCabinet : MonoBehaviour
    {
        [SerializeField] Transform door;
        [SerializeField] Quaternion closedRotation;
        [SerializeField] Transform latch;
        [SerializeField] Quaternion lockedRotation;
        bool open;
        public bool IsOpen => open;
        public string Hint => "急救用品柜 / FIRST AID CABINET";

        public void Configure(Transform leaf,Transform catchHandle)
        {
            door=leaf;
            closedRotation=leaf.localRotation;
            latch=catchHandle;
            lockedRotation=latch.localRotation;
        }

        public bool Toggle()
        {
            var camera=Camera.main;
            if(door==null||latch==null||camera==null||Vector3.Distance(camera.transform.position,transform.position)>2.4f)return false;
            open=!open;
            latch.localRotation=lockedRotation*Quaternion.Euler(0,0,open?90f:0);
            door.localRotation=closedRotation*Quaternion.Euler(0,open?-100f:0,0);
            return true;
        }

        void OnDisable()
        {
            if(door!=null)door.localRotation=closedRotation;
            if(latch!=null)latch.localRotation=lockedRotation;
            open=false;
        }
    }
}
