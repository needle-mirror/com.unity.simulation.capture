#if !UNITY_SIMULATION_SDK_DISABLED
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Simulation
{
    /// <summary>
    /// 
    /// </summary>
    public class TimeScale : MonoBehaviour
    {
        /// <summary>
        /// </summary>
        public int targetFrameRate = 60;

        /// <summary>
        /// </summary>
        public int timeScale = 1;

        /// <summary>
        /// </summary>
        public bool autoScale = true;

        /// <summary>
        /// </summary>
        public int targetUtilizationPercentage = 100;

        /// <summary>
        /// </summary>
        public float frameJitterDampening = 0.9f;

        float averageFrameTime;

        void Awake()
        {
            if (targetFrameRate <= 0)
            {
                targetFrameRate = Application.targetFrameRate;
                if (targetFrameRate <= 0)
                    targetFrameRate = 60;
            }
        }

        void Start()
        {
            QualitySettings.vSyncCount = 0;
            if (targetFrameRate > 0 && timeScale > 0)
            {
                Time.captureFramerate = targetFrameRate * timeScale;
                Time.timeScale = timeScale;
            }
        }

        void Update()
        {
            averageFrameTime = (1 - frameJitterDampening) * Time.unscaledDeltaTime + frameJitterDampening * averageFrameTime;
            if (targetFrameRate > 0 && autoScale)
            {
                var frameDeltaTime  = 1.0f / targetFrameRate;
                var utilizationTime = frameDeltaTime * targetUtilizationPercentage * 0.01f;

                timeScale = (int)Mathf.Ceil(utilizationTime / averageFrameTime);
#if UNITY_EDITOR
                timeScale = Mathf.Clamp(timeScale, 1, 100);
#endif
                if (timeScale != Time.timeScale)
                {
                    Time.captureFramerate = targetFrameRate * timeScale;
                    Time.timeScale = timeScale;
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TimeScale))]
    public class TimeScaleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var ts = target as TimeScale;

            ts.targetFrameRate = EditorGUILayout.IntField("Target Frame Rate", ts.targetFrameRate);
            ts.autoScale = EditorGUILayout.Toggle("Auto Scale", ts.autoScale);
            if (ts.autoScale)
            {
                EditorGUILayout.LabelField("Time Scale", ts.timeScale.ToString());
                ts.targetUtilizationPercentage = EditorGUILayout.IntSlider("Target Utilization Percentage", ts.targetUtilizationPercentage, 1, 100);
                ts.frameJitterDampening = EditorGUILayout.Slider("Frame Jitter Dampening", ts.frameJitterDampening, 0, 1);
            }
            else
                ts.timeScale = EditorGUILayout.IntSlider("Time Scale", ts.timeScale, 1, 1000);
        }
    }
#endif // UNITY_EDITOR
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
