using System;
using System.Linq;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabWorkstationLayout
{
    public static void Build(Transform root,Transform body,Material enamel,Material steel,Material black,Material paper)
    {
        var seat=body.Find("Assistant_Seat_Pivot");
        if(seat==null)throw new InvalidOperationException("Assistant seat missing");
        var seatPosition=seat.localPosition;seatPosition.x=-.65f;seat.localPosition=seatPosition;
        // Put the hinge axis ahead of the partition face, preserving the closed leaf pose.
        var passage=body.GetComponentsInChildren<Transform>().First(t=>t.name=="Passenger_Passage_Door_Pivot");
        var children=passage.Cast<Transform>().ToArray();
        var positions=children.Select(t=>t.position).ToArray();
        var rotations=children.Select(t=>t.rotation).ToArray();
        passage.position=body.TransformPoint(new Vector3(NF5KPassageDoorDetail.AxisX,0,NF5KPassageDoorDetail.AxisZ));
        for(int i=0;i<children.Length;i++)children[i].SetPositionAndRotation(positions[i],rotations[i]);
        var renderers=body.GetComponentsInChildren<Renderer>();
        var paint=renderers.First(r=>r.name=="Continuous_Folded_Console_Shell").sharedMaterial;
        var top=renderers.First(r=>r.name=="Contoured_Control_Deck").sharedMaterial;
        GameObject Part(string name,PrimitiveType type,Vector3 p,Vector3 size,Material material,bool solid=false)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        void Box(string name,Vector3 p,Vector3 size,Material material,bool solid=false)=>Part(name,PrimitiveType.Cube,p,size,material,solid);
        void Rod(string name,Vector3 a,Vector3 b,float radius,Material material)
        {
            var g=Part(name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),material);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        }
        Box("Assistant_Work_Shelf",new Vector3(-.80f,.8165f,.025f),new Vector3(.95f,.045f,.58f),top,true);
        Box("Aux_Folded_Front_Apron",new Vector3(-.80f,.694f,-.239f),new Vector3(.95f,.20f,.014f),paint,true);
        Box("Aux_Outer_Support_Return",new Vector3(-1.255f,.408f,.025f),new Vector3(.04f,.772f,.54f),paint,true);
        foreach(float z in new[]{-.15f,.22f})
        {
            Box("Aux_Floor_Anchor_Flange",new Vector3(-1.255f,.011f,z),new Vector3(.12f,.022f,.11f),steel,true);
            foreach(float x in new[]{-1.295f,-1.215f})Part("Aux_Floor_Anchor_Bolt",PrimitiveType.Cylinder,new Vector3(x,.025f,z),new Vector3(.012f,.003f,.012f),steel);
        }
        foreach(float x in new[]{-1.15f,-.44f})
        {
            Rod("Aux_Diagonal_Desk_Bracket",new Vector3(x,.790f,.22f),new Vector3(x,.60f,-.232f),.011f,steel);
            Box("Aux_Bracket_Attachment_Plate",new Vector3(x,.63f,-.229f),new Vector3(.065f,.10f,.012f),paint);
            foreach(float y in new[]{.605f,.655f})Box("Aux_Bracket_Fastener",new Vector3(x,y,-.221f),new Vector3(.010f,.010f,.004f),steel);
        }
        Box("Aux_Apron_Service_Inset",new Vector3(-.80f,.694f,-.248f),new Vector3(.67f,.14f,.003f),black);
        Box("Aux_Apron_Service_Cover",new Vector3(-.80f,.694f,-.250f),new Vector3(.65f,.125f,.003f),paint);
        // A removable filler bridges only the front work surface; the rear centre passage remains open.
        Box("Aux_Main_Deck_Bridge",new Vector3(-.275f,.827f,-.04f),new Vector3(.104f,.024f,.24f),top,true);
        Box("Aux_Bridge_Folded_Return",new Vector3(-.275f,.770f,-.153f),new Vector3(.104f,.114f,.014f),paint,true);
        foreach(float x in new[]{-.320f,-.230f})
        {
            Box("Aux_Bridge_Expansion_Seam",new Vector3(x,.8393f,-.04f),new Vector3(.001f,.0006f,.23f),black);
            foreach(float z in new[]{-.13f,.05f})Part("Aux_Bridge_Captive_Screw",PrimitiveType.Cylinder,new Vector3(x,.840f,z),new Vector3(.007f,.001f,.007f),steel);
        }
        Box("Aux_Front_Retaining_Lip",new Vector3(-.80f,.861f,-.260f),new Vector3(.95f,.044f,.010f),paint);
        Box("Aux_Outer_Desk_Edge",new Vector3(-1.270f,.846f,.025f),new Vector3(.010f,.014f,.56f),paint);

        Box("Radio_Receiver_Visual_Only",new Vector3(-.99f,.904f,-.17f),new Vector3(.29f,.13f,.16f),black);
        Box("Radio_Faceplate",new Vector3(-.99f,.904f,-.082f),new Vector3(.26f,.105f,.012f),enamel);
        foreach(float x in new[]{-1.105f,-.875f})foreach(float y in new[]{.864f,.944f})
            Box("Radio_Face_Screw",new Vector3(x,y,-.074f),new Vector3(.008f,.008f,.004f),steel);
        for(int i=0;i<7;i++)Box("Radio_Speaker_Slot",new Vector3(-1.06f+i*.012f,.904f,-.074f),new Vector3(.004f,.065f,.003f),black);
        for(int i=0;i<2;i++)
        {
            var knob=Part("Radio_Control_Knob",PrimitiveType.Cylinder,new Vector3(-.94f+i*.055f,.899f,-.067f),new Vector3(.025f,.010f,.025f),black);knob.transform.localRotation=Quaternion.Euler(90,0,0);
            Box("Radio_Knob_Index",new Vector3(-.94f+i*.055f,.907f,-.055f),new Vector3(.003f,.006f,.003f),paper);
        }
        foreach(float x in new[]{-1.09f,-.89f})Box("Radio_Desk_Securing_Foot",new Vector3(x,.845f,-.16f),new Vector3(.025f,.012f,.13f),steel);
        Box("Handset_Cradle",new Vector3(-1.18f,.8615f,-.13f),new Vector3(.07f,.045f,.19f),steel);
        Rod("Handset_Grip",new Vector3(-1.18f,.9115f,-.20f),new Vector3(-1.18f,.9115f,-.06f),.018f,black);
        foreach(float z in new[]{-.20f,-.06f})Part("Handset_Earpiece",PrimitiveType.Sphere,new Vector3(-1.18f,.8965f,z),new Vector3(.06f,.045f,.05f),black);
        Vector3 a=new Vector3(-1.18f,.895f,-.045f),b=new Vector3(-1.24f,.846f,.025f),c=new Vector3(-1.24f,.846f,-.13f),e=new Vector3(-1.14f,.88f,-.15f);
        Vector3 Cord(float t){float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*e;}
        for(int i=0;i<24;i++)
        {
            Rod("Handset_Supported_Cord",Cord(i/24f),Cord((i+1)/24f),.003f,black);
            Part("Handset_Cord_Continuous_Joint",PrimitiveType.Sphere,Cord(i/24f),Vector3.one*.006f,black);
        }
        Box("Timetable_Clipboard",new Vector3(-.58f,.843f,.12f),new Vector3(.20f,.008f,.25f),black);
        Box("Timetable_Blank_Insert",new Vector3(-.58f,.8485f,.12f),new Vector3(.18f,.003f,.23f),paper);
        Box("Timetable_Clip",new Vector3(-.58f,.853f,.011f),new Vector3(.065f,.008f,.018f),steel);
        for(int i=0;i<9;i++)Box("Timetable_Print_Rule",new Vector3(-.58f,.8504f,.042f+i*.020f),new Vector3(.15f,.0006f,.0008f),enamel);
        Box("Assistant_Map_Pocket_Back",new Vector3(-1.32f,.57f,.44f),new Vector3(.035f,.27f,.24f),enamel);
        Box("Assistant_Map_Pocket_Front",new Vector3(-1.24f,.53f,.44f),new Vector3(.025f,.19f,.24f),enamel);
        foreach(float z in new[]{.32f,.44f,.56f})Box("Map_Pocket_Divider",new Vector3(-1.28f,.53f,z),new Vector3(.10f,.19f,.015f),steel);
        Box("Map_Pocket_Base",new Vector3(-1.28f,.435f,.44f),new Vector3(.10f,.015f,.24f),enamel);

        NF5KCabCommunicationDetail.Build(root,enamel,steel,black,paper);
        NF5KPassageDoorDetail.Build(root,body,enamel,steel,black);
        var lamp=body.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Assistant_Reading_Light");
        if(lamp!=null)
        {
            var aim=body.TransformPoint(new Vector3(-.58f,.85f,.12f));
            lamp.rotation=Quaternion.FromToRotation(Vector3.down,(aim-lamp.position).normalized);
            var beam=lamp.GetComponentInChildren<Light>();if(beam!=null)beam.transform.rotation=Quaternion.LookRotation((aim-beam.transform.position).normalized);
        }
        var lighting=body.GetComponent<CabInteriorLighting>();
        if(lighting!=null)
        {
            for(int i=0;i<lighting.Fixtures.Length;i++)
                if(lighting.Fixtures[i].circuit==InteriorLightCircuit.AssistantTask)lighting.Fixtures[i].intensity=.5f;
            lighting.SetAssistantTask(lighting.AssistantTaskOn);
        }
        Debug.Log("NF5K_AUX_WORKSTATION_BUILT: 0.95m x 0.58m worktop at 0.839m, assistant seat x=-0.65m, fixed main console, front bridge and independent communication/document zones");
    }
}
