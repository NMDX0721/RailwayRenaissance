using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabCirculators
{
    [Serializable] sealed class ManufacturerProfile { public string manufacturer,brandId,model; }
    static Mesh Persist(string name,Mesh mesh)
    {
        string path="Assets/Resources/Cab/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        mesh.name=name;
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        // Updating native serialized data alone can retain an old GPU vertex/UV buffer.
        existing.Clear();existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;
        existing.subMeshCount=mesh.subMeshCount;
        for(int sub=0;sub<mesh.subMeshCount;sub++)existing.SetTriangles(mesh.GetTriangles(sub),sub);
        existing.bounds=mesh.bounds;existing.UploadMeshData(false);
        UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);return existing;
    }
    static Mesh Cage()
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var normals=new List<Vector3>();var uv=new List<Vector2>();
        void Tube(Vector3 a,Vector3 b,float radius)
        {
            var direction=(b-a).normalized;var u=Vector3.Cross(direction,Mathf.Abs(direction.y)>.9f?Vector3.right:Vector3.up).normalized;
            var v=Vector3.Cross(direction,u);int start=vertices.Count;
            for(int end=0;end<2;end++)for(int i=0;i<12;i++)
            {float angle=i*Mathf.PI/6;var n=u*Mathf.Cos(angle)+v*Mathf.Sin(angle);vertices.Add((end==0?a:b)+radius*n);normals.Add(n);uv.Add(new Vector2(end, i/12f));}
            for(int i=0;i<12;i++){int j=(i+1)%12;triangles.AddRange(new[]{start+i,start+j,start+12+j,start+i,start+12+j,start+12+i});}
        }
        void Ring(float radius,float z,float wire)
        {
            int start=vertices.Count;const int segments=384,sides=16;
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;
                var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var n=radial*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b);
                vertices.Add(radial*radius+Vector3.forward*z+wire*n);normals.Add(n);uv.Add(new Vector2(i/(float)segments,j/(float)sides));
            }
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {int a=start+i*sides+j,b=start+((i+1)%segments)*sides+j,c=start+((i+1)%segments)*sides+(j+1)%sides,d=start+i*sides+(j+1)%sides;triangles.AddRange(new[]{a,b,c,a,c,d});}
        }
        Vector3 Point(float radius,float angle,float z)=>new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,z);
        foreach(float radius in new[]{.028f,.042f,.056f,.070f,.084f,.094f})
        {
            float z=.072f-.044f*Mathf.Pow(radius/.094f,2);
            Ring(radius,z,radius>.09f?.0018f:.0010f);
        }
        Ring(.094f,-.016f,.0018f);
        for(int spoke=0;spoke<12;spoke++)
        {
            float angle=spoke*Mathf.PI/6;int start=vertices.Count;const int steps=32,sides=12;
            var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            var tangent=new Vector3(-Mathf.Sin(angle),Mathf.Cos(angle),0);
            for(int k=0;k<=steps;k++)
            {
                float r=Mathf.Lerp(.012f,.094f,k/(float)steps),z=.072f-.044f*Mathf.Pow(r/.094f,2);
                float slope=-.088f*r/(.094f*.094f);var along=(radial+Vector3.forward*slope).normalized;
                var other=Vector3.Cross(along,tangent).normalized;
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides;var n=tangent*Mathf.Cos(a)+other*Mathf.Sin(a);
                    vertices.Add(radial*r+Vector3.forward*z+n*.0012f);normals.Add(n);uv.Add(new Vector2(k/(float)steps,j/(float)sides));
                }
            }
            for(int k=0;k<steps;k++)for(int j=0;j<sides;j++)
            {int a=start+k*sides+j,b=start+(k+1)*sides+j,c=start+(k+1)*sides+(j+1)%sides,d=start+k*sides+(j+1)%sides;triangles.AddRange(new[]{a,c,b,a,d,c});}
            if(spoke%3==0)
            {
                Tube(Point(.094f,angle,-.016f),Point(.094f,angle,.028f),.0018f);
                // Rear guard load path ends inside the stationary motor nose, behind the rotor.
                Tube(Point(.030f,angle,-.020f),Point(.094f,angle,-.016f),.0018f);
            }
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.RecalculateBounds();return Persist("Cab_Circulator_Wire_Cage_V3",mesh);
    }
    static Mesh Blade()
    {
        const int n=64;var vertices=new List<Vector3>();var triangles=new List<int>();
        // Rounded swept paddle, pitched across its chord; closed 1.4 mm sheet.
        Vector3 Surface(float x,float y,float offset)=>new Vector3(x,y,.30f*y+.002f*Mathf.Sin((x-.008f)/.076f*Mathf.PI)+offset);
        for(int side=0;side<2;side++)
        {
            float offset=side==0?-.0007f:.0007f;
            vertices.Add(Surface(.046f,.005f,offset));
            for(int i=0;i<n;i++)
            {float a=i*Mathf.PI*2/n;float x=.046f+.038f*Mathf.Cos(a);float y=.005f+.026f*Mathf.Sin(a)+.005f*Mathf.Cos(a);vertices.Add(Surface(x,y,offset));}
        }
        for(int i=0;i<n;i++)
        {
            int a=1+i,b=1+(i+1)%n,c=n+1+a,d=n+1+b;
            triangles.AddRange(new[]{0,b,a,n+1,c,d,a,b,d,a,d,c});
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x/.09f,v.y/.06f+.5f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();return Persist("Cab_Circulator_Pitched_Blade_V3",mesh);
    }
    static Mesh SmoothCylinder()
    {
        const int n=96;var v=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        for(int end=0;end<2;end++)for(int i=0;i<=n;i++)
        {float a=i*Mathf.PI*2/n;var normal=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));v.Add(normal*.5f+Vector3.up*(end==0?-1:1));normals.Add(normal);uv.Add(new Vector2(i/(float)n,end));}
        for(int i=0;i<n;i++){int a=i,b=i+1,c=n+1+i,d=c+1;triangles.AddRange(new[]{a,c,d,a,d,b});}
        for(int end=0;end<2;end++)
        {
            int start=v.Count;float y=end==0?-1:1;var normal=Vector3.up*y;v.Add(new Vector3(0,y,0));normals.Add(normal);uv.Add(new Vector2(.5f,.5f));
            for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Cos(a)*.5f,y,Mathf.Sin(a)*.5f));normals.Add(normal);uv.Add(new Vector2(Mathf.Cos(a)*.5f+.5f,Mathf.Sin(a)*.5f+.5f));}
            for(int i=0;i<n;i++){int a=start+1+i,b=start+1+(i+1)%n;triangles.AddRange(end==0?new[]{start,a,b}:new[]{start,b,a});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return Persist("Cab_Circulator_Smooth_Cylinder_V3",mesh);
    }
    static Mesh PanHead()
    {
        var profile=new[]{new Vector2(.48f,0),new Vector2(.50f,.20f),new Vector2(.47f,.55f),new Vector2(.40f,.78f)};
        var vertices=new List<Vector3>();var shell=new List<int>();var floor=new List<int>();const int n=96;
        for(int row=0;row<profile.Length;row++)for(int i=0;i<=n;i++)
        {float a=i*Mathf.PI*2/n;vertices.Add(new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x));}
        for(int row=0;row<profile.Length-1;row++)for(int i=0;i<n;i++)
        {int a=row*(n+1)+i,b=a+1,c=a+n+1,d=c+1;shell.AddRange(new[]{a,c,d,a,d,b});}
        var circle=new List<Vector2>();for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;circle.Add(new Vector2(.4f*Mathf.Cos(a),.4f*Mathf.Sin(a)));}
        List<Vector2> Clip(List<Vector2> input,float z,bool greater)
        {
            var result=new List<Vector2>();
            for(int i=0;i<input.Count;i++)
            {
                var a=input[i];var b=input[(i+1)%input.Count];bool ina=greater?a.y>=z:a.y<=z, inb=greater?b.y>=z:b.y<=z;
                if(ina)result.Add(a);
                if(ina!=inb)result.Add(Vector2.Lerp(a,b,(z-a.y)/(b.y-a.y)));
            }
            return result;
        }
        void Face(List<Vector2> polygon,float height,List<int> indices)
        {
            int start=vertices.Count;var center=Vector2.zero;foreach(var v in polygon)center+=v;center/=polygon.Count;
            vertices.Add(new Vector3(center.x,height,center.y));foreach(var v in polygon)vertices.Add(new Vector3(v.x,height,v.y));
            for(int i=0;i<polygon.Count;i++)indices.AddRange(new[]{start,start+1+(i+1)%polygon.Count,start+1+i});
        }
        Face(Clip(circle,.045f,true),.78f,shell);Face(Clip(circle,-.045f,false),.78f,shell);
        var recess=Clip(Clip(circle,.045f,false),-.045f,true);Face(recess,.48f,floor);
        for(int i=0;i<recess.Count;i++)
        {
            var a=recess[i];var b=recess[(i+1)%recess.Count];int start=vertices.Count;
            vertices.AddRange(new[]{new Vector3(a.x,.78f,a.y),new Vector3(b.x,.78f,b.y),new Vector3(a.x,.48f,a.y),new Vector3(b.x,.48f,b.y)});
            shell.AddRange(new[]{start,start+3,start+1,start,start+2,start+3});
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.subMeshCount=2;mesh.SetTriangles(shell,0);mesh.SetTriangles(floor,1);
        mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x+.5f,v.z+.5f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();return Persist("Cab_Fan_Recessed_Slotted_Head_V6",mesh);
    }
    static Mesh Yoke(Vector3 left,Vector3 right)
    {
        // One folded 10 x 3 mm steel strap with rounded shoulders and no pipe end caps.
        var path=new List<Vector3>();var lt=new Vector3(-.050f,.070f,-.060f);var rt=new Vector3(.050f,.070f,-.060f);
        var l1=Vector3.Lerp(left,lt,.82f);var l2=Vector3.Lerp(lt,rt,.14f);
        var r1=Vector3.Lerp(lt,rt,.86f);var r2=Vector3.Lerp(rt,right,.18f);
        path.Add(left);path.Add(l1);
        for(int i=1;i<=12;i++){float t=i/12f;path.Add((1-t)*(1-t)*l1+2*(1-t)*t*lt+t*t*l2);}
        path.Add(r1);
        for(int i=1;i<=12;i++){float t=i/12f;path.Add((1-t)*(1-t)*r1+2*(1-t)*t*rt+t*t*r2);}
        path.Add(right);
        var vertices=new List<Vector3>();var indices=new List<int>();
        for(int i=0;i<path.Count;i++)
        {
            var along=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)]).normalized;
            var width=Vector3.Cross(Vector3.forward,along).normalized*.005f;
            vertices.Add(path[i]+width+Vector3.forward*.0015f);vertices.Add(path[i]-width+Vector3.forward*.0015f);
            vertices.Add(path[i]-width-Vector3.forward*.0015f);vertices.Add(path[i]+width-Vector3.forward*.0015f);
        }
        for(int i=0;i<path.Count-1;i++)for(int j=0;j<4;j++)
        {int a=i*4+j,b=i*4+(j+1)%4,c=(i+1)*4+j,d=(i+1)*4+(j+1)%4;indices.AddRange(new[]{a,b,d,a,d,c});}
        indices.AddRange(new[]{0,2,1,0,3,2});int last=(path.Count-1)*4;indices.AddRange(new[]{last,last+1,last+2,last,last+2,last+3});
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return Persist("Cab_Fan_Folded_Yoke_V4_"+(left.z>right.z?"L":"R"),mesh);
    }
    static void Print(Transform parent,string name,string image,Vector3 position,Vector2 size,Quaternion rotation,bool circular=false)
    {
        string path="Assets/Resources/Cab/"+image+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Generate circulator prints first: "+image);
        importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        string materialPath="Assets/Resources/Cab/"+image+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Metallic",.18f);material.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        var mesh=new Mesh();float x=size.x*.5f,y=size.y*.5f;
        mesh.vertices=new[]{new Vector3(-x,-y,0),new Vector3(x,-y,0),new Vector3(-x,y,0),new Vector3(x,y,0)};
        mesh.uv=new[]{Vector2.right,Vector2.zero,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,2,1,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
        if(circular)
        {
            const int n=192;var vertices=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{new Vector2(.5f,.5f)};var triangles=new List<int>();
            for(int i=0;i<n;i++){float angle=i*Mathf.PI*2/n;float u=Mathf.Cos(angle),v=Mathf.Sin(angle);vertices.Add(new Vector3(u*x,v*y,0));uv.Add(new Vector2(.5f-u*.5f,v*.5f+.5f));}
            for(int i=0;i<n;i++)triangles.AddRange(new[]{0,1+i,1+(i+1)%n});
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        var go=new GameObject(name);go.layer=30;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=rotation;
        go.AddComponent<MeshFilter>().sharedMesh=Persist(name+"_Face_V2",mesh);go.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    public static void Build(Transform fittings,Transform body)
    {
        var source=Resources.Load<TextAsset>("Cab/circulator_profile");
        if(source==null)throw new InvalidOperationException("Missing circulator manufacturer profile");
        var profile=JsonUtility.FromJson<ManufacturerProfile>(source.text);
        if(profile==null||string.IsNullOrWhiteSpace(profile.manufacturer)||string.IsNullOrWhiteSpace(profile.brandId)||string.IsNullOrWhiteSpace(profile.model))throw new InvalidOperationException("Incomplete circulator manufacturer profile");
        foreach(var reserved in body.GetComponentsInChildren<Transform>().Where(t=>t.name=="Ceiling_Fan_Reserved").ToArray())UnityEngine.Object.DestroyImmediate(reserved.gameObject);
        Material Finish(string name,Color color,float metal,float smooth)
        {
            string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
            m.SetFloat("_SpecularHighlights",1);m.SetFloat("_EnvironmentReflections",1);
            m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            if(!name.Contains("Rubber"))
            {
                bool fine=name.Contains("Selector")||name.Contains("Fastener");
                string texturePath="Assets/Resources/Cab/"+name+(fine?"_Surface_V6.asset":"_Surface_V4.asset");
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if(texture==null)
                {
                    const int size=256;texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name=name+"_Surface_V4",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
                    var pixels=new Color[size*size];
                    for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                    {
                        float grain=Mathf.PerlinNoise(x*(fine?.67f:.12f),y*(fine?.67f:.8f)),broad=Mathf.PerlinNoise(x*.031f,y*.024f);
                        float value=fine?.975f+.015f*grain+.01f*broad:.91f+.07f*broad+.02f*grain;
                        pixels[y*size+x]=new Color(value,value,value,fine?.90f+.06f*grain:.75f+.20f*grain);
                    }
                    texture.SetPixels(pixels);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,texturePath);
                }
                m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");m.SetFloat("_SmoothnessTextureChannel",1);
                m.SetTexture("_BaseMap",texture);m.SetTextureScale("_BaseMap",Vector2.one);
            }
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        var cageFinish=Finish("Cab_Fan_Satin_Steel",new Color(.49f,.52f,.54f),.88f,.38f);
        var enamel=Finish("Cab_Fan_Grey_Green_Enamel",new Color(.095f,.17f,.20f),.02f,.31f);
        var bladeFinish=Finish("Cab_Fan_Blade_Paint",new Color(.73f,.74f,.71f),.88f,.43f);
        var rubber=Finish("Cab_Fan_Cable_Rubber",new Color(.075f,.08f,.07f),0,.18f);
        var mounting=Finish("Cab_Fan_Mount_Enamel",new Color(.66f,.61f,.48f),.02f,.27f);
        var cage=Cage();var blade=Blade();var cylinder=SmoothCylinder();
        var centreFinish=Finish("Cab_Fan_Centre_Satin_V3",new Color(.10f,.22f,.24f),.03f,.27f);
        var stemFinish=Finish("Cab_Fan_Drop_Stem_Steel_V4",new Color(.22f,.24f,.25f),.78f,.32f);
        var fastenerFinish=Finish("Cab_Fan_Fastener_Steel_V4",new Color(.58f,.60f,.61f),.92f,.36f);
        var slotFinish=Finish("Cab_Fan_Fastener_Recess_V4",new Color(.16f,.18f,.19f),.75f,.24f);
        var selectorFinish=Finish("Cab_Fan_Selector_Graphite_V5",new Color(.20f,.23f,.235f),.04f,.32f);
        var pointerFinish=Finish("Cab_Fan_Selector_Ivory_Index_V5",new Color(.84f,.83f,.70f),.02f,.24f);
        var screwMesh=PanHead();
        var logoInk=Finish("Cab_Fan_Wing_Rail_Logo_Ink_V1",new Color(.055f,.095f,.12f),.05f,.20f);
        foreach(int side in new[]{-1,1})
        {
            var root=new GameObject(side>0?"Driver_Circulation_Fan":"Assistant_Circulation_Fan").transform;
            root.SetParent(fittings,false);root.localPosition=new Vector3(side*1.02f,1.93f,.30f);root.gameObject.layer=30;
            GameObject Part(string name,Transform parent,Vector3 p,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube)
            {
                var g=GameObject.CreatePrimitive(type);g.name=name;g.layer=30;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
                g.GetComponent<Renderer>().sharedMaterial=mat;if(type==PrimitiveType.Cylinder)g.GetComponent<MeshFilter>().sharedMesh=cylinder;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
            }
            void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material mat)
            {
                var g=Part(name,parent,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),mat,PrimitiveType.Cylinder);
                g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            }
            void Screw(string name,Transform parent,Vector3 contact,Vector3 outward,float diameter)
            {
                var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=contact;
                float clocking=Mathf.Repeat(contact.x*937+contact.z*613+contact.y*479,180);
                g.localRotation=Quaternion.FromToRotation(Vector3.up,outward)*Quaternion.AngleAxis(clocking,Vector3.up);
                float washer=diameter*.08f;
                Part("Captive_Washer",g,new Vector3(0,washer*.5f,0),new Vector3(diameter*1.18f,washer*.5f,diameter*1.18f),fastenerFinish,PrimitiveType.Cylinder);
                var cap=new GameObject("Rounded_Slotted_Head");cap.layer=30;cap.transform.SetParent(g,false);cap.transform.localPosition=new Vector3(0,washer,0);cap.transform.localScale=new Vector3(diameter,diameter*.4f,diameter);
                cap.AddComponent<MeshFilter>().sharedMesh=screwMesh;cap.AddComponent<MeshRenderer>().sharedMaterials=new[]{fastenerFinish,slotFinish};

            }
            Part("Fan_Roof_Isolator",root,new Vector3(0,.167f,-.060f),new Vector3(.084f,.006f,.068f),rubber);
            Part("Fan_Roof_Flange",root,new Vector3(0,.161f,-.060f),new Vector3(.080f,.006f,.064f),mounting);
            foreach(float x in new[]{-.028f,.028f})foreach(float z in new[]{-.080f,-.040f})
                Screw("Fan_Flange_Screw",root,new Vector3(x,.158f,z),Vector3.down,.0065f);
            Rod("Fan_Rigid_Drop_Stem",root,new Vector3(0,.161f,-.06f),new Vector3(0,.070f,-.06f),.006f,stemFinish);
            Part("Fan_Stem_Upper_Collar",root,new Vector3(0,.151f,-.060f),new Vector3(.020f,.006f,.020f),fastenerFinish,PrimitiveType.Cylinder);
            Part("Fan_Stem_Lower_Collar",root,new Vector3(0,.074f,-.060f),new Vector3(.018f,.004f,.018f),fastenerFinish,PrimitiveType.Cylinder);
            var head=new GameObject("Fan_Adjusted_Head").transform;head.SetParent(root,false);
            head.localRotation=Quaternion.LookRotation(new Vector3(-side*.40f,-.35f,1));
            var motor=Part("Fan_Motor_Housing",head,new Vector3(0,0,-.044f),new Vector3(.068f,.037f,.068f),enamel,PrimitiveType.Cylinder);
            motor.transform.localRotation=Quaternion.Euler(90,0,0);
            var left=root.InverseTransformPoint(head.TransformPoint(new Vector3(-.044f,0,-.044f)));
            var right=root.InverseTransformPoint(head.TransformPoint(new Vector3(.044f,0,-.044f)));
            var yoke=new GameObject("Fan_Continuous_Folded_Yoke");yoke.layer=30;yoke.transform.SetParent(root,false);
            yoke.AddComponent<MeshFilter>().sharedMesh=Yoke(left,right);yoke.AddComponent<MeshRenderer>().sharedMaterial=mounting;
            foreach(float sign in new[]{-1f,1f})
            {
                var bearingRoot=root.InverseTransformPoint(head.TransformPoint(new Vector3(sign*.033f,0,-.044f)));
                var outside=root.InverseTransformPoint(head.TransformPoint(new Vector3(sign*.044f,0,-.044f)));
                Rod("Fan_Head_Pivot_Axle",root,bearingRoot,outside,.004f,fastenerFinish);
                Screw("Fan_Pivot_Captive_Screw",head,new Vector3(sign*.044f,0,-.044f),Vector3.right*sign,.007f);
            }
            Rod("Fan_Motor_Output_Shaft",head,new Vector3(0,0,-.012f),new Vector3(0,0,.006f),.004f,cageFinish);
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;Part("Fan_Motor_Rear_Vent",head,new Vector3(Mathf.Cos(angle)*.029f,Mathf.Sin(angle)*.029f,-.082f),new Vector3(.004f,.0025f,.001f),rubber);
            }
            Part("Fan_Rear_Rating_Plate_Base",head,new Vector3(0,0,-.08125f),new Vector3(.039f,.030f,.0005f),cageFinish);
            Print(head,"Fan_Rear_Manufacturer_Rating_Plate","Cab_Circulator_Rating_Plate_V1",new Vector3(0,0,-.08156f),new Vector2(.036f,.027f),Quaternion.Euler(0,180,0));
            foreach(float sign in new[]{-1f,1f})Screw("Fan_Rating_Plate_Screw",head,new Vector3(sign*.016f,sign*.012f,-.0815f),Vector3.back,.0025f);
            var guard=new GameObject("Fan_Wire_Guard");guard.layer=30;guard.transform.SetParent(head,false);guard.AddComponent<MeshFilter>().sharedMesh=cage;guard.AddComponent<MeshRenderer>().sharedMaterial=cageFinish;
            for(int i=0;i<4;i++)
            {
                float angle=i*Mathf.PI/2;
                Part("Fan_Rear_Guard_Anchor",head,new Vector3(Mathf.Cos(angle)*.031f,Mathf.Sin(angle)*.031f,-.022f),new Vector3(.009f,.009f,.012f),cageFinish);
            }
            var rotor=new GameObject("Fan_Three_Blade_Rotor").transform;rotor.SetParent(head,false);rotor.localPosition=new Vector3(0,0,.005f);
            for(int i=0;i<3;i++)
            {
                var g=new GameObject("Fan_Pitched_Blade");g.layer=30;g.transform.SetParent(rotor,false);g.transform.localRotation=Quaternion.Euler(0,0,i*120);
                g.AddComponent<MeshFilter>().sharedMesh=blade;g.AddComponent<MeshRenderer>().sharedMaterial=bladeFinish;
            }
            var hub=Part("Fan_Rotor_Hub",rotor,Vector3.zero,new Vector3(.032f,.006f,.032f),cageFinish,PrimitiveType.Cylinder);hub.transform.localRotation=Quaternion.Euler(90,0,0);
            var badge=Part("Fan_Guard_Centre_Cap",head,new Vector3(0,0,.072f),new Vector3(.042f,.002f,.042f),centreFinish,PrimitiveType.Cylinder);badge.transform.localRotation=Quaternion.Euler(90,0,0);
            var rim=Part("Fan_Manufacturer_Medallion_Metal_Rim",head,new Vector3(0,0,.0739f),new Vector3(.043f,.0003f,.043f),fastenerFinish,PrimitiveType.Cylinder);rim.transform.localRotation=Quaternion.Euler(90,0,0);
            Print(head,"Fan_Manufacturer_Logo_"+profile.brandId,"Cab_Circulator_Manufacturer_Medallion_V1",new Vector3(0,0,.07456f),new Vector2(.041f,.041f),Quaternion.identity,true);
            var panel=new GameObject("Fan_Desk_Speed_Control").transform;panel.SetParent(root,false);
            // Logical ownership remains with the fan; physical placement is on the worktop.
            panel.position=body.TransformPoint(new Vector3(side>0?.85f:-1.13f,.8405f,.23f));
            panel.rotation=body.rotation;
            Part("Fan_Control_Metal_Plate",panel,Vector3.zero,new Vector3(.068f,.003f,.070f),mounting);
            foreach(float x in new[]{-.027f,.027f})foreach(float z in new[]{-.027f,.027f})
                Screw("Fan_Control_Fixing_Screw",panel,new Vector3(x,.0015f,z),Vector3.up,.0045f);
            var knob=new GameObject("Fan_Three_Position_Knob");knob.layer=30;knob.transform.SetParent(panel,false);knob.transform.localPosition=new Vector3(-.012f,.0015f,0);
            Part("Fan_Speed_Selector_Metal_Bezel",knob.transform,new Vector3(0,.0008f,0),new Vector3(.024f,.0008f,.024f),fastenerFinish,PrimitiveType.Cylinder);
            Part("Fan_Speed_Selector_Grip",knob.transform,new Vector3(0,.004f,0),new Vector3(.021f,.0025f,.021f),selectorFinish,PrimitiveType.Cylinder);
            for(int rib=0;rib<16;rib++)
            {
                float angle=rib*Mathf.PI/8;
                var groove=Part("Fan_Selector_Grip_Flute",knob.transform,new Vector3(Mathf.Sin(angle)*.01045f,.0038f,Mathf.Cos(angle)*.01045f),new Vector3(.00045f,.0032f,.0003f),stemFinish);
                groove.transform.localRotation=Quaternion.Euler(0,rib*22.5f,0);
            }
            Part("Fan_Knob_Index",knob.transform,new Vector3(0,.00658f,.0045f),new Vector3(.0012f,.00016f,.007f),pointerFinish);
            Print(panel,"Fan_Control_Indexed_Scale","Cab_Circulator_Control_Scale_V2",new Vector3(0,.00162f,0),new Vector2(.068f,.070f),Quaternion.Euler(-90,0,0));
            Part("Fan_Swing_Rocker_Metal_Bezel",panel,new Vector3(.021f,.0025f,0),new Vector3(.017f,.002f,.028f),fastenerFinish);
            Part("Fan_Swing_Rocker_Recess",panel,new Vector3(.021f,.00455f,0),new Vector3(.0145f,.0002f,.024f),rubber);
            var swing=new GameObject("Fan_Desk_Swing_Selector");swing.layer=30;swing.transform.SetParent(panel,false);swing.transform.localPosition=new Vector3(.021f,.0065f,0);
            Part("Fan_Swing_Rocker_Actuator",swing.transform,Vector3.zero,new Vector3(.013f,.004f,.022f),selectorFinish);
            Part("Fan_Swing_Rocker_Tactile_Line",swing.transform,new Vector3(0,.0021f,0),new Vector3(.007f,.0002f,.0007f),pointerFinish);
            var controlHit=panel.gameObject.AddComponent<BoxCollider>();controlHit.center=new Vector3(0,.005f,0);controlHit.size=new Vector3(.068f,.018f,.070f);panel.gameObject.layer=30;
            var glandRoot=root.InverseTransformPoint(head.TransformPoint(new Vector3(0,.023f,-.075f)));
            Rod("Fan_Flexible_Power_Lead",root,glandRoot,new Vector3(0,.09f,-.074f),.0025f,rubber);
            Rod("Fan_Power_Lead",root,new Vector3(0,.09f,-.074f),new Vector3(0,.165f,-.06f),.0025f,rubber);
            Part("Fan_Cable_Motor_Gland",head,new Vector3(0,.023f,-.075f),new Vector3(.010f,.010f,.012f),rubber);
            Part("Fan_Cable_P_Clip",root,new Vector3(0,.09f,-.073f),new Vector3(.012f,.006f,.009f),cageFinish);
            Part("Fan_Cable_P_Clip",root,new Vector3(0,.13f,-.0665f),new Vector3(.012f,.006f,.009f),cageFinish);

            var loopObject=new GameObject("Fan_Service_Loop_Cable");loopObject.layer=30;loopObject.transform.SetParent(root,false);
            var loop=loopObject.AddComponent<LineRenderer>();loop.useWorldSpace=true;loop.positionCount=13;loop.widthMultiplier=.005f;loop.numCapVertices=6;loop.numCornerVertices=4;loop.generateLightingData=true;loop.sharedMaterial=rubber;
            root.Find("Fan_Flexible_Power_Lead").GetComponent<Renderer>().enabled=false;

            var yaw=new GameObject("Fan_Vertical_Oscillation_Pivot").transform;yaw.SetParent(root,false);yaw.localPosition=new Vector3(0,.070f,-.060f);
            foreach(var item in root.Cast<Transform>().Where(t=>t==head||t.name=="Fan_Continuous_Folded_Yoke"||t.name=="Fan_Head_Pivot_Axle").ToArray())item.SetParent(yaw,true);
            Part("Fan_Yaw_Bearing_Housing",root,new Vector3(0,.070f,-.060f),new Vector3(.024f,.005f,.024f),stemFinish,PrimitiveType.Cylinder);
            head.gameObject.layer=30;var controller=root.gameObject.AddComponent<CabCirculationFan>();controller.Configure(rotor,knob.transform,side>0);
            controller.ConfigureMotion(yaw,swing.transform,root.Find("Fan_Flexible_Power_Lead"),head.Find("Fan_Cable_Motor_Gland"),root.GetComponentsInChildren<Transform>().First(t=>t.name=="Fan_Cable_P_Clip"));

        }
        Debug.Log("NF5K_CIRCULATORS_BUILT: two 169mm rounded three-blade fans, mesh wire guards, ceiling flanges/yokes, retained cables and desk-mounted 0/1/2 controls");
    }
}
