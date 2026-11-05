using UnityEngine;

namespace RailwayRenaissance.Cab
{
    [DisallowMultipleComponent]
    public sealed class CabCoolingFanRotor : MonoBehaviour
    {
        [SerializeField] CabSimulationDriver driver;
        const float VisualSpoolRpmPerSecond = 2600f;

        public float VisualRpm { get; private set; }

        public void Bind(CabSimulationDriver simulationDriver) => driver = simulationDriver;

        void Update()
        {
            float target = driver == null ? 0f : (float)driver.CoolingFanTargetRpm;
            VisualRpm = Mathf.MoveTowards(VisualRpm, target, VisualSpoolRpmPerSecond * Time.deltaTime);
            transform.Rotate(Vector3.right, VisualRpm * 6f * Time.deltaTime, Space.Self);
        }
    }
}
