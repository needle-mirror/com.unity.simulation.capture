#if !UNITY_SIMULATION_SDK_DISABLED
using System;

using UnityEngine;

namespace Unity.Simulation
{
    /// <summary>
    /// Component for limiting the simulation based on time.
    /// Add this component to a GameObject and set the time in seconds for how long you want the simulation to run.
    /// </summary>
    public class TimeLimit : MonoBehaviour
    {
        /// <summary>
        /// Timer source to use when measuring elapsed time.
        /// </summary>
        public Timer timer;

        /// <summary>
        /// The maxmimum time limit you can specify. Values will be clamped to this maximum.
        /// </summary>
        public const float kMaximumTimeLimitInSeconds = 14400;

        /// <summary>
        /// The desired time limit in seconds. Once this limit is reached, the simulation will be shutdown.
        /// </summary>
        public float TimeLimitInSeconds;

        void Start()
        {
            timer = new Timer();
            TimeLimitInSeconds = Mathf.Clamp(TimeLimitInSeconds, 0, kMaximumTimeLimitInSeconds);
        }

        void Update()
        {
            if (timer.elapsedSeconds >= TimeLimitInSeconds)
            {
                Log.V($"Time limit reached {TimeLimitInSeconds} seconds.");

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void OnValidate()
        {
            if (TimeLimitInSeconds == 0 || TimeLimitInSeconds > kMaximumTimeLimitInSeconds)
            {
                Log.W($"TimeLimit must be set to a valid number of seconds, where 0 < limit < {kMaximumTimeLimitInSeconds}.");
            }
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
