using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabPassageDoor : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Quaternion closed;
        float angle,target;
        bool wantsOpen;
        BoxCollider[] moving,obstacles;
        public bool IsOpen => angle>=84.9f;
        public bool Moving => Mathf.Abs(angle-target)>.01f;
        public float Angle => angle;
        public bool NextActionCloses => wantsOpen;
        public string Hint => "驾驶室通道门 · "+(LastWarning!=null?"遇阻停止，请让出空间":Moving?"转动中":angle<.1f?"关闭":IsOpen?"打开":"停在中途");
        public string LastWarning {get;private set;}
        public void Configure(Transform vehicle)
        {
            body=vehicle;closed=transform.localRotation;angle=target=0;wantsOpen=false;Cache();
        }
        void Cache(){moving=GetComponentsInChildren<BoxCollider>();obstacles=body!=null?body.GetComponentsInChildren<BoxCollider>():new BoxCollider[0];}
        void Awake(){Cache();}
        public bool Toggle()
        {
            var camera=Camera.main;
            if(!isActiveAndEnabled||Moving||camera==null||Vector3.Distance(camera.transform.position,transform.position)>2.4f)return false;
            Cache();wantsOpen=!wantsOpen;target=wantsOpen?85:0;LastWarning=null;return true;
        }
        void Update(){Advance(Time.deltaTime);}
        public void Advance(float seconds)
        {
            if(!Moving||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            // Limit each collision step even if a frame stalls.
            int count=Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(seconds,.5f)*340/2),1,85);
            for(int step=0;step<count&&Moving;step++)
            {
                float next=Mathf.MoveTowards(angle,target,Mathf.Min(seconds,.5f)*340/count);
                transform.localRotation=closed*Quaternion.Euler(0,next,0);
                bool blocked=false;
                foreach(var leaf in moving)
                {
                    if(!leaf.enabled)continue;
                    foreach(var obstacle in obstacles)
                    {
                        if(obstacle==null||!obstacle.enabled||obstacle.isTrigger||!obstacle.gameObject.activeInHierarchy||obstacle.transform.IsChildOf(transform))continue;
                        if(Physics.ComputePenetration(leaf,leaf.transform.position,leaf.transform.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out float depth)&&depth>.002f){blocked=true;break;}
                    }
                    var camera=Camera.main;
                    if(camera!=null)
                    {
                        var local=leaf.transform.InverseTransformPoint(camera.transform.position)-leaf.center;
                        var half=leaf.size*.5f;
                        var closest=new Vector3(Mathf.Clamp(local.x,-half.x,half.x),Mathf.Clamp(local.y,-half.y,half.y),Mathf.Clamp(local.z,-half.z,half.z))+leaf.center;
                        if(Vector3.Distance(leaf.transform.TransformPoint(closest),camera.transform.position)<.18f)blocked=true;
                    }
                    if(blocked)break;
                }
                if(blocked)
                {
                    transform.localRotation=closed*Quaternion.Euler(0,angle,0);target=angle;LastWarning="门扇前有障碍，请让出空间后再操作";break;
                }
                angle=next;
            }
            if(Mathf.Abs(angle-target)<=.01f){angle=target;transform.localRotation=closed*Quaternion.Euler(0,angle,0);}
            Physics.SyncTransforms();
        }
        void OnDisable(){angle=target=0;wantsOpen=false;transform.localRotation=closed;LastWarning=null;}
    }
}
