// Compatibility entry for existing fittings generation; the rotary selector is retired.
public static class NF5KParkingSelectorDetail
{
    public static void Apply(RailwayRenaissance.Cab.CabEngineState state)=>NF5KParkingBrakePanelSetup.Apply(state);
    public static void Batch()=>NF5KParkingBrakePanelSetup.Batch();
}
