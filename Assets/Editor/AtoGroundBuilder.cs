using System.IO;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

// Ground for the ATO line: a wide grass plane below the lowest ballast plus an embankment strip that follows the route height,
// so graded lines read as raised formation instead of floating hills. Mesh only, no colliders.
public static class AtoGroundBuilder
{
    const string Root="ATO_Ground";
    const float BallastBottom=.6f,TopHalf=2.5f,Slope=1.5f,PlaneDrop=1.6f;
    public static GameObject Build(CabReviewTrack track,float step=5f)
    {
        var old=GameObject.Find(Root);if(old!=null)Object.DestroyImmediate(old);
        var old2=GameObject.Find("Review_Ground_Not_Vehicle");if(old2!=null)Object.DestroyImmediate(old2);
        var root=new GameObject(Root);
        double lo=track.RangeStart-20,hi=track.RangeEnd+20,minY=double.MaxValue;
        for(double s=track.RangeStart;s<=track.RangeEnd;s+=step)minY=System.Math.Min(minY,track.At(s).Position.y-BallastBottom);
        float planeY=(float)minY-PlaneDrop;
        basePlaneY=planeY;trackZMin=8.6f-(float)track.RangeEnd;trackZMax=8.6f-(float)track.RangeStart;
        BuildTerrain(root.transform,(float)lo,(float)hi);
        // Embankment: flat top (half width TopHalf) sloping down to the plane.
        int n=(int)System.Math.Ceiling((hi-lo)/step)+1;var v=new Vector3[n*4];var uv=new Vector2[n*4];var tri=new System.Collections.Generic.List<int>();
        for(int i=0;i<n;i++)
        {
            double s=System.Math.Min(lo+i*step,hi);
            float top=track.At(System.Math.Max(track.RangeStart,System.Math.Min(track.RangeEnd,s))).Position.y-BallastBottom-.03f;
            float z=8.6f-(float)s,w=TopHalf+Slope*(top-planeY);
            v[i*4]=new Vector3(-w,planeY,z);v[i*4+1]=new Vector3(-TopHalf,top,z);v[i*4+2]=new Vector3(TopHalf,top,z);v[i*4+3]=new Vector3(w,planeY,z);
            for(int k=0;k<4;k++)uv[i*4+k]=new Vector2(v[i*4+k].x/6f,(float)s/6f);
            if(i==0)continue;int a=(i-1)*4,b=i*4;
            for(int k=0;k<3;k++){tri.Add(a+k);tri.Add(b+k);tri.Add(a+k+1);tri.Add(a+k+1);tri.Add(b+k);tri.Add(b+k+1);}
        }
        var mesh=new Mesh{name="ATO_Embankment",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=v,uv=uv,triangles=tri.ToArray()};mesh.RecalculateNormals();
        if(mesh.normals[5].y<0){var t=tri.ToArray();for(int i=0;i<t.Length;i+=3){int x=t[i+1];t[i+1]=t[i+2];t[i+2]=x;}mesh.triangles=t;mesh.RecalculateNormals();}
        mesh.RecalculateBounds();
        var bank=new GameObject("Embankment");bank.transform.SetParent(root.transform);
        bank.AddComponent<MeshFilter>().sharedMesh=mesh;bank.AddComponent<MeshRenderer>().sharedMaterial=Mat("ATO_Ground_Formation",new Color(.30f,.38f,.20f),1.2f);
        return root;
    }
    static float basePlaneY,trackZMin,trackZMax;
    // Ground height at a world position: flat beside the formation, then gentle bumps and, further out, low hills so the horizon is not a ruler line.
    public static float GroundY(float x,float z)
    {
        float dz=Mathf.Max(0,Mathf.Max(trackZMin-z,z-trackZMax));float d=Mathf.Sqrt(x*x+dz*dz);
        float hill=Mathf.SmoothStep(0,1,(d-90)/480f)*(2f+40f*Mathf.Pow(Mathf.PerlinNoise(x/300f+17.3f,z/300f+5.1f),1.6f));
        float bump=Mathf.SmoothStep(0,1,(d-16)/70f)*.9f*(Mathf.PerlinNoise(x/11f,z/11f)-.5f+.5f*(Mathf.PerlinNoise(x/40f,z/40f)-.5f));
        return basePlaneY+hill+bump;
    }
    static void BuildTerrain(Transform parent,float lo,float hi)
    {
        const float Cell=10f;float half=900f;
        float z0=8.6f-hi-900f,z1=8.6f-lo+900f;int nx=(int)(2*half/Cell),nz=(int)((z1-z0)/Cell);
        var v=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[v.Length];var tri=new int[nx*nz*6];
        for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
        {
            float x=-half+i*Cell,z=z0+j*Cell;v[j*(nx+1)+i]=new Vector3(x,GroundY(x,z),z);uv[j*(nx+1)+i]=new Vector2(x/18f,z/18f);
        }
        int k=0;
        for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;tri[k++]=a;tri[k++]=c;tri[k++]=b;tri[k++]=b;tri[k++]=c;tri[k++]=d;}
        var mesh=new Mesh{name="ATO_Terrain",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=v,uv=uv,triangles=tri};mesh.RecalculateNormals();mesh.RecalculateBounds();
        if(mesh.normals[(nx+1)*(nz/2)+nx/2].y<0){for(int i=0;i<tri.Length;i+=3){int t=tri[i+1];tri[i+1]=tri[i+2];tri[i+2]=t;}mesh.triangles=tri;mesh.RecalculateNormals();}
        var go=new GameObject("Ground_Terrain");go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=Mat("ATO_Ground_Grass",new Color(.33f,.41f,.22f),1f);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    public static Material Mat(string name,Color c,float tiling)
    {
        Directory.CreateDirectory("Assets/Materials/ATO");string path="Assets/Materials/ATO/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_BaseMap",Noise(name));m.SetTextureScale("_BaseMap",new Vector2(tiling,tiling));m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",.05f);m.SetFloat("_Metallic",0);
        EditorUtility.SetDirty(m);return m;
    }
    // Tileable value noise, greyscale around 0.9, so the material colour keeps its tint.
    static Texture2D Noise(string name)
    {
        string path="Assets/Models/ATO/Textures/"+name+"_Noise.png";
        if(!File.Exists(path))
        {
            Directory.CreateDirectory("Assets/Models/ATO/Textures");const int N=256;var t=new Texture2D(N,N,TextureFormat.RGB24,false);var r=new System.Random(name.Length*7919);
            var g=new float[16,16];for(int y=0;y<16;y++)for(int x=0;x<16;x++)g[x,y]=(float)r.NextDouble();
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                float fx=x/16f,fy=y/16f;int x0=(int)fx,y0=(int)fy;float tx=fx-x0,ty=fy-y0;tx=tx*tx*(3-2*tx);ty=ty*ty*(3-2*ty);
                float a=Mathf.Lerp(g[x0%16,y0%16],g[(x0+1)%16,y0%16],tx),b=Mathf.Lerp(g[x0%16,(y0+1)%16],g[(x0+1)%16,(y0+1)%16],tx),n=Mathf.Lerp(a,b,ty);
                float v=.78f+.28f*n+.10f*((float)r.NextDouble()-.5f);t.SetPixel(x,y,new Color(v,v,v));
            }
            File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
