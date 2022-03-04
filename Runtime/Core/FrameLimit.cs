#if !UNITY_SIMULATION_SDK_DISABLED
using System.Diagnostics;

using UnityEngine;

namespace Unity.Simulation
{
    public class FrameLimit : MonoBehaviour
    {
        public int FrameLimitCount;

        void Update()
        {
            if (Time.frameCount >= FrameLimitCount)
            {
                Log.V($"Frame limit reached {FrameLimitCount} frames.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void OnValidate()
        {
            if (FrameLimitCount == 0)
            {
                Log.W($"FrameLimit must be set to a valid number of frames, where 0 < limit");
            }
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
