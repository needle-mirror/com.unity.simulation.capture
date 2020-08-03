using System;
using System.Collections.Generic;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
#if URP_ENABLED
using UnityEngine.Rendering.Universal;
#endif

#if HDRP_ENABLED
using UnityEngine.Rendering.HighDefinition;
#endif

#if UNITY_2019_3_OR_NEWER

namespace Unity.Simulation
{

    /// <summary>
    /// An enum indicating the type of rendering pipeline being used.
    /// </summary>
    public enum RenderingPipelineType
    {
        URP,
        HDRP,
        BUILTIN
    }

    /// <summary>
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public class SRPSupport
    { 
        static SRPSupport _instance = null;

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            _instance = new SRPSupport();

            if (_instance.UsingCustomRenderPipeline())
            {
                CaptureCamera.SRPSupport = _instance;

                Manager.Instance.StartNotification += () =>
                {
                    RenderPipelineManager.endFrameRendering += (ScriptableRenderContext context, Camera[] cameras) =>
                    {
                        foreach (var camera in cameras)
                        {
                            if (Application.isPlaying && _instance._pendingCameraRequests.ContainsKey(camera))
                            {
                                var pendingRequests = _instance._pendingCameraRequests[camera].ToArray();
                                _instance._pendingCameraRequests[camera].Clear();

                                foreach (var r in pendingRequests)
                                {
                                    r.data.colorFunctor?.Invoke(r);
                                    r.data.depthFunctor?.Invoke(r);
                                    r.data.motionVectorsFunctor?.Invoke(r);
                                }
                            }
                        }
                    };
                };
            }
        }

        Dictionary<Camera, List<AsyncRequest<CaptureCamera.CaptureState>>> _pendingCameraRequests = new Dictionary<Camera, List<AsyncRequest<CaptureCamera.CaptureState>>>();

        /// <summary>
        /// Returns true if using a custom render pipeline or false otherwise.
        /// </summary>
        /// <returns>bool</returns>
        public bool UsingCustomRenderPipeline()
        {
            return GraphicsSettings.currentRenderPipeline != null;
        }

        /// <summary>
        /// Get Current Rendering Pipeline type.
        /// </summary>
        /// <returns>RenderingPipelineType indicating type of current renering pipeline : (URP/HDRP/Built-in)</returns>
        public RenderingPipelineType GetCurrentPipelineRenderingType()
        {
#if URP_ENABLED
            if (UsingCustomRenderPipeline() && RenderPipelineManager.currentPipeline is UniversalRenderPipeline)
                return RenderingPipelineType.URP;
#endif
#if HDRP_ENABLED
            if (UsingCustomRenderPipeline() && RenderPipelineManager.currentPipeline is UnityEngine.Rendering.HighDefinition.HDRenderPipeline)
                return RenderingPipelineType.HDRP;
#endif

            return RenderingPipelineType.BUILTIN;
        }

        /// <summary>
        /// With different rendering pipelines, the moment when you need to capture a camera migh be different.
        /// This method will allow for the CaptureCamera class to operate as normal, while allowing the author
        /// of the render pipeline to decide when the work get dispatched.
        /// </summary>
        /// <param name="camera">The camera that you wish to queue a request for.</param>
        /// <param name="request">The request you are queueing for this camera.</param>
        public void QueueCameraRequest(Camera camera, AsyncRequest<CaptureCamera.CaptureState> request)
        {
            if (!_instance._pendingCameraRequests.ContainsKey(camera))
                _instance._pendingCameraRequests.Add(camera, new List<AsyncRequest<CaptureCamera.CaptureState>>());
            _instance._pendingCameraRequests[camera].Add(request);
        }
    }
}

#endif
