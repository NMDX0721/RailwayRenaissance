using System;

namespace RailwayRenaissance.Cab
{
    public sealed class CabWiperMotion
    {
        public int Mode { get; private set; }
        public double Phase { get; private set; }
        public bool Parked => Phase == 0;
        public bool Parking => Mode == 0 && !Parked;
        public bool Moving { get; private set; }
        public double Sweep => .5-.5*Math.Cos(Phase*Math.PI*2);
        public bool SelectMode(int value)
        {
            if(value<0||value>2||value==Mode)return false;
            Mode=value;return true;
        }
        public void Advance(float dt,bool powered)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
            Moving=powered&&(Mode>0||!Parked);
            if(!Moving)return;
            double next=Phase+Math.Min(dt,.25f)*(Mode==2?1:.6);
            Phase=Mode==0&&next>=1?0:next-Math.Floor(next);
            Moving=powered&&(Mode>0||!Parked);
        }
        public void Reset(){Mode=0;Phase=0;Moving=false;}
    }
}
