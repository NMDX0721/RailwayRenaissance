using System;
using RailwayRenaissance.Cab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>PR7 integration checks without rebuilding or saving review scenes.</summary>
public static class PR7CabReview
{
    public static void AtoBatch()
    {
        try
        {
            NF5KSimulationReview.Validate();
            EditorSceneManager.OpenScene("Assets/Scenes/CabAtoResearchReview.unity");
            NF5KAtoPlayReview.Begin();
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    public static void DiffuseBatch()
    {
        var vehicle = new GameObject("PR7DiffuseVehicle");
        vehicle.SetActive(false);
        Color sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor;
        try
        {
            RenderSettings.ambientSkyColor = RenderSettings.ambientEquatorColor = Color.black;
            var lighting = vehicle.AddComponent<CabInteriorLighting>();
            var passenger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            passenger.transform.SetParent(vehicle.transform, false);
            passenger.transform.localPosition = new Vector3(0, 1, 6);
            var cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cab.transform.SetParent(vehicle.transform, false);
            cab.transform.localPosition = new Vector3(0, 1, 0);
            var lampObject = new GameObject("PassengerLamp");
            lampObject.transform.SetParent(vehicle.transform, false);
            lampObject.transform.localPosition = new Vector3(0, 2, 6);
            var lamp = lampObject.AddComponent<Light>();
            var bounce = new GameObject("Bounce").AddComponent<Light>();
            bounce.transform.SetParent(lampObject.transform, false);
            lighting.Configure(null, new[] { new CabInteriorLighting.Fixture
            {
                light = lamp, bounce = bounce, intensity = 1, bounceIntensity = 1,
                circuit = InteriorLightCircuit.PassengerFront
            } });
            var fill = vehicle.AddComponent<CabDiffuseFill>();
            var passengerRenderer = passenger.GetComponent<Renderer>();
            var cabRenderer = cab.GetComponent<Renderer>();
            fill.Configure(new[] { passengerRenderer, cabRenderer });
            vehicle.SetActive(true);
            lighting.SetSupply(true, true);
            lighting.SetPassengerZone(0, true); fill.Refresh();
            Vector4 before = Coefficient(passengerRenderer), cabBefore = Coefficient(cabRenderer);
            if (before.sqrMagnitude < .001f || cabBefore.sqrMagnitude > .000001f)
                throw new InvalidOperationException("Diffuse fixture did not distinguish passenger and cab");
            vehicle.transform.SetPositionAndRotation(new Vector3(31, 2, -100), Quaternion.Euler(0, 63, 0));
            lighting.SetPassengerZone(0, false); fill.Refresh();
            if (Coefficient(passengerRenderer).sqrMagnitude > .000001f)
                throw new InvalidOperationException("Passenger off state retained lamp contribution");
            lighting.SetPassengerZone(0, true); fill.Refresh();
            if ((Coefficient(passengerRenderer) - before).sqrMagnitude > .000001f ||
                (Coefficient(cabRenderer) - cabBefore).sqrMagnitude > .000001f)
                throw new InvalidOperationException("Vehicle movement changed cabin diffuse partition or attenuation");
            Debug.Log("PR7_DIFFUSE_LOCAL_FRAME_OK: passenger/cab partition and on/off SH coefficient invariant after translation and yaw");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        finally
        {
            UnityEngine.Object.DestroyImmediate(vehicle);
            RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator;
        }
    }

    private static Vector4 Coefficient(Renderer renderer)
    {
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        return block.GetVector("unity_SHAr");
    }
}
