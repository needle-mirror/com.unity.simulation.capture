#if !UNITY_SIMULATION_SDK_DISABLED
using UnityEngine;

namespace Unity.Simulation
{
    /// <summary>
    /// Condition object to evaluate if a specific number of frames since object instantiation have elapsed.
    /// </summary>
    public class FrameCountCondition : ICondition
    {
        private int _startFrame;
        private int _frameCount;
        
        public FrameCountCondition(int frameCount)
        {
            _startFrame = Time.frameCount;
            _frameCount = frameCount;
        }

        /// <summary>
        /// Check if the specified number of frames have passed.
        /// </summary>
        /// <returns>True if the frame count has been met or exceeded.</returns>
        public bool HasConditionBeenMet()
        {
            int elapsedFrames = Time.frameCount - _startFrame;

            return elapsedFrames >= _frameCount;
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
