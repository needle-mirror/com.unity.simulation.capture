#if !UNITY_SIMULATION_SDK_DISABLED
using System;
using System.Diagnostics;

using UnityEngine;

namespace Unity.Simulation
{
    /// <summary>
    /// Enumeration for the various supported timer sources.
    /// </summary>
    [Serializable]
    public enum TimerSource
    {
        None,
        [Tooltip("C# Stopwatch (wall time)")]
        Stopwatch,
        [Tooltip("Time.timeSinceLevelLoad")]
        Time,
        [Tooltip("Time.unscaledTime")]
        UnscaledTime,
        [Tooltip("Time.fixedTime")]
        FixedTime,
        [Tooltip("Time.fixedUnscaledTime")]
        FixedUnscaledTime,
        [Tooltip("Unix Epoch Time (since Jan 1st. 1970)")]
        UnixEpoch
    }

    /// <summary>
    /// Timer class for abstracting elapsed time from various sources.
    /// </summary>
    [Serializable]
    public class Timer
    {
        Stopwatch stopwatch;

        public Timer()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        /// <summary>
        /// The selecter timer source. Defaults to Time.timeSinceLevelLoad.
        /// </summary>
        public TimerSource timerSource = TimerSource.Time;

        /// <summary>
        /// Returns the elapsed time from the specified timer source.
        /// </summary>
        public double elapsedSeconds
        {
            get
            {
                switch (timerSource)
                {
                    case TimerSource.Stopwatch:
                        return stopwatch.Elapsed.TotalSeconds;
                    case TimerSource.Time:
                        return Time.timeSinceLevelLoad;
                    case TimerSource.UnscaledTime:
                        return Time.unscaledTime;
                    case TimerSource.FixedTime:
                        return Time.fixedTime;
                    case TimerSource.FixedUnscaledTime:
                        return Time.fixedUnscaledTime;
                    case TimerSource.UnixEpoch:
                        return (double)DateTime.Now.ToUniversalTime().Subtract(TimeUtility.UnixEpoch).TotalSeconds;
                    default:
                        throw new InvalidOperationException("Invalid TimeSource specified.");
                }
            }
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
