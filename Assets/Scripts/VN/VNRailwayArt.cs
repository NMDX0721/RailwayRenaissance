using System;
using UnityEngine;
using UnityEngine.UIElements;

public enum VNArtIcon { Bookmark, Replay, Speed, Train, Signal }
public enum VNSignalAspect { Proceed, Reduced, Caution, Restricted, Stop }

public sealed class VNInkIcon : VisualElement
{
    public VNSignalAspect Aspect;
    private readonly VNArtIcon kind;
    public VNInkIcon(VNArtIcon value)
    {
        kind = value; pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
    }
    private void Draw(MeshGenerationContext context)
    {
        var p = context.painter2D;
        float w = contentRect.width, h = contentRect.height;
        if (w <= 0 || h <= 0) return;
        Vector2 V(float x, float y) => new Vector2(x * w, y * h);
        p.strokeColor = new Color(.96f, .84f, .61f); p.fillColor = p.strokeColor; p.lineWidth = 1.7f;
        void Path(params Vector2[] points) { p.BeginPath(); p.MoveTo(points[0]); for (int i=1;i<points.Length;i++) p.LineTo(points[i]); p.Stroke(); }
        if (kind == VNArtIcon.Bookmark)
            Path(V(.28f,.14f),V(.72f,.14f),V(.72f,.88f),V(.5f,.68f),V(.28f,.88f),V(.28f,.14f));
        else if (kind == VNArtIcon.Replay)
        {
            Path(V(.13f,.38f),V(.31f,.38f),V(.53f,.19f),V(.53f,.81f),V(.31f,.62f),V(.13f,.62f),V(.13f,.38f));
            p.BeginPath();p.Arc(V(.52f,.5f),w*.22f,-48,48);p.Stroke();
            p.BeginPath();p.Arc(V(.52f,.5f),w*.34f,-46,46);p.Stroke();
        }
        else if (kind == VNArtIcon.Speed)
        {
            // Two forward chevrons with a shared rail underline, distinct from chapter skip.
            Path(V(.14f,.24f),V(.4f,.47f),V(.14f,.7f)); Path(V(.5f,.24f),V(.76f,.47f),V(.5f,.7f));
            Path(V(.12f,.84f),V(.85f,.84f));
        }
        else if (kind == VNArtIcon.Train)
        {
            p.fillColor = new Color(.19f,.28f,.3f);
            p.BeginPath();p.MoveTo(V(.06f,.22f));p.LineTo(V(.72f,.22f));p.LineTo(V(.93f,.47f));p.LineTo(V(.93f,.7f));p.LineTo(V(.06f,.7f));p.ClosePath();p.Fill();p.Stroke();
            p.fillColor = new Color(.99f,.86f,.54f);
            for(int i=0;i<3;i++){float x=.17f+i*.17f;p.BeginPath();p.MoveTo(V(x,.31f));p.LineTo(V(x+.11f,.31f));p.LineTo(V(x+.11f,.47f));p.LineTo(V(x,.47f));p.ClosePath();p.Fill();}
            Path(V(.06f,.58f),V(.9f,.58f));
            p.fillColor=new Color(.12f,.15f,.17f);
            foreach(float x in new[]{.23f,.71f}){p.BeginPath();p.Arc(V(x,.76f),h*.09f,0,360);p.Fill();p.Stroke();}
        }
        else
        {
            // Five-aspect Japanese colour-light head, Y/Y/R/Y/G from top to bottom.
            p.fillColor = new Color(.035f,.04f,.037f,.97f);p.strokeColor=new Color(.3f,.31f,.27f);p.lineWidth=1;
            p.BeginPath();p.MoveTo(V(.17f,.12f));p.Arc(V(.5f,.12f),w*.33f,180,360);p.LineTo(V(.83f,.87f));p.Arc(V(.5f,.87f),w*.33f,0,180);p.ClosePath();p.Fill();p.Stroke();
            for(int i=0;i<5;i++)
            {
                bool lit = Aspect == VNSignalAspect.Proceed ? i==4 : Aspect == VNSignalAspect.Reduced ? i==1||i==4 : Aspect == VNSignalAspect.Caution ? i==3 : Aspect == VNSignalAspect.Restricted ? i==0||i==3 : i==2;
                Color color=i==4?new Color(.36f,.92f,.64f):i==2?new Color(1f,.28f,.23f):new Color(1f,.78f,.24f);
                var centre=V(.5f,.13f+i*.17f);
                p.fillColor=new Color(.08f,.09f,.078f);p.strokeColor=new Color(.24f,.25f,.22f);p.lineWidth=1;
                p.BeginPath();p.Arc(centre,w*.245f,0,360);p.Fill();p.Stroke();
                p.fillColor=lit?color:new Color(.13f,.145f,.125f);
                p.BeginPath();p.Arc(centre,w*.177f,0,360);p.Fill();
                // Projecting dark visor and its grey lip, instead of a flat traffic-light disc.
                p.fillColor=new Color(.018f,.024f,.021f);p.strokeColor=new Color(.35f,.36f,.3f);
                p.BeginPath();p.Arc(centre,w*.26f,185,355);p.LineTo(centre+new Vector2(w*.23f,-h*.023f));p.Arc(centre+new Vector2(0,-h*.023f),w*.23f,355,185,ArcDirection.CounterClockwise);p.ClosePath();p.Fill();p.Stroke();
                if(lit){p.fillColor=new Color(1,.97f,.76f,.36f);p.BeginPath();p.Arc(centre+new Vector2(-w*.04f,-w*.04f),w*.06f,0,360);p.Fill();}
            }
            p.strokeColor=new Color(.42f,.43f,.36f);p.lineWidth=2;Path(V(.5f,.97f),V(.5f,1f));
        }
    }
}

public sealed class VNRouteProfile
{
    public string Script, Line, Landscape;
    public Color Accent;
    public float[] Heights;
    public int Motif;
    public VNSignalAspect[] Beats;
    public VNRouteProfile(string script,string line,string landscape,string color,int motif,float[] heights,params VNSignalAspect[] beats)
    {
        Script=script;Line=line;Landscape=landscape;ColorUtility.TryParseHtmlString(color,out Accent);Motif=motif;Heights=heights;Beats=beats;
    }
    public static readonly VNRouteProfile[] All =
    {
        new("prologue_01_news","塞默林铁路","旧高架桥 · 群山与停运的站场","#B99A78",0,new[]{.72f,.7f,.56f,.6f,.48f,.58f,.73f},VNSignalAspect.Restricted,VNSignalAspect.Stop,VNSignalAspect.Caution),
        new("prologue_02_day0","东海道本线","城市屋脊 · 江岸 · 出发灯火","#D8B879",1,new[]{.7f,.7f,.66f,.62f,.6f,.64f,.7f},VNSignalAspect.Caution,VNSignalAspect.Reduced,VNSignalAspect.Proceed),
        new("prologue_03_journey","西伯利亚铁路","长途地平线 · 林海与边境","#98AFB4",2,new[]{.72f,.62f,.7f,.48f,.67f,.56f,.7f},VNSignalAspect.Reduced,VNSignalAspect.Restricted,VNSignalAspect.Stop,VNSignalAspect.Caution,VNSignalAspect.Reduced),
        new("prologue_04_arrival","大吉岭喜马拉雅铁路","茶坡 · 雾谷 · 故乡站舍","#A8B896",3,new[]{.76f,.62f,.48f,.68f,.52f,.62f,.72f},VNSignalAspect.Reduced,VNSignalAspect.Caution,VNSignalAspect.Restricted),
        new("prologue_05_inspection","阿里山林业铁路","林间步道 · 旧桥 · 夜巡马灯","#8EA58D",2,new[]{.75f,.58f,.67f,.45f,.6f,.48f,.75f},VNSignalAspect.Caution,VNSignalAspect.Restricted,VNSignalAspect.Stop,VNSignalAspect.Caution),
        new("prologue_06_team","大井川铁道","村落与站台 · 重新聚起的人","#C7AA7C",3,new[]{.72f,.7f,.58f,.59f,.66f,.65f,.72f},VNSignalAspect.Caution,VNSignalAspect.Reduced,VNSignalAspect.Proceed),
        new("prologue_07_first_repair","尼尔吉里山地铁路","工场 · 齿轨坡段 · 修复的桥梁","#C09B79",4,new[]{.76f,.7f,.61f,.48f,.5f,.62f,.73f},VNSignalAspect.Restricted,VNSignalAspect.Caution,VNSignalAspect.Reduced),
        new("prologue_08_first_run","伯尔尼纳铁路","高桥 · 湖岸 · 第一趟爬坡","#9FBDB6",0,new[]{.76f,.65f,.48f,.58f,.46f,.6f,.72f},VNSignalAspect.Caution,VNSignalAspect.Reduced,VNSignalAspect.Proceed),
        new("prologue_09_funding","莱茵河谷铁路","河岸城镇 · 连接山外的货流","#C5B181",1,new[]{.7f,.65f,.7f,.62f,.65f,.59f,.72f},VNSignalAspect.Caution,VNSignalAspect.Restricted,VNSignalAspect.Reduced),
        new("prologue_10_transition","只见线","山村 · 水桥 · 延向远方的铁路","#AABD9E",2,new[]{.72f,.67f,.57f,.62f,.7f,.58f,.72f},VNSignalAspect.Reduced,VNSignalAspect.Proceed),
        new("post_001_first_sand","戈尔纳格拉特铁路","荒坡与初临的挑战","#B6A584",0,new[]{.76f,.72f,.62f,.5f,.6f,.64f,.74f},VNSignalAspect.Caution,VNSignalAspect.Restricted),
        new("post_002_zhao_visit","箱根登山铁道","层层坡段 · 来访与审视","#B29D8E",2,new[]{.75f,.54f,.7f,.5f,.66f,.52f,.74f},VNSignalAspect.Caution,VNSignalAspect.Restricted,VNSignalAspect.Reduced),
        new("post_003_qingxi_freight","弗洛姆铁路","谷地货站 · 瀑布与货流","#9DB5B0",4,new[]{.76f,.55f,.45f,.59f,.64f,.68f,.74f},VNSignalAspect.Caution,VNSignalAspect.Reduced,VNSignalAspect.Proceed),
        new("post_004_chen_mayor","莱蒂亚阿尔布拉线","石桥与聚落 · 外界的目光","#C1AD8C",0,new[]{.74f,.64f,.5f,.62f,.54f,.65f,.74f},VNSignalAspect.Reduced,VNSignalAspect.Caution),
        new("post_005_irondragon","西高地铁路","荒原与铁桥 · 竞争者的影子","#9EAAA9",0,new[]{.74f,.6f,.65f,.51f,.56f,.67f,.74f},VNSignalAspect.Reduced,VNSignalAspect.Restricted),
        new("post_006_zhou_meeting","哥达山地铁路","隧道与回环 · 对手的交锋","#B6A08B",4,new[]{.74f,.5f,.61f,.46f,.66f,.57f,.74f},VNSignalAspect.Caution,VNSignalAspect.Restricted,VNSignalAspect.Caution),
        new("post_007_expose","卡尔卡—西姆拉铁路","山口与隧道 · 逼近的风雨","#B7A292",4,new[]{.74f,.58f,.48f,.65f,.45f,.61f,.74f},VNSignalAspect.Caution,VNSignalAspect.Restricted,VNSignalAspect.Stop),
        new("post_008_sandwhale_leak","卑尔根铁路","高原冷色 · 山外的阴云","#96ABB9",0,new[]{.74f,.62f,.52f,.55f,.5f,.65f,.74f},VNSignalAspect.Restricted,VNSignalAspect.Stop,VNSignalAspect.Caution),
        new("post_009_ending","因兰兹铁路","森林与长日 · 尚未走完的路","#B1B995",2,new[]{.73f,.64f,.69f,.56f,.65f,.62f,.73f},VNSignalAspect.Caution,VNSignalAspect.Reduced,VNSignalAspect.Proceed)
    };
    private static readonly System.Collections.Generic.Dictionary<string,VNRouteProfile> Index = BuildIndex();
    private static System.Collections.Generic.Dictionary<string,VNRouteProfile> BuildIndex()
    {
        var result=new System.Collections.Generic.Dictionary<string,VNRouteProfile>();foreach(var profile in All)result.Add(profile.Script,profile);return result;
    }
    public static VNRouteProfile Find(string script) => script!=null&&Index.TryGetValue(script,out var profile)?profile:All[0];
}

public sealed class VNCapsuleEnd : VisualElement
{
    internal static readonly Color FrostTint = new Color(.55f, .46f, .35f, .76f);
    internal static readonly Color CapsuleTint = new Color(.10f, .067f, .047f, .96f);

    internal static void DrawGrain(MeshGenerationContext context)
    {
        Rect rect = context.visualElement.contentRect;
        int columns = Mathf.FloorToInt(rect.width / 4);
        int rows = Mathf.FloorToInt(rect.height / 4);
        if (columns == 0 || rows == 0) return;

        var painter = context.painter2D;
        painter.fillColor = new Color(1f, .91f, .75f, .15f);
        painter.BeginPath();
        var random = new System.Random(253);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float x = (column + .15f + .7f * (float)random.NextDouble()) * rect.width / columns;
                float y = (row + .15f + .7f * (float)random.NextDouble()) * rect.height / rows;
                painter.MoveTo(new Vector2(x, y));
                painter.LineTo(new Vector2(x + .65f, y));
                painter.LineTo(new Vector2(x + .65f, y + .65f));
                painter.LineTo(new Vector2(x, y + .65f));
                painter.ClosePath();
            }
        }
        painter.Fill();
    }

    private readonly bool right;
    public VNCapsuleEnd(bool isRight)
    {
        right=isRight;name=isRight?"progress-cap-right":"progress-cap-left";pickingMode=PickingMode.Ignore;
        style.position=Position.Absolute;style.top=style.bottom=0;style.width=9;
        if(isRight)style.right=0;else style.left=0;
        generateVisualContent+=context=>
        {
            float h=contentRect.height,r=h*.5f;if(r<=0)return;
            float x=right?0:contentRect.width;var p=context.painter2D;
            p.fillColor=CapsuleTint;
            p.BeginPath();p.MoveTo(new Vector2(x,0));
            p.Arc(new Vector2(x,r),r,270,90,right?ArcDirection.Clockwise:ArcDirection.CounterClockwise);
            p.ClosePath();p.Fill();
            p.strokeColor=new Color(200f/255f,150f/255f,80f/255f,.42f);p.lineWidth=1;
            p.BeginPath();p.Arc(new Vector2(x,r),Mathf.Max(0,r-.5f),270,90,right?ArcDirection.Clockwise:ArcDirection.CounterClockwise);p.Stroke();
        };
    }
}

public sealed class VNScenicRoute : VisualElement
{
    public VNRouteProfile Profile { get; private set; } = VNRouteProfile.All[0];
    public float Progress { get; private set; }
    private readonly VNInkIcon train;
    public VNScenicRoute()
    {
        name="progress-track";style.flexGrow=1;style.height=Length.Percent(100);style.position=Position.Relative;
        train=new VNInkIcon(VNArtIcon.Train){name="progress-thumb"};train.style.position=Position.Absolute;train.style.width=24;train.style.height=14;
        Add(train);generateVisualContent+=Draw;RegisterCallback<GeometryChangedEvent>(_=>PlaceTrain());
    }
    public void SetRoute(string script)
    {
        var profile=VNRouteProfile.Find(script);if(Profile==profile)return;Profile=profile;
        tooltip="本章风景意象："+Profile.Line+" · "+Profile.Landscape;MarkDirtyRepaint();PlaceTrain();
    }
    public void SetProgress(float value){Progress=Mathf.Clamp01(value);PlaceTrain();MarkDirtyRepaint();}
    public float Normalize(float x)=>Mathf.Clamp01((x-24)/Mathf.Max(1,contentRect.width-48));
    private float HeightAt(float x)
    {
        float t=Mathf.Clamp01(x)*(Profile.Heights.Length-1);int i=Mathf.Min((int)t,Profile.Heights.Length-2);
        return Mathf.Lerp(Profile.Heights[i],Profile.Heights[i+1],t-i)*contentRect.height;
    }
    private void PlaceTrain()
    {
        train.style.left=24+Progress*Mathf.Max(1,contentRect.width-48)-12;train.style.top=HeightAt(Progress)-10;
    }
    private void Draw(MeshGenerationContext context)
    {
        float w=contentRect.width,h=contentRect.height;if(w<48||h<10)return;
        var p=context.painter2D;var accent=Profile.Accent;
        void Line(Vector2 a,Vector2 b,Color color,float size=1){p.strokeColor=color;p.lineWidth=size;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();}
        void Poly(Color c,params Vector2[] points){p.fillColor=c;p.BeginPath();p.MoveTo(points[0]);for(int i=1;i<points.Length;i++)p.LineTo(points[i]);p.ClosePath();p.Fill();}
        var far=new Color(accent.r,accent.g,accent.b,.18f);var near=new Color(accent.r,accent.g,accent.b,.34f);
        for(int i=0;i<15;i++)
        {
            float x=(i+.3f)*w/15;float y=h*.67f;float size=3+(i*13+Profile.Motif*7)%7;
            if(Profile.Motif==1)
            {
                Poly(far,new(x,y),new(x,y-size),new(x+15,y-size),new(x+15,y));
                Line(new(x-4,y-size),new(x+7,y-size-5),near);Line(new(x+7,y-size-5),new(x+19,y-size),near);
            }
            else if(Profile.Motif==2)
            {
                Poly(far,new(x-11,y),new(x,y-size),new(x+11,y));Line(new(x,y-4),new(x,y+4),near);
            }
            else if(Profile.Motif==3)
            {
                p.strokeColor=far;p.lineWidth=2;p.BeginPath();p.MoveTo(new(x-20,y));p.QuadraticCurveTo(new(x,y-size),new(x+26,y));p.Stroke();
                Line(new(x-10,y+4),new(x+22,y+4),far);
            }
            else Poly(far,new(x-26,y),new(x,y-size),new(x+34,y));
        }
        // A little bridge, tunnel or platform makes the silhouette specific beyond its palette.
        float bx=w*(Profile.Motif==4?.63f:.35f), by=HeightAt(Profile.Motif==4?.63f:.35f);
        if(Profile.Motif==4)
        {
            p.strokeColor=near;p.lineWidth=4;p.BeginPath();p.Arc(new(bx,by),14,180,360);p.Stroke();
        }
        else if(Profile.Motif==0)
        {
            for(int i=0;i<5;i++)Line(new(bx+i*15,by+2),new(bx+i*15,h-3),near,2);
            Line(new(bx-5,by+4),new(bx+67,by+4),near,2);
        }
        else
        {
            Line(new(bx,by-12),new(bx+46,by-12),near,2);Line(new(bx+4,by-12),new(bx+4,by+5),near);
            Line(new(bx+40,by-12),new(bx+40,by+5),near);
        }
        // Sleepers, untravelled rail, then the read portion. The same geometry drives the train.
        for(float x=24;x<w-24;x+=14){float y=HeightAt(Normalize(x));Line(new(x-2,y-3),new(x+2,y+5),near);}
        for(int pass=0;pass<3;pass++)
        {
            float limit=pass==2?Progress:1;p.strokeColor=pass==2?accent:new Color(.58f,.55f,.47f,.36f);p.lineWidth=pass==2?2:1;
            p.BeginPath();p.MoveTo(new(24,HeightAt(0)+(pass==1?3:0)));
            for(int i=1;i<=120;i++){float t=Mathf.Min(i/120f,limit);p.LineTo(new(24+t*(w-48),HeightAt(t)+(pass==1?3:0)));if(t>=limit)break;}p.Stroke();
        }
        for(int i=0;i<5;i++)
        {
            float t=i/4f;p.fillColor=t<=Progress?accent:new Color(.34f,.35f,.32f);p.strokeColor=accent;p.lineWidth=1;
            p.BeginPath();p.Arc(new(24+t*(w-48),HeightAt(t)+1),3,0,360);p.Fill();p.Stroke();
        }
        float ex=w-24,ey=HeightAt(1);Line(new(ex,ey-23),new(ex,ey-3),accent);
        Poly(accent,new(ex,ey-23),new(ex+9,ey-19),new(ex,ey-15));
    }
}
