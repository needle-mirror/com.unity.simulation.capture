#if !UNITY_SIMULATION_SDK_DISABLED
namespace Unity.Simulation
{
    /// <summary>
    /// Condition object to evaluate if a specific number of simulation seconds (scaled) since object
    /// instantiation have elapsed.
    /// </summary>
    public class SimSecondsCondition : ICondition
    {
        private double _startTime;
        private double _seconds;
        
        public SimSecondsCondition(double seconds)
        {
            _startTime = Manager.Instance.SimulationElapsedTime;
            _seconds = seconds;
        }

        /// <summary>
        /// Check if the specified number of simulation seconds (scaled) have passed.
        /// </summary>
        /// <returns>True if the specified number of seconds has elapsed.</returns>
        public bool HasConditionBeenMet()
        {
            double elapsedSeconds = Manager.Instance.SimulationElapsedTime - _startTime;

            return elapsedSeconds >= _seconds;
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
