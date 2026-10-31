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
            var visors=state.GetComponentsInChildren<CabSunVisor>();
            Assert(visors.Length==2,"Expected independent driver/assistant visors");
            foreach(var visor in visors)
            {
                camera.transform.position=visor.transform.TransformPoint(new Vector3(0,-.3f,.65f));
                var core=visor.transform.Find("Visor_Moving_Leaf/Visor_Core").GetComponent<Collider>();Physics.SyncTransforms();
                Assert(Physics.Raycast(camera.transform.position,(core.bounds.center-camera.transform.position).normalized,out var visorHit,1.2f,1<<30)
                    &&visorHit.collider.GetComponentInParent<CabSunVisor>()==visor,"Stowed visor is not ray reachable");
                focus.SetValue(ui,visor);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
                Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==2,"Visor contextual choices missing");
                typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,0);
                typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
                Assert(visor.Step==1,"Visor contextual adjustment failed");
                Assert(visor.Adjust(1)&&visor.Step==2&&!visor.Adjust(1),"Visor lower stop failed");
                foreach(var other in visors)if(other!=visor)Assert(other.Step==0,"Visor changed the other seat's setting");
                for(int pose=2;pose>=0;pose--)
                {
                    foreach(var filter in visor.transform.Find("Visor_Moving_Leaf").GetComponentsInChildren<MeshFilter>())
                        foreach(var vertex in filter.sharedMesh.vertices)
                        {
                            Vector3 local=visor.transform.parent.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                            Assert(local.y>1.76f&&local.y<2.01f&&local.z>-.51f&&Mathf.Abs(local.x)<1.04f,"Visor leaves conservative roof/glass/eye-height envelope");
                        }
                    if(pose>0)Assert(visor.Adjust(-1),"Visor could not be raised");
                }
                Assert(!visor.Adjust(-1),"Visor upper stop failed");
                camera.transform.position=visor.transform.TransformPoint(new Vector3(0,0,3));
                Assert(!visor.Adjust(1)&&visor.Step==0,"Remote visor operation accepted");
                camera.transform.position=visor.transform.TransformPoint(new Vector3(0,-.3f,.65f));
                Assert(visor.Adjust(1),"Visor could not be lowered for restoration check");visor.enabled=false;
                Assert(visor.Step==0,"Visor disable restoration failed");visor.enabled=true;
            }
            Debug.Log("NF5K_CAB_VISORS_OK: real rays/context commands, independent 3-position stops, vertex envelope, distance guard and restoration; not a full visibility certification");
            var cabinets=state.GetComponentsInChildren<CabStorageCabinet>();
            Assert(cabinets.Length==1,"Expected one first-aid cabinet");
            var cabinet=cabinets[0];var door=cabinet.transform.Find("Aid_Door_Hinge");
            var cabinetLatch=door.Find("Aid_Quarter_Turn_Latch");var lockedLatch=cabinetLatch.localRotation;
            camera.transform.position=cabinet.transform.TransformPoint(new Vector3(0,0,.65f));
            Physics.SyncTransforms();
            Assert(Physics.Raycast(camera.transform.position,(cabinet.transform.position-camera.transform.position).normalized,out var cabinetHit,1f,1<<30)
                &&cabinetHit.collider.GetComponentInParent<CabStorageCabinet>()==cabinet,"Cabinet door is not reachable by ray");
            focus.SetValue(ui,cabinet);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==1,"Cabinet open/close choice missing");
            var closedDoor=door.localRotation;
            typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
            Assert(cabinet.IsOpen&&Mathf.Abs(Quaternion.Angle(closedDoor,door.localRotation)-100)<.01f,"Cabinet door did not open through contextual command");
            Assert(Mathf.Abs(Quaternion.Angle(lockedLatch,cabinetLatch.localRotation)-90)<.01f,"Cabinet catch did not release");
            foreach(var seat in state.GetComponentsInChildren<BoxCollider>())
                if(seat.name.StartsWith("Passenger_Seat_"))
                    Assert(!door.Find("Aid_Cabinet_Door").GetComponent<Renderer>().bounds.Intersects(seat.bounds),"Open cabinet intersects passenger seat");
            camera.transform.position=cabinet.transform.TransformPoint(new Vector3(0,0,3));
            Assert(!cabinet.Toggle()&&cabinet.IsOpen,"Remote cabinet operation accepted");
            camera.transform.position=cabinet.transform.TransformPoint(new Vector3(0,0,.65f));
            Assert(cabinet.Toggle()&&!cabinet.IsOpen&&Quaternion.Angle(closedDoor,door.localRotation)<.001f,"Cabinet did not close exactly");
            Assert(Quaternion.Angle(lockedLatch,cabinetLatch.localRotation)<.001f,"Cabinet catch did not relock");
            Assert(cabinet.Toggle(),"Cabinet failed to reopen");cabinet.enabled=false;
            Assert(!cabinet.IsOpen&&Quaternion.Angle(closedDoor,door.localRotation)<.001f,"Cabinet disable restoration failed");cabinet.enabled=true;
            Debug.Log("NF5K_STORAGE_CABINET_OK: real door ray, context command, 100 degree hinge, selected seat clearance, distance guard and exact restoration");
            var manual=Array.Find(state.GetComponentsInChildren<CabSafetyEquipment>(),t=>t.IsManual);
            Assert(manual!=null,"Fire equipment manual missing");
            var bottom=manual.transform.Find("Manual_Pocket_Load_Bearing_Bottom");
            var pages=manual.transform.Find("Manual_Bound_Page_Block");
            var cover=manual.transform.Find("Manual_Back_Cover");
            Assert(bottom!=null&&pages!=null&&cover!=null,"Manual load path missing");
            float shelfTop=bottom.GetComponent<Renderer>().bounds.max.y;
            Assert(Mathf.Abs(cover.GetComponent<Renderer>().bounds.min.y-shelfTop)<.0001f,"Book cover not seated on bottom shelf");
            Assert(pages.GetComponent<Renderer>().bounds.min.y>=shelfTop-.0001f,"Page block penetrates shelf");
            var pointers=Array.FindAll(state.GetComponentsInChildren<Transform>(),t=>t.name=="Pressure_Gauge_Tapered_Pointer");
            Assert(pointers.Length==2,"Pressure pointer count");
            foreach(var pointer in pointers)Assert(Quaternion.Angle(pointer.localRotation,Quaternion.identity)>1&&Quaternion.Angle(pointer.localRotation,Quaternion.identity)<12,"Pointer must vary within green zone");
            Assert(Quaternion.Angle(pointers[0].localRotation,pointers[1].localRotation)>5,"Pointers should not be identical");
            foreach(var pointer in pointers)
            {
                var assembly=pointer.parent;
                var pin=assembly.Find("Safety_Pin_Shaft").GetComponent<Renderer>();
                var lever=assembly.Find("Squeeze_Lever_Continuous").GetComponent<Renderer>();
                Assert(pin.bounds.Intersects(lever.bounds),"Safety pin misses the squeeze lever envelope");
                int ears=0;foreach(Transform child in assembly)if(child.name=="Valve_Hinge_Clevis")
                {ears++;Assert(pin.bounds.Intersects(child.GetComponent<Renderer>().bounds),"Safety pin misses a fixed clevis ear");}
                Assert(ears==2,"Fixed clevis ear count");
                Assert(!pin.bounds.Intersects(assembly.Find("Lever_Hinge_Pin").GetComponent<Renderer>().bounds),"Locking pin intersects hinge pin");
                var lens=assembly.Find("Pressure_Gauge_Convex_Lens");
                var hub=assembly.Find("Pressure_Gauge_Pointer_Hub");
                Assert(lens.localPosition.z-lens.localScale.z*.5f>hub.localPosition.z+hub.localScale.z*.5f,"Gauge hub intersects glass");
            }
            Debug.Log("NF5K_EXTINGUISHER_ASSEMBLY_OK: locking pin intersects lever/clevis envelopes, clears hinge pin; pointer hub clears glass; not a strength or valve solver");
            Debug.Log("NF5K_SERVICE_MOUNTING_OK: book seated on shelf; pointers distinct and inside green zone; static authored mounting only");
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
