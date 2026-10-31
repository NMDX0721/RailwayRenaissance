using System;
using System.IO;
using System.Security.Cryptography;

namespace RailwayRenaissance.Cab.Research
{
    public sealed class AtoRouteTask
    {
        public double Origin {get;}
        public double Distance {get;}
        public double TargetSeconds {get;}
        public double TimeTolerance {get;}
        public double Target=>Origin+Distance;
        readonly double[] starts,limits;
        public AtoRouteTask(double origin,double distance,double seconds,double tolerance,double[] starts,double[] limits)
        {
            if(!Finite(origin)||!Finite(distance)||distance<=0||!Finite(seconds)||seconds<=0||!Finite(tolerance)||tolerance<0||
                starts==null||limits==null||starts.Length==0||starts.Length!=limits.Length||starts.Length>32||starts[0]!=0)
                throw new ArgumentException("Invalid ATO route task");
            for(int i=0;i<starts.Length;i++)if(!Finite(starts[i])||starts[i]<0||starts[i]>=distance||i>0&&starts[i]<=starts[i-1]||!Finite(limits[i])||limits[i]<=0||limits[i]>30)
                throw new ArgumentException("Invalid speed segment");
            Origin=origin;Distance=distance;TargetSeconds=seconds;TimeTolerance=tolerance;this.starts=(double[])starts.Clone();this.limits=(double[])limits.Clone();
        }
        static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        public static AtoRouteTask Standard(double origin)=>new AtoRouteTask(origin,900,120,5,new[]{0.0,250,650},new[]{12.0,18,10});
        public double LimitAt(double position)
        {
            double s=position-Origin;int i=0;while(i+1<starts.Length&&s>=starts[i+1])i++;return limits[i];
        }
        public double PreviewSpeed(double position,double deceleration=.45)
        {
            double value=LimitAt(position),s=position-Origin;
            for(int i=0;i<starts.Length;i++)if(starts[i]>s)value=Math.Min(value,Math.Sqrt(limits[i]*limits[i]+2*deceleration*Math.Max(0,starts[i]-s)));
            return Math.Min(value,Math.Sqrt(2*deceleration*Math.Max(0,Target-position)));
        }
        public string Encode()
        {
            byte[] payload;
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                w.Write(1);w.Write(Origin);w.Write(Distance);w.Write(TargetSeconds);w.Write(TimeTolerance);w.Write(starts.Length);
                for(int i=0;i<starts.Length;i++){w.Write(starts[i]);w.Write(limits[i]);}w.Flush();payload=stream.ToArray();
            }
            using(var hash=SHA256.Create())return Convert.ToBase64String(payload)+"."+Convert.ToBase64String(hash.ComputeHash(payload));
        }
        public static AtoRouteTask Decode(string text)
        {
            try
            {
                if(string.IsNullOrEmpty(text)||text.Length>4096)throw new ArgumentException("Invalid task checkpoint");
                var parts=text.Split('.');if(parts.Length!=2)throw new ArgumentException("Invalid task checksum");
                var bytes=Convert.FromBase64String(parts[0]);
                using(var hash=SHA256.Create())if(Convert.ToBase64String(hash.ComputeHash(bytes))!=parts[1])throw new ArgumentException("Task checksum mismatch");
                using(var stream=new MemoryStream(bytes))using(var r=new BinaryReader(stream))
                {
                    if(r.ReadInt32()!=1)throw new ArgumentException("Unsupported task version");
                    double origin=r.ReadDouble(),distance=r.ReadDouble(),seconds=r.ReadDouble(),tolerance=r.ReadDouble();
                    int n=r.ReadInt32();if(n<1||n>32)throw new ArgumentException("Invalid segment count");
                    var starts=new double[n];var limits=new double[n];for(int i=0;i<n;i++){starts[i]=r.ReadDouble();limits[i]=r.ReadDouble();}
                    if(stream.Position!=stream.Length)throw new ArgumentException("Trailing task bytes");
                    return new AtoRouteTask(origin,distance,seconds,tolerance,starts,limits);
                }
            }
            catch(Exception e) when(e is IOException||e is FormatException){throw new ArgumentException("Invalid task checkpoint",e);}
        }
    }
}
