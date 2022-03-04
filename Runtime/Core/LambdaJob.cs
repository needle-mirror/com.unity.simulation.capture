#if !UNITY_SIMULATION_SDK_DISABLED
using System;
using System.Runtime.InteropServices;

using Unity.Jobs;

namespace Unity.Simulation
{
    public struct LambdaJob : IJob
    {
        public delegate void DelegateType();

        GCHandle _handle;

        public void Execute()
        {
            (_handle.Target as DelegateType).Invoke();
            _handle.Free();
        }

        public LambdaJob(DelegateType lambda)
        {
            _handle = GCHandle.Alloc(lambda, GCHandleType.Normal);
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
