#if !UNITY_SIMULATION_SDK_DISABLED
namespace Unity.Simulation
{
    /// <summary>
    /// Interface for defining a specific condition to check for.
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// Check if the condition is met.
        /// </summary>
        /// <returns>True if the condition has been met.</returns>
        bool HasConditionBeenMet();
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
