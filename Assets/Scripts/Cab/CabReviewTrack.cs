using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    // Authored horizontal inspection alignment; positive distance is the cab's forward direction (-Z).
    [Serializable]
    public sealed class CabReviewTrack
    {
        public const float RailDatum=-1.3612222f;
        public const double Start=-60, End=500, Radius=180;
        const double Spacing=.25;
        Vector3[] points;
        public struct Sample
        {
            public Vector3 Position, Forward, Right;
            public double Curvature;
        }
        public static double Curvature(double s)
        {
            double enter=Smooth((s-35)/25),leave=1-Smooth((s-145)/25);
            return enter*leave/Radius;
        }
        static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
        void Build()
        {
            if(points!=null)return;
            points=new Vector3[(int)((End-Start)/Spacing)+1];double x=0,z=-Start,yaw=0;
            for(int i=0;i<points.Length;i++)
            {
                points[i]=new Vector3((float)x,0,(float)z);
                double k=Curvature(Start+(i+.5)*Spacing),angle=yaw+k*Spacing*.5;
                x+=Math.Sin(angle)*Spacing;z-=Math.Cos(angle)*Spacing;yaw+=k*Spacing;
            }
        }
        public Sample At(double s,int seed=0)
        {
            if(double.IsNaN(s)||double.IsInfinity(s))throw new ArgumentException("Non-finite track distance");
            Build();double bounded=Math.Max(Start,Math.Min(End,s)),index=(bounded-Start)/Spacing;
            int i=Math.Min(points.Length-2,(int)index);float t=(float)(index-i);
            Vector3 forward=(points[i+1]-points[i]).normalized;
            Vector3 p=Vector3.Lerp(points[i],points[i+1],t)+forward*(float)(s-bounded);
            double phase=(uint)seed*.000013;
            p.y=RailDatum+(float)(.0012*Math.Sin(s*.85+phase)+.0007*Math.Sin(s*1.73+phase*.7));
            p.z+=8.6f;
            return new Sample{Position=p,Forward=forward,Right=Vector3.Cross(forward,Vector3.up).normalized,Curvature=Curvature(s)};
        }
    }
}
