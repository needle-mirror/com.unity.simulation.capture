#if !UNITY_SIMULATION_SDK_DISABLED
namespace Unity.Simulation
{
    /// <summary>
    /// Condition object to evaluate if a specific number of wall seconds since object instantiation have elapsed.
    /// </summary>
    public class WallSecondsCondition : ICondition
    {
        private double _startTime;
        private double _seconds;

        public WallSecondsCondition(double seconds)
        {
            _startTime = Manager.Instance.WallElapsedTime;
            _seconds = seconds;
        }

        /// <summary>
        /// Check if the specified number of wall seconds have passed.
        /// </summary>
        /// <returns>True if the specified number of seconds has elapsed.</returns>
        public bool HasConditionBeenMet()
        {
            double elapsedSeconds = Manager.Instance.WallElapsedTime - _startTime;

            return elapsedSeconds >= _seconds;
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
