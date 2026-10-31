using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabCommunicationReview
{
    public static void Validate(CabEngineState state)
    {
        void Assert(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        var equipment=state.GetComponentsInChildren<CabWorkstationEquipment>();
        Assert(equipment.Length==2,"Expected radio and crew document clipboard");
        var radio=equipment.First(t=>t.Kind==CabWorkstationEquipment.EquipmentKind.Radio);
        var record=equipment.First(t=>t.Kind==CabWorkstationEquipment.EquipmentKind.Documents);
        var lighting=state.GetComponentInParent<CabInteriorLighting>();
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        bool normal=lighting.MainPower,backup=lighting.BatteryPower;
        var ui=state.GetComponent<CabContextInteraction>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var focus=typeof(CabContextInteraction).GetField("focused",flags);var prior=focus.GetValue(ui);
        var choices=typeof(CabContextInteraction).GetField("choices",flags);
        var build=typeof(CabContextInteraction).GetMethod("BuildChoices",flags);
        var execute=typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags);
        var selected=typeof(CabContextInteraction).GetField("selected",flags);
        var settings=typeof(CabContextInteraction).GetField("settings",flags);bool priorSettings=(bool)settings.GetValue(ui);
        try
        {
            settings.SetValue(ui,false);
            camera.transform.position=radio.transform.position+Vector3.forward*.45f;Physics.SyncTransforms();
            Assert(Physics.Raycast(camera.transform.position,Vector3.back,out var hit,.6f,1<<30)&&hit.collider.GetComponentInParent<CabWorkstationEquipment>()==radio,"Radio face ray unreachable");
            focus.SetValue(ui,radio);build.Invoke(ui,null);
            Assert(((System.Collections.ICollection)choices.GetValue(ui)).Count==6,"Radio contextual actions missing");
            selected.SetValue(ui,0);execute.Invoke(ui,null);Assert(radio.SwitchedOn,"Radio primary action failed");
            lighting.SetSupply(false,false);Assert(!radio.Powered,"Radio remains powered without supply");
            Assert(radio.Operate(1)&&radio.Channel==2,"Mechanical channel selection without supply failed");
            lighting.SetSupply(false,true);Assert(radio.Powered&&radio.Channel==2,"Backup supply or retained setting failed");
            for(int i=0;i<7;i++)radio.Operate(1);Assert(radio.Channel==1,"Eight-channel wrap failed");
            Assert(radio.Operate(2)&&radio.Channel==8,"Reverse channel wrap failed");
            for(int i=0;i<8;i++)radio.Operate(3);Assert(radio.Volume==5&&!radio.Operate(3),"Volume maximum failed");
            for(int i=0;i<8;i++)radio.Operate(4);Assert(radio.Volume==0&&!radio.Operate(4),"Volume minimum failed");
            camera.transform.position=radio.transform.position+Vector3.forward*3;
            Assert(!radio.Operate(0)&&!ui.ShowWorkstationInfo(radio),"Remote radio operation accepted");
            camera.transform.position=record.transform.position+Vector3.up*.4f;Physics.SyncTransforms();
            Assert(Physics.Raycast(camera.transform.position,Vector3.down,out var recordHit,.6f,1<<30)&&recordHit.collider.GetComponentInParent<CabWorkstationEquipment>()==record,"Document ray unreachable");
            focus.SetValue(ui,record);build.Invoke(ui,null);selected.SetValue(ui,0);execute.Invoke(ui,null);
            Assert(ui.ModalOpen&&record.Description.Contains("CW-01"),"Document contextual reading failed");
            ui.CloseEquipmentInfo();Assert(!ui.ModalOpen,"Document modal did not close");
            radio.enabled=false;Assert(!radio.SwitchedOn&&radio.Channel==1&&radio.Volume==3,"Radio disable restoration failed");radio.enabled=true;
            Debug.Log("NF5K_COMMUNICATION_PLAY_OK: radio/clipboard real rays, contextual actions, supply/backup, retained detents, channel wrap, volume stops, distance guards, document modal and disable restoration; no radio transmission/audio backend");
        }
        finally
        {
            ui.CloseEquipmentInfo();radio.enabled=false;radio.enabled=true;lighting.SetSupply(normal,backup);
            settings.SetValue(ui,priorSettings);focus.SetValue(ui,prior);camera.transform.SetPositionAndRotation(position,rotation);
        }
    }
}
