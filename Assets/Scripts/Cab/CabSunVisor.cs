using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabSunVisor : MonoBehaviour
    {
        [SerializeField] Transform leaf;
        [SerializeField] bool driverSide;
        [SerializeField] int step;
        public int Step => step;
        public string Hint => (driverSide?"司机遮阳板":"副司机遮阳板")+" · "+(step==0?"收起":step==1?"半降":"降下");

        public void Configure(Transform panel,bool driver)
        {
            leaf=panel;driverSide=driver;step=0;Apply();
        }
        public bool Adjust(int direction)
        {
            var camera=Camera.main;
            if(leaf==null||camera==null||direction==0||Vector3.Distance(camera.transform.position,transform.position)>2.4f)return false;
            int next=Mathf.Clamp(step+(direction>0?1:-1),0,2);
            if(next==step)return false;
            step=next;Apply();return true;
        }
        void Apply(){if(leaf!=null)leaf.localRotation=Quaternion.Euler(-90+45*step,0,0);}
        void OnDisable(){step=0;Apply();}
    }
}
