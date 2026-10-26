using System;
using System.Reflection;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KSafetyEquipmentReview
{
    public static void Validate(CabEngineState state)
    {
        var items=Array.FindAll(state.GetComponentsInChildren<CabSafetyEquipment>(),t=>!t.IsManual);if(items.Length==0)return;
        void Assert(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        Assert(items.Length==3,"Expected two extinguishers and one hammer");
        var camera=Camera.main;var p=camera.transform.position;var q=camera.transform.rotation;
        var ui=state.GetComponent<CabContextInteraction>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var focus=typeof(CabContextInteraction).GetField("focused",flags);var original=focus.GetValue(ui);
        try
        {
            var manual=Array.Find(state.GetComponentsInChildren<CabSafetyEquipment>(),t=>t.IsManual);
            Assert(manual!=null,"Fire equipment manual missing");
            focus.SetValue(ui,manual);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==1,"Manual should offer reading, not equipment removal");
            bool priorModal=ui.ModalOpen;
            Assert(ui.ShowEquipmentInfo(manual)&&ui.ModalOpen,"Manual did not open");ui.CloseEquipmentInfo();
            Assert(ui.ModalOpen==priorModal&&!manual.Toggle(),"Manual reading changed the prior modal state or allowed unsupported pickup");
            Debug.Log("NF5K_MANUAL_INTERACTION_OK: fixed manual offers reading and opens/closes the equipment dossier");
            foreach(var equipment in items)
            {
                Vector3 near=equipment.transform.position+equipment.transform.TransformDirection(equipment.transform.localPosition.x>0?Vector3.left:Vector3.right)*.65f;
                camera.transform.position=near;
                Physics.SyncTransforms();var hitbox=equipment.GetComponent<Collider>();
                bool visible=Physics.Raycast(new Ray(camera.transform.position,(hitbox.bounds.center-camera.transform.position).normalized),out var hit,1.0f,1<<30);
                Assert(visible&&hit.collider==hitbox,"Safety dock ray target occluded: "+equipment.name+" at "+equipment.transform.position+" by "+(visible?hit.collider.name:"no hit"));
                focus.SetValue(ui,equipment);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
                var choices=(System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui);
                Assert(choices.Count==2,"Safety context menu missing inspect/take choices");
                var bands=(Transform[])typeof(CabSafetyEquipment).GetField("retainingBands",flags).GetValue(equipment);
                Quaternion closed=bands!=null&&bands.Length>0?bands[0].localRotation:Quaternion.identity;
                Assert(equipment.Toggle()&&equipment.Held,"Equipment pickup failed");
                if(bands!=null&&bands.Length>0)Assert(Quaternion.Angle(closed,bands[0].localRotation)>90,"Retaining band did not unlock");
                camera.transform.position=equipment.transform.position+Vector3.forward*4;
                Assert(!equipment.Toggle()&&equipment.Held,"Equipment remotely returned to dock");
                camera.transform.position=near;
                Assert(equipment.Toggle()&&!equipment.Held,"Equipment return failed");
                if(bands!=null&&bands.Length>0)Assert(Quaternion.Angle(closed,bands[0].localRotation)<.001f,"Retaining band failed to close");
                Assert(equipment.Toggle(),"Second pickup failed");equipment.enabled=false;
                Assert(!equipment.Held,"Disable did not restore safety equipment");equipment.enabled=true;
            }
            camera.transform.position=items[0].transform.position+Vector3.forward*.65f;Assert(items[0].Toggle(),"Pickup for exclusivity failed");
            camera.transform.position=items[1].transform.position+Vector3.forward*.65f;Assert(!items[1].Toggle(),"Two safety items held simultaneously");
            camera.transform.position=items[0].transform.position+Vector3.forward*.65f;Assert(items[0].Toggle(),"Final return failed");
            Debug.Log("NF5K_SAFETY_EQUIPMENT_PLAY_OK: three dock rays, context choices, pickup/near-dock return, remote-return guard, single-item limit and disable restoration");
        }
        finally{foreach(var item in items){item.enabled=false;item.enabled=true;}focus.SetValue(ui,original);camera.transform.SetPositionAndRotation(p,q);}
    }
}
