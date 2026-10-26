using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RailwayRenaissance.Cab;

public static class NF5KContextInteractionReview
{
    public static void BatchPR6()
    {
        try
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/CabSimulationReview.unity");
            NF5KSimulationReview.Validate();NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}
    }
    public static void Validate(CabEngineState state)
    {
        var ui=state.GetComponent<CabContextInteraction>();
        if(ui==null)throw new InvalidOperationException("Context interaction missing");
        string saved=ui.PrimaryPath;
        bool modal=ui.ModalOpen;
        var interactionFlags=BindingFlags.Instance|BindingFlags.NonPublic;
        bool originalCursorAim=(bool)typeof(CabContextInteraction).GetField("cursorAim",interactionFlags).GetValue(ui);
        var originalSettings=InputSystem.settings;
        var background=originalSettings.backgroundBehavior;
        var editorBehavior=originalSettings.editorInputBehaviorInPlayMode;
        originalSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        originalSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var originalKeyboard=Keyboard.current;var originalMouse=Mouse.current;
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Set(string name,object value)=>typeof(CabContextInteraction).GetField(name,flags).SetValue(ui,value);
        void Update()=>typeof(CabContextInteraction).GetMethod("Update",flags).Invoke(ui,null);
        void Pump()=>InputSystem.Update();
        void Assert(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
        var target=Array.Find(state.GetComponentsInChildren<CabDrivingTarget>(),t=>t.Action==CabDrivingAction.IndependentBrake);
        float before=state.IndependentBrakeNotch;
        try
        {
            Assert(!CabContextInteraction.ValidPath("<Keyboard>/Escape")&&!CabContextInteraction.ValidPath("<Mouse>/delta")
                &&!ui.SetPrimaryPath("<Keyboard>/F2",false),"Reserved binding accepted");
            Assert(ui.SetPrimaryPath("<Keyboard>/e",false),"E binding rejected");
            var interiorLights=UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>();
            var exteriorLights=UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>();
            var overOtherUI=typeof(CabContextInteraction).GetMethod("OverOtherUI",flags);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(30,Screen.height-40)});
            Pump();
            Set("focused",target);Set("focusedCollider",target.GetComponentInChildren<Collider>());
            Set("actionRect",new Rect(0,0,Screen.width,Screen.height));Set("settings",false);Set("inputAfter",-1f);
            Assert(exteriorLights==null||exteriorLights.BlocksPointer(new Vector2(30,40)),"Exterior lighting review panel did not block its visible area");
            var interiorPanelPoint=new Vector2(Mathf.Max(250,Screen.width-260)+20,40);
            Assert(interiorLights==null||interiorLights.BlocksPointer(interiorPanelPoint),"Interior lighting review panel did not block its visible area");
            Assert((bool)overOtherUI.Invoke(ui,null),"Context interaction did not honor the exterior lighting panel rectangle");
            InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(300,Screen.height-40)});Pump();
            Assert(!(bool)overOtherUI.Invoke(ui,null),"Lighting panel pointer guard blocked an uncovered area");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));Pump();Update();
            var action=(InputAction)typeof(CabContextInteraction).GetField("primary",flags).GetValue(ui);
            Assert(state.IndependentBrakeNotch==before+.25f,"Rebound E did not execute selected action; pressed="+action.WasPressedThisFrame()+" enabled="+action.enabled+" camera="+Camera.main+" mouse="+Mouse.current.position.ReadValue()+" screen="+Screen.width+"x"+Screen.height+" modal="+ui.ModalOpen+" distance="+Vector3.Distance(Camera.main.transform.position,target.GetComponentInChildren<Collider>().bounds.center)+" focused="+typeof(CabContextInteraction).GetField("focused",flags).GetValue(ui)+" otherUI="+typeof(CabContextInteraction).GetMethod("OverOtherUI",flags).Invoke(ui,null));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());Pump();
            Set("settings",true);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));Pump();Update();
            Assert(state.IndependentBrakeNotch==before+.25f,"Settings click-through changed controls");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());Pump();
            Assert(ui.SetPrimaryPath("<Mouse>/leftButton",false),"Mouse binding rejected");
            Set("settings",false);Set("actionRect",new Rect());Set("inputAfter",-1f);
            // Test action lookup after a binding change without changing the saved user preference.
            InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(300,Screen.height-40)}.WithButton(MouseButton.Left));Pump();
            Assert(action.WasPressedThisFrame(),"Mouse rebinding not active");
            Set("cursorAim",false);
            Assert(interiorLights!=null&&exteriorLights!=null,"Lighting panel fixtures missing from committed review scene");
            foreach(var point in new[]{new Vector2(30,40),interiorPanelPoint})
            {
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(point.x,Screen.height-point.y)});Pump();
                Set("focused",target);Set("focusedCollider",target.GetComponentInChildren<Collider>());Set("inputAfter",-1f);
                Assert((bool)overOtherUI.Invoke(ui,null),"Context pointer guard omitted a lighting panel");
                float priorBrake=state.IndependentBrakeNotch;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(point.x,Screen.height-point.y)}.WithButton(MouseButton.Left));Pump();Update();
                Assert(state.IndependentBrakeNotch==priorBrake,"Mouse lighting-panel click executed the center-reticle cab action");
            }
            Debug.Log("PR6_LIGHTING_POINTER_OK: both lighting panels block actual mouse-primary center-reticle dispatch");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(300,Screen.height-40)});Pump();
            Set("cursorAim",originalCursorAim);
            Assert(ui.SetPrimaryPath("<Keyboard>/e",false),"E restore rejected");
            var equipment=state.GetComponentInChildren<CabSafetyEquipment>();
            if(equipment!=null)
            {
                float brakeBeforeInfo=state.IndependentBrakeNotch;
                Assert(ui.ShowEquipmentInfo(equipment)&&ui.ModalOpen,"Equipment information did not open");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));Pump();Update();
                Assert(state.IndependentBrakeNotch==brakeBeforeInfo&&!equipment.Held,"Information page allowed background interaction");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());Pump();ui.CloseEquipmentInfo();
                Assert(!ui.ModalOpen,"Equipment information did not close");
                Debug.Log("NF5K_EQUIPMENT_INFORMATION_OK: modal equipment dossier blocks background commands and closes cleanly");
            }
            var stop=Array.Find(state.GetComponentsInChildren<CabStartupClickTarget>(),t=>t.Action==CabStartupAction.Stop);
            Set("focused",stop);Set("focusedCollider",stop.GetComponent<Collider>());
            Set("actionRect",new Rect(0,0,Screen.width,Screen.height));Set("inputAfter",-1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));Pump();Update();
            Assert(state.IsEngineStarted,"Critical stop executed on initial press");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());Pump();Update();
            Assert((float)typeof(CabContextInteraction).GetField("holdStart",flags).GetValue(ui)<0&&state.IsEngineStarted,"Released hold was not cancelled");
            var reverser=Array.Find(state.GetComponentsInChildren<CabDrivingTarget>(),t=>t.Action==CabDrivingAction.Reverser);
            var driver=state.GetComponent<CabSimulationDriver>();
            if(reverser!=null&&driver!=null)
            {
                var controls=state.GetComponentInChildren<CabStartupControls>();
                var unavailable=typeof(CabContextInteraction).GetMethod("UnavailableReason",flags);
                foreach(var direction in new[]{ReverserPosition.Forward,ReverserPosition.Reverse})
                {
                    Assert(state.SetReverser(direction),"Stopped reverser setup failed");state.StopEngine();driver.Advance(6);
                    Set("focused",reverser);Set("focusedCollider",reverser.GetComponentInChildren<Collider>());
                    int neutralIndex=direction==ReverserPosition.Forward?1:0;
                    Assert(unavailable.Invoke(ui,new object[]{neutralIndex})==null,"Stopped reverser could not return to neutral");
                    Assert(reverser.Activate(neutralIndex==1)&&state.Reverser==ReverserPosition.Neutral,"Stopped physical reverser failed to return to neutral");
                    Assert(unavailable.Invoke(ui,new object[]{0})!=null&&unavailable.Invoke(ui,new object[]{1})!=null,"Stopped engine allowed selecting a driving direction");
                    Assert(controls.TryStartEngine(),"Neutral recovery did not permit restart");driver.Advance(6);Assert(state.IsEngineRunning,"Recovered engine did not restart");
                }
                Debug.Log("PR6_STOPPED_REVERSER_OK: forward/reverse return to neutral, stopped direction guards and actual restart");
            }
            Debug.Log("NF5K_CONTEXT_INTERACTION_OK: E action dispatch, live mouse rebind, reserved key rejection, modal input isolation, critical hold cancellation; user preferences unchanged");
        }
        finally
        {
            state.SetIndependentBrake(before);Set("cursorAim",originalCursorAim);ui.SetPrimaryPath(saved,false);Set("settings",modal);Set("focused",null);Set("focusedCollider",null);
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            originalKeyboard?.MakeCurrent();originalMouse?.MakeCurrent();
            originalSettings.backgroundBehavior=background;originalSettings.editorInputBehaviorInPlayMode=editorBehavior;
        }
    }
}
