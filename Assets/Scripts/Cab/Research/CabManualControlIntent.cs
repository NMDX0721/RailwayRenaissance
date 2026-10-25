using System;

namespace RailwayRenaissance.Cab.Research
{
    // Called by actual input entrypoints, never by visual state refresh.
    public static class CabManualControlIntent
    {
        public static event Action<CabEngineState> Requested;
        public static void Notify(CabEngineState owner){if(owner!=null)Requested?.Invoke(owner);}
    }
}
