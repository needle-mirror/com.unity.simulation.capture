#if HDRP_ENABLED
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

using System;
using System.IO;

namespace Unity.Simulation
{
    public class HDRPCallbackPass : CustomPass
    {
        public RenderPassCallbackDelegate callback;

        public HDRPCallbackPass(string name)
        {
            this.name = name;
        }

        protected override void Execute(ScriptableRenderContext context, CommandBuffer commandBuffer, HDCamera hdCamera, CullingResults cullingResult)
        {
            if (Application.isPlaying)
            {
                callback?.Invoke(context, hdCamera.camera, commandBuffer);
            }
        }
    }
}
#endif // HDRP_ENABLED
