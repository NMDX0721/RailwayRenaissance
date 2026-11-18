using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    // Authored horizontal inspection alignment; positive distance is the cab's forward direction (-Z).
    // Default instance: the legacy -60..500 m review curve. Straight(...) builds a straight line with a piecewise-linear
    // grade profile (the ATO test line); its rail height follows the profile exactly, with no added irregularity.
    [Serializable]
    public sealed class CabReviewTrack
    {
        public const float RailDatum=-1.3612222f;
        public const double Start=-60, End=500, Radius=180;
        const double Spacing=.25;
        Vector3[] points;
        double start=Start,end=End;
        bool curved=true;
        double[] pieceStart,pieceRun,pieceRise,pieceBase;
        public struct Sample
        {
            public Vector3 Position, Forward, Right;
            public double Curvature;
        }
        public double RangeStart=>start;
        public double RangeEnd=>end;
        public bool HasGrade=>pieceStart!=null;
        // pieces: consecutive (horizontal run, rise) segments beginning at `from`; the line is straight and spans from..to.
        public static CabReviewTrack Straight(double from,double to,double[] runs,double[] rises)
        {
            if(runs==null||rises==null||runs.Length!=rises.Length)throw new ArgumentException("Grade pieces need matching runs and rises");
            var t=new CabReviewTrack{start=from,end=to,curved=false,pieceStart=new double[runs.Length],pieceRun=(double[])runs.Clone(),pieceRise=(double[])rises.Clone(),pieceBase=new double[runs.Length]};
            double s=from,h=0;
            for(int i=0;i<runs.Length;i++){t.pieceStart[i]=s;t.pieceBase[i]=h;s+=runs[i];h+=rises[i];}
            return t;
        }
        public static double Curvature(double s)
        {
            double enter=Smooth((s-35)/25),leave=1-Smooth((s-145)/25);
            return enter*leave/Radius;
        }
        double K(double s)=>curved?Curvature(s):0;
        // Rail-top height above the datum at route distance s (0 for the legacy curve).
        public double HeightAt(double s)
        {
            if(pieceStart==null)return 0;
            double c=Math.Max(start,Math.Min(end,s));
            int i=Array.BinarySearch(pieceStart,c);if(i<0)i=Math.Max(0,~i-1);
            if(i>=pieceStart.Length)i=pieceStart.Length-1;
            double u=Math.Max(0,Math.Min(1,(c-pieceStart[i])/pieceRun[i]));
            return pieceBase[i]+pieceRise[i]*u;
        }
        // Longitudinal grade in permille, positive when the rail rises toward increasing route distance.
        public double GradePermilleAt(double s)
        {
            if(pieceStart==null)return 0;
            double c=Math.Max(start,Math.Min(end,s));
            int i=Array.BinarySearch(pieceStart,c);if(i<0)i=Math.Max(0,~i-1);
            if(i>=pieceStart.Length)i=pieceStart.Length-1;
            return pieceRise[i]/pieceRun[i]*1000;
        }
        static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
        void Build()
        {
            if(points!=null)return;
            points=new Vector3[(int)((end-start)/Spacing)+1];double x=0,z=-start,yaw=0;
            for(int i=0;i<points.Length;i++)
            {
                points[i]=new Vector3((float)x,0,(float)z);
                double k=K(start+(i+.5)*Spacing),angle=yaw+k*Spacing*.5;
                x+=Math.Sin(angle)*Spacing;z-=Math.Cos(angle)*Spacing;yaw+=k*Spacing;
            }
        }
        public Sample At(double s,int seed=0)
        {
            if(double.IsNaN(s)||double.IsInfinity(s))throw new ArgumentException("Non-finite track distance");
            Build();double bounded=Math.Max(start,Math.Min(end,s)),index=(bounded-start)/Spacing;
            int i=Math.Min(points.Length-2,(int)index);float t=(float)(index-i);
            Vector3 forward=(points[i+1]-points[i]).normalized;
            Vector3 p=Vector3.Lerp(points[i],points[i+1],t)+forward*(float)(s-bounded);
            double phase=(uint)seed*.000013;
            p.y=RailDatum+(float)HeightAt(s);
            if(pieceStart==null)p.y+=(float)(.0012*Math.Sin(s*.85+phase)+.0007*Math.Sin(s*1.73+phase*.7));
            p.z+=8.6f;
            return new Sample{Position=p,Forward=forward,Right=Vector3.Cross(forward,Vector3.up).normalized,Curvature=K(s)};
        }
    }
}
