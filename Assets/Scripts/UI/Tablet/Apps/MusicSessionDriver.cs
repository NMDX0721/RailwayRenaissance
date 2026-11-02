using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public sealed class MusicSessionDriver : MonoBehaviour
    {
        void Update() => MusicLibrarySession.Tick(Time.unscaledDeltaTime);
        void OnApplicationPause(bool paused) { if (paused) MusicLibrarySession.Save(); }
        void OnApplicationQuit() => MusicLibrarySession.Save();
    }
}
