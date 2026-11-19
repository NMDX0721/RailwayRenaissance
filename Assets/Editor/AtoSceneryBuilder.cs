using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

// Lineside scenery for the ATO test line: scattered trees, catenary-style masts with wires, speed boards and haze.
// Meshes only (no colliders), built from fixed random seeds so every build is identical. Visual only; it does not touch the route table or the vehicle.
public static class AtoSceneryBuilder
{
    const string Root="ATO_Scenery";
    const float MastX=-3.4f,MastSpacing=60f,WireHeight=5.3f,MastHeight=8.2f;

    public static void Build(CabReviewTrack track)
    {
        var old=GameObject.Find(Root);if(old!=null)Object.DestroyImmediate(old);
        var root=new GameObject(Root);
        Trees(root.transform,track);
        Masts(root.transform,track);
        Boards(root.transform,track);
        Platform(root.transform,track);
        Markers(root.transform,track);
        Bushes(root.transform,track);
        Haze();
        TuneTrackTextures();
    }

    // ------------------------------------------------------------------ trees
    static void Trees(Transform parent,CabReviewTrack track)
    {
        var rnd=new System.Random(20261107);
        var trunks=new MeshBuilder();
        var conifers=new MeshBuilder[3];for(int i=0;i<conifers.Length;i++)conifers[i]=new MeshBuilder();
        var leaves=new MeshBuilder[6];for(int i=0;i<leaves.Length;i++)leaves[i]=new MeshBuilder();
        double zLo=8.6-track.RangeEnd-120,zHi=8.6-track.RangeStart+120;
        int clusters=70,placed=0;
        for(int c=0;c<clusters;c++)
        {
            float side=rnd.NextDouble()<.5?-1:1;float cx=side*(24+(float)rnd.NextDouble()*230);float cz=(float)(zLo+rnd.NextDouble()*(zHi-zLo));
            int n=6+rnd.Next(14);int kind=rnd.Next(3);int tint=rnd.Next(5);
            for(int i=0;i<n;i++)
            {
                float x=cx+((float)rnd.NextDouble()-.5f)*34,z=cz+((float)rnd.NextDouble()-.5f)*34;
                if(Mathf.Abs(x)<18)continue; // keep the corridor and the external camera clear
                float y=AtoGroundBuilder.GroundY(x,z)-.1f;float h=5.5f+(float)rnd.NextDouble()*8.5f;
                bool pine=kind==0||(kind==2&&rnd.NextDouble()<.4);
                var root=new Vector3(x,y,z);var lean=new Vector3(((float)rnd.NextDouble()-.5f)*.08f,0,((float)rnd.NextDouble()-.5f)*.08f);
                if(pine) Pine(trunks,conifers[rnd.Next(conifers.Length)],rnd,root,h,lean);
                else
                {
                    int m=rnd.NextDouble()<.08?5:(tint+(rnd.NextDouble()<.3?1:0))%5; // index 5 is a rare autumn crown
                    Broadleaf(trunks,leaves[m],rnd,root,h,lean);
                }
                placed++;
            }
        }
        { // Tree groups close to the line (18-40 m off the axis) so that the foreground of every view has trees, not only the distant clusters.
            var nr=new System.Random(20261109);
            for(double ss=track.RangeStart+20;ss<track.RangeEnd;ss+=48)
            {
                float side=((int)((ss-track.RangeStart)/48)%2==0)?-1:1;float gx=side*(19+(float)nr.NextDouble()*18);float gz=8.6f-(float)ss;int cnt=2+nr.Next(3);bool pg=nr.NextDouble()<.45;
                for(int i=0;i<cnt;i++)
                {
                    float x=gx+((float)nr.NextDouble()-.5f)*9,z=gz+((float)nr.NextDouble()-.5f)*14;if(Mathf.Abs(x)<18)continue;
                    float h=6f+(float)nr.NextDouble()*7f;var root=new Vector3(x,AtoGroundBuilder.GroundY(x,z)-.1f,z);var lean=new Vector3(((float)nr.NextDouble()-.5f)*.08f,0,((float)nr.NextDouble()-.5f)*.08f);
                    if(pg||nr.NextDouble()<.25)Pine(trunks,conifers[nr.Next(conifers.Length)],nr,root,h,lean);else Broadleaf(trunks,leaves[nr.Next(5)],nr,root,h,lean);
                    placed++;
                }
            }
        }
        Emit(parent,"Trees_Trunks",trunks,AtoGroundBuilder.Mat("ATO_Tree_Trunk",new Color(.27f,.20f,.14f),4f));
        var pineTints=new[]{new Color(.10f,.24f,.14f),new Color(.14f,.29f,.17f),new Color(.09f,.20f,.13f)};
        for(int i=0;i<conifers.Length;i++)Emit(parent,"Trees_Conifer"+i,conifers[i],AtoGroundBuilder.Mat("ATO_Tree_Conifer"+i,pineTints[i],6f));
        var leafTints=new[]{new Color(.22f,.40f,.13f),new Color(.30f,.45f,.15f),new Color(.17f,.33f,.12f),new Color(.38f,.48f,.17f),new Color(.26f,.38f,.16f),new Color(.58f,.40f,.14f)};
        for(int i=0;i<leaves.Length;i++)Emit(parent,"Trees_Broadleaf"+i,leaves[i],AtoGroundBuilder.Mat("ATO_Tree_Broadleaf"+i,leafTints[i],6f));
        Debug.Log("ATO_SCENERY trees="+placed);
    }

    // Conifer: tapered trunk and seven jagged, slightly drooping skirts that shrink toward the tip.
    static void Pine(MeshBuilder trunks,MeshBuilder crown,System.Random rnd,Vector3 root,float h,Vector3 lean)
    {
        trunks.Tapered(root,h*.035f+.09f,h*.012f+.03f,h*.55f,7,lean);
        float rr=h*(.20f+.06f*(float)rnd.NextDouble());int layers=7;
        for(int k=0;k<layers;k++)
        {
            float f=k/(float)layers;float r=rr*(1.25f-.95f*f)*(.9f+.2f*(float)rnd.NextDouble());
            var b=root+new Vector3(lean.x*h*f,h*(.14f+.76f*f),lean.z*h*f);
            crown.JaggedCone(b,r,h*.26f*(1.05f-.3f*f),11,rnd,.18f);
        }
    }

    // Broadleaf: tapered trunk with two or three limbs and a crown of several overlapping irregular lobes.
    static void Broadleaf(MeshBuilder trunks,MeshBuilder crown,System.Random rnd,Vector3 root,float h,Vector3 lean)
    {
        float th=h*(.40f+.10f*(float)rnd.NextDouble());
        trunks.Tapered(root,h*.040f+.09f,h*.022f+.05f,th,7,lean);
        var top=root+new Vector3(lean.x*th,th,lean.z*th);float rr=h*(.24f+.08f*(float)rnd.NextDouble());
        int limbs=2+rnd.Next(2);
        for(int i=0;i<limbs;i++)
        {
            float a=(float)(rnd.NextDouble()*Mathf.PI*2);var end=top+new Vector3(Mathf.Cos(a)*rr*.8f,rr*(.5f+.5f*(float)rnd.NextDouble()),Mathf.Sin(a)*rr*.8f);
            trunks.Segment(top-new Vector3(0,th*.12f,0),end,h*.02f+.05f);
        }
        int lobes=5+rnd.Next(4);
        for(int i=0;i<lobes;i++)
        {
            float a=(float)(rnd.NextDouble()*Mathf.PI*2),d=rr*(.15f+.75f*(float)rnd.NextDouble());
            float lr=rr*(.55f+.40f*(float)rnd.NextDouble());
            var c=top+new Vector3(Mathf.Cos(a)*d,rr*(.45f+.6f*(float)rnd.NextDouble())-rr*.15f,Mathf.Sin(a)*d);
            crown.NoisyBlob(c,lr,lr*(.70f+.2f*(float)rnd.NextDouble()),9,6,rnd,.16f);
        }
    }

    // ------------------------------------------------------------------ catenary-style masts and wires
    static void Masts(Transform parent,CabReviewTrack track)
    {
        var masts=new MeshBuilder();var wires=new MeshBuilder();
        for(double s=track.RangeStart+30;s<=track.RangeEnd-10;s+=MastSpacing)
        {
            var p=track.At(s).Position;float z=8.6f-(float)s;float baseY=p.y-.9f;
            masts.Cylinder(new Vector3(MastX,baseY,z),.14f,MastHeight,8);
            masts.Box(new Vector3(MastX/2f,baseY+WireHeight+.55f,z),new Vector3(Mathf.Abs(MastX),.10f,.10f));    // cantilever arm
            masts.Box(new Vector3(MastX,baseY-.1f,z),new Vector3(.55f,.35f,.55f));                              // foundation block
        }
        for(double s=track.RangeStart;s<track.RangeEnd;s+=5)
        {
            var a=track.At(s).Position;var b=track.At(System.Math.Min(track.RangeEnd,s+5)).Position;
            float sag=-.12f*Mathf.Sin((float)(((s-track.RangeStart)%MastSpacing)/MastSpacing*Mathf.PI));
            float sagB=-.12f*Mathf.Sin((float)(((s+5-track.RangeStart)%MastSpacing)/MastSpacing*Mathf.PI));if(((s+5-track.RangeStart)%MastSpacing)<5)sagB=0;
            wires.Segment(new Vector3(0,a.y+WireHeight+sag,8.6f-(float)s),new Vector3(0,b.y+WireHeight+sagB,8.6f-(float)(s+5)),.018f);
            wires.Segment(new Vector3(0,a.y+WireHeight+.55f+sag*.6f,8.6f-(float)s),new Vector3(0,b.y+WireHeight+.55f+sagB*.6f,8.6f-(float)(s+5)),.014f);
        }
        Emit(parent,"Masts",masts,AtoGroundBuilder.Mat("ATO_Mast_Concrete",new Color(.58f,.59f,.60f),2f));
        Emit(parent,"Wires",wires,AtoGroundBuilder.Mat("ATO_Wire_Metal",new Color(.42f,.43f,.45f),1f));
    }

    // ------------------------------------------------------------------ speed boards at the limit changes and a stop board
    static void Boards(Transform parent,CabReviewTrack track)
    {
        var digits=new MeshBuilder();
        var plate=AtoGroundBuilder.Mat("ATO_Board_White",new Color(.93f,.93f,.9f),1f);Glow(plate,new Color(.55f,.55f,.52f));
        var red=AtoGroundBuilder.Mat("ATO_Board_Red",new Color(.75f,.12f,.1f),1f);Glow(red,new Color(.35f,.05f,.04f));
        var post=AtoGroundBuilder.Mat("ATO_Mast_Concrete",new Color(.58f,.59f,.60f),2f);
        foreach(var (s,label) in new[]{(0.0,"12"),(250.0,"18"),(650.0,"10"),(900.0,"")})
        {
            var p=track.At(s).Position;float z=8.6f-(float)s;float y=p.y-.9f;
            var g=new GameObject(label==""?"Board_Stop_900":"Board_Speed_"+label);g.transform.SetParent(parent);
            var pole=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(pole.GetComponent<Collider>());pole.transform.SetParent(g.transform);
            pole.transform.position=new Vector3(MastX-.7f,y+1.2f,z);pole.transform.localScale=new Vector3(.12f,2.4f,.12f);pole.GetComponent<Renderer>().sharedMaterial=post;
            var board=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(board.GetComponent<Collider>());board.transform.SetParent(g.transform);
            board.transform.position=new Vector3(MastX-.7f,y+2.5f,z);board.transform.localScale=new Vector3(1.3f,1.0f,.06f);board.GetComponent<Renderer>().sharedMaterial=label==""?red:plate;
            for(int i=0;i<label.Length;i++)
            {   // digits are built from boxes (seven-segment style) so they are ordinary depth-tested geometry; the driver reads left to right along -X
                float cx=MastX-.7f+(label.Length==2?(i==0?.3f:-.3f):0);
                Digit(digits,label[i],new Vector3(cx,y+2.5f,z+.045f),.38f,.72f,.075f);
            }
        }
        Emit(parent,"Board_Digits",digits,AtoGroundBuilder.Mat("ATO_Board_Digit",new Color(.05f,.05f,.05f),1f));
    }
    // ------------------------------------------------------------------ 100 m distance markers (opposite the masts) and low bushes beside the formation
    static void Markers(Transform parent,CabReviewTrack track)
    {
        var posts=new MeshBuilder();var plates=new MeshBuilder();var digits=new MeshBuilder();
        for(int s=100;s<=1100;s+=100)
        {
            if(s>=840&&s<=960)continue; // the platform stands here
            float z=8.6f-s;float y=track.At(s).Position.y-.9f;const float x=3.7f;
            posts.Box(new Vector3(x,y+.45f,z),new Vector3(.08f,.9f,.08f));
            plates.Box(new Vector3(x,y+.95f,z),new Vector3(.56f,.34f,.04f));
            string label=(s/100).ToString();
            for(int i=0;i<label.Length;i++){float cx=x+(label.Length==2?(i==0?.14f:-.14f):0);Digit(digits,label[i],new Vector3(cx,y+.95f,z+.03f),.17f,.25f,.03f);}
        }
        Emit(parent,"Marker_Posts",posts,AtoGroundBuilder.Mat("ATO_Mast_Concrete",new Color(.58f,.59f,.60f),2f));
        var mp=AtoGroundBuilder.Mat("ATO_Board_White",new Color(.93f,.93f,.9f),1f);Glow(mp,new Color(.55f,.55f,.52f));Emit(parent,"Marker_Plates",plates,mp);
        Emit(parent,"Marker_Digits",digits,AtoGroundBuilder.Mat("ATO_Board_Digit",new Color(.05f,.05f,.05f),1f));
    }

    static void Bushes(Transform parent,CabReviewTrack track)
    {
        var rnd=new System.Random(20261108);var b=new MeshBuilder();
        double zLo=8.6-track.RangeEnd-40,zHi=8.6-track.RangeStart+40;
        for(int i=0;i<260;i++)
        {
            float side=rnd.NextDouble()<.5?-1:1;float x=side*(7.5f+(float)rnd.NextDouble()*30f);float z=(float)(zLo+rnd.NextDouble()*(zHi-zLo));
            if(x>2f&&z>8.6f-960&&z<8.6f-840)continue; // platform area
            float r=.5f+(float)rnd.NextDouble()*1.0f;float y=AtoGroundBuilder.GroundY(x,z);
            b.Blob(new Vector3(x,y+r*.35f,z),r,r*.55f,7,4);
            if(rnd.NextDouble()<.5)b.Blob(new Vector3(x+r*.7f,y+r*.28f,z+r*.3f),r*.7f,r*.42f,7,4);
        }
        Emit(parent,"Bushes",b,AtoGroundBuilder.Mat("ATO_Bush",new Color(.19f,.33f,.13f),6f));
    }

    // ------------------------------------------------------------------ station platform at the stop point (visual only, clear of the vehicle gauge)
    static void Platform(Transform parent,CabReviewTrack track)
    {
        float y0=track.At(900).Position.y;float zc=8.6f-900f;const float x0=2.15f,w=4.2f,len=96f,h=.5f;
        float baseY=AtoGroundBuilder.GroundY(x0+w,zc)-.2f;float top=y0+h;
        var slab=new MeshBuilder();var edge=new MeshBuilder();var roof=new MeshBuilder();var posts=new MeshBuilder();
        slab.Box(new Vector3(x0+w/2,(baseY+top)/2,zc),new Vector3(w,top-baseY,len));
        edge.Box(new Vector3(x0+.2f,top+.01f,zc),new Vector3(.4f,.03f,len));
        for(float dz=-45;dz<=-5;dz+=10){posts.Box(new Vector3(x0+w-.9f,top+1.7f,zc+dz),new Vector3(.12f,3.4f,.12f));}
        roof.Box(new Vector3(x0+w/2+.2f,top+3.45f,zc-25f),new Vector3(w+.6f,.12f,44f));
        Emit(parent,"Platform_Slab",slab,AtoGroundBuilder.Mat("ATO_Platform_Concrete",new Color(.62f,.62f,.60f),3f));
        Emit(parent,"Platform_Edge",edge,AtoGroundBuilder.Mat("ATO_Platform_Edge",new Color(.92f,.78f,.15f),1f));
        Emit(parent,"Platform_Posts",posts,AtoGroundBuilder.Mat("ATO_Mast_Concrete",new Color(.58f,.59f,.60f),2f));
        Emit(parent,"Platform_Roof",roof,AtoGroundBuilder.Mat("ATO_Platform_Roof",new Color(.20f,.30f,.45f),2f));
    }

    static void Digit(MeshBuilder b,char c,Vector3 center,float w,float h,float t)
    {
        // segments: a top, b upper right, c lower right, d bottom, e lower left, f upper left, g middle
        string on=c=='0'?"abcdef":c=='1'?"bc":c=='2'?"abged":c=='8'?"abcdefg":"";
        float hx=w/2,hy=h/2;
        foreach(char s in on)
        {
            Vector3 p;Vector3 size;
            switch(s)
            {
                case 'a':p=new Vector3(0,hy,0);size=new Vector3(w,t,.02f);break;
                case 'd':p=new Vector3(0,-hy,0);size=new Vector3(w,t,.02f);break;
                case 'g':p=Vector3.zero;size=new Vector3(w,t,.02f);break;
                case 'b':p=new Vector3(-hx,hy/2,0);size=new Vector3(t,hy,.02f);break;   // right as seen by the driver is -X
                case 'c':p=new Vector3(-hx,-hy/2,0);size=new Vector3(t,hy,.02f);break;
                case 'e':p=new Vector3(hx,-hy/2,0);size=new Vector3(t,hy,.02f);break;
                default:p=new Vector3(hx,hy/2,0);size=new Vector3(t,hy,.02f);break;
            }
            b.Box(center+p,size);
        }
    }

    // ------------------------------------------------------------------ haze, so distant detail fades instead of shimmering
    static void Haze()
    {
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0011f;RenderSettings.fogColor=new Color(.74f,.82f,.92f);
    }

    // Anisotropic filtering and a slight mip bias remove the moire on the ballast bed and sleepers in the distance.
    static void TuneTrackTextures()
    {
        foreach(var guid in AssetDatabase.FindAssets("Track_ t:Texture2D",new[]{"Assets/Models/ATO/Textures"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var imp=AssetImporter.GetAtPath(path) as TextureImporter;if(imp==null)continue;
            if(imp.anisoLevel==16&&Mathf.Approximately(imp.mipMapBias,-.3f))continue;
            imp.anisoLevel=16;imp.mipMapBias=-.3f;imp.SaveAndReimport();
        }
        // Far away the polished rail head and the pale concrete sleepers must still read against the dark ballast: a fully metallic rail
        // reflects almost nothing here and turns into a dark thread, so ease the metallic value and lift both materials with a little emission.
        foreach(var (matName,metallic,emit) in new[]{("Track_Rail_Steel",.45f,new Color(.34f,.32f,.30f)),("Track_Sleeper_Concrete",-1f,new Color(.13f,.13f,.13f))})
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ATO/"+matName+".mat");if(m==null)continue;
            if(metallic>=0)m.SetFloat("_Metallic",metallic);
            m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;m.SetColor("_EmissionColor",emit);EditorUtility.SetDirty(m);
        }
        foreach(var guid in AssetDatabase.FindAssets("Track_Ballast t:Material"))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(m!=null&&m.HasProperty("_BumpScale")){m.SetFloat("_BumpScale",.5f);EditorUtility.SetDirty(m);}
        }
    }

    // Board faces point away from the sun, so a little emission keeps the white plates and the digits readable.
    static void Glow(Material m,Color c){m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;m.SetColor("_EmissionColor",c);EditorUtility.SetDirty(m);}

    static void Emit(Transform parent,string name,MeshBuilder b,Material mat)
    {
        if(b.Count==0)return;var go=new GameObject(name);go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh=b.ToMesh(name);go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        var r=go.GetComponent<MeshRenderer>();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
    }

    // Small procedural mesh helper: cylinder, cone, blob (low-poly ellipsoid), box and line segment.
    sealed class MeshBuilder
    {
        readonly List<Vector3> v=new List<Vector3>();readonly List<int> t=new List<int>();
        public int Count=>v.Count;
        public Mesh ToMesh(string name)
        {
            var m=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.SetVertices(v);m.SetTriangles(t,0);
            var uv=new Vector2[v.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(v[i].x*.3f,v[i].z*.3f+v[i].y*.2f);m.uv=uv;
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        void Quad(int a,int b,int c,int d){t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d);}
        public void Cylinder(Vector3 bottom,float radius,float height,int seg)
        {
            int o=v.Count;
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;var d=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);v.Add(bottom+d);v.Add(bottom+d+Vector3.up*height);}
            for(int i=0;i<seg;i++){int j=(i+1)%seg;Quad(o+i*2,o+j*2,o+j*2+1,o+i*2+1);}
        }
        public void Cone(Vector3 bottom,float radius,float height,int seg)
        {
            int o=v.Count;v.Add(bottom+Vector3.up*height);
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;v.Add(bottom+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
            for(int i=0;i<seg;i++){int j=(i+1)%seg;t.Add(o);t.Add(o+1+j);t.Add(o+1+i);}
        }
        public void Blob(Vector3 center,float radius,float halfHeight,int seg,int rings)
        {
            int o=v.Count;
            for(int r=0;r<=rings;r++){float p=Mathf.PI*r/rings;float y=Mathf.Cos(p)*halfHeight,rr=Mathf.Sin(p)*radius;
                for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;v.Add(center+new Vector3(Mathf.Cos(a)*rr,y,Mathf.Sin(a)*rr));}}
            for(int r=0;r<rings;r++)for(int i=0;i<seg;i++){int j=(i+1)%seg;int a=o+r*seg+i,b=o+r*seg+j,c=o+(r+1)*seg+j,d=o+(r+1)*seg+i;t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d);}
        }
        public void Box(Vector3 center,Vector3 size)
        {   // 24 vertices, one outward-facing quad per face, so normals are flat and lighting is correct
            var h=size*.5f;
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                var n=Vector3.zero;n[axis]=sign;var u=Vector3.zero;u[(axis+1)%3]=1;var w=Vector3.zero;w[(axis+2)%3]=1;
                var c=center+Vector3.Scale(n,h);var du=Vector3.Scale(u,h);var dw=Vector3.Scale(w,h);
                int o=v.Count;v.Add(c-du-dw);v.Add(c+du-dw);v.Add(c+du+dw);v.Add(c-du+dw);
                if(Vector3.Dot(Vector3.Cross(v[o+1]-v[o],v[o+2]-v[o]),n)>0)Quad(o,o+1,o+2,o+3);else Quad(o,o+3,o+2,o+1);
            }
        }
        public void Tapered(Vector3 bottom,float rBottom,float rTop,float height,int seg,Vector3 lean)
        {
            int o=v.Count;
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));v.Add(bottom+d*rBottom);v.Add(bottom+d*rTop+new Vector3(lean.x*height,height,lean.z*height));}
            for(int i=0;i<seg;i++){int j=(i+1)%seg;Quad(o+i*2,o+j*2,o+j*2+1,o+i*2+1);}
        }
        public void JaggedCone(Vector3 bottom,float radius,float height,int seg,System.Random rnd,float jag)
        {
            int o=v.Count;v.Add(bottom+Vector3.up*height);
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;float r=radius*(1f-jag*.5f+jag*(float)rnd.NextDouble());v.Add(bottom+new Vector3(Mathf.Cos(a)*r,-height*.12f*(float)rnd.NextDouble(),Mathf.Sin(a)*r));}
            for(int i=0;i<seg;i++){int j=(i+1)%seg;t.Add(o);t.Add(o+1+j);t.Add(o+1+i);}
            int c=v.Count;v.Add(bottom);for(int i=0;i<seg;i++){int j=(i+1)%seg;t.Add(c);t.Add(o+1+i);t.Add(o+1+j);} // underside, so the skirt is not see-through
        }
        public void NoisyBlob(Vector3 center,float radius,float halfHeight,int seg,int rings,System.Random rnd,float amp)
        {
            int o=v.Count;
            for(int r=0;r<=rings;r++){float p=Mathf.PI*r/rings;float y=Mathf.Cos(p)*halfHeight,rr=Mathf.Sin(p)*radius;
                for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;float k=1f-amp+2f*amp*(float)rnd.NextDouble();if(r==0||r==rings)k=1;v.Add(center+new Vector3(Mathf.Cos(a)*rr*k,y*(r==0||r==rings?1:(.9f+.2f*(float)rnd.NextDouble())),Mathf.Sin(a)*rr*k));}}
            for(int r=0;r<rings;r++)for(int i=0;i<seg;i++){int j=(i+1)%seg;int a=o+r*seg+i,b=o+r*seg+j,c=o+(r+1)*seg+j,d=o+(r+1)*seg+i;t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d);}
        }
        public void Segment(Vector3 a,Vector3 b,float thickness)
        {
            var dir=b-a;float len=dir.magnitude;if(len<1e-4f)return;var rot=Quaternion.LookRotation(dir.normalized,Vector3.up);var h=thickness*.5f;
            int o=v.Count;
            for(int i=0;i<8;i++){var local=new Vector3((i&1)==0?-h:h,(i&2)==0?-h:h,(i&4)==0?0:len);v.Add(a+rot*local);}
            int[] f={0,2,3,1, 4,5,7,6, 0,1,5,4, 2,6,7,3, 0,4,6,2, 1,3,7,5};
            for(int k=0;k<6;k++){int p=o+f[k*4],q=o+f[k*4+1],r=o+f[k*4+2],s=o+f[k*4+3];Quad(p,q,r,s);Quad(p,s,r,q);}
        }
    }
}
