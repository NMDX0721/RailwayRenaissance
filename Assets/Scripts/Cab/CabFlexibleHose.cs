using UnityEngine;

namespace RailwayRenaissance.Cab
{
    [DefaultExecutionOrder(150)]
    public sealed class CabFlexibleHose : MonoBehaviour
    {
        [SerializeField] Transform from,to;
        [SerializeField] float radius=.009f;
        Mesh runtimeMesh;
        Mesh updatedMesh;
        Vector3 lastA,lastB,lastUp,lastRight,lastForward;
        float lastRadius;
        public int GeometryUpdateCount { get; private set; }
        readonly Vector3[] vertices=new Vector3[17*8];
        readonly int[] triangles=new int[16*8*6];
        public void Configure(Transform a,Transform b){from=a;to=b;updatedMesh=null;}
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            runtimeMesh=new Mesh{name="Flexible_Service_Hose_Runtime"};runtimeMesh.MarkDynamic();GetComponent<MeshFilter>().sharedMesh=runtimeMesh;
        }
        void LateUpdate(){if(runtimeMesh!=null)UpdateGeometry(runtimeMesh);}
        void OnDisable(){if(runtimeMesh!=null){Destroy(runtimeMesh);runtimeMesh=null;}updatedMesh=null;}
        public void UpdateGeometry(Mesh mesh)
        {
            if(from==null||to==null||mesh==null)return;
            var localA=transform.InverseTransformPoint(from.position);
            var localB=transform.InverseTransformPoint(to.position);
            var localUp=transform.InverseTransformVector(Vector3.up);
            var localRight=transform.InverseTransformVector(Vector3.right);
            var localForward=transform.InverseTransformVector(Vector3.forward);
            bool sameMesh=updatedMesh==mesh && mesh.vertexCount==vertices.Length;
            // Compare in mesh space: a shared rigid translation does not deform the tube.
            const float toleranceSquared=1e-10f;
            if(sameMesh && lastRadius==radius && (localA-lastA).sqrMagnitude<toleranceSquared
                && (localB-lastB).sqrMagnitude<toleranceSquared && (localUp-lastUp).sqrMagnitude<toleranceSquared
                && (localRight-lastRight).sqrMagnitude<toleranceSquared && (localForward-lastForward).sqrMagnitude<toleranceSquared)return;
            const int steps=16,sides=8;
            var a=from.position;var b=to.position;var c=a-Vector3.up*.18f;var d=b-Vector3.up*.18f;
            Vector3 Point(float t){float s=1-t;return s*s*s*a+3*s*s*t*c+3*s*t*t*d+t*t*t*b;}
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps;var point=Point(t);var tangent=(Point(Mathf.Min(1,t+.002f))-Point(Mathf.Max(0,t-.002f))).normalized;
                var right=Vector3.Cross(tangent,Mathf.Abs(Vector3.Dot(tangent,Vector3.right))<.9f?Vector3.right:Vector3.forward).normalized;
                var up=Vector3.Cross(tangent,right).normalized;
                for(int j=0;j<sides;j++)
                {
                    float angle=j*Mathf.PI*2/sides;vertices[i*sides+j]=transform.InverseTransformPoint(point+radius*(right*Mathf.Cos(angle)+up*Mathf.Sin(angle)));
                    if(i==steps)continue;int k=(i*sides+j)*6,p=i*sides+j,q=i*sides+(j+1)%sides;
                    triangles[k]=p;triangles[k+1]=q;triangles[k+2]=p+sides;triangles[k+3]=q;triangles[k+4]=q+sides;triangles[k+5]=p+sides;
                }
            }
            mesh.vertices=vertices;if(!sameMesh)mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            updatedMesh=mesh;lastA=localA;lastB=localB;lastUp=localUp;lastRight=localRight;lastForward=localForward;lastRadius=radius;
            GeometryUpdateCount++;
        }
        public float EndpointError()
        {
            Vector3 first=Vector3.zero,last=Vector3.zero;
            for(int i=0;i<8;i++){first+=transform.TransformPoint(vertices[i]);last+=transform.TransformPoint(vertices[128+i]);}
            return Mathf.Max(Vector3.Distance(first/8,from.position),Vector3.Distance(last/8,to.position));
        }
    }
}
