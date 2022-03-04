using UnityEditor;

namespace Unity.Simulation
{
    /// <summary>
    /// Utility class that shuts down the SDK when exiting playmode.
    /// </summary>
    [InitializeOnLoad]
    public static class ExitPlaymode
    {
        static ExitPlaymode()
        {
            EditorApplication.playModeStateChanged += (PlayModeStateChange change) =>
            {
                if (change == PlayModeStateChange.ExitingPlayMode)
                {
                    // If we are paused and in playmode, and you exit, then the shutdown code will not run.
                    // Fortunately, unpausing the application here works, and the shutdown occurs properly.
                    EditorApplication.isPaused = false;

                    Manager.Instance.Shutdown();
                }
            };
        }
    }
}