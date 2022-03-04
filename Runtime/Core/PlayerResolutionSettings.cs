using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.Simulation
{
    [Serializable]
    public struct PlayerResolution
    {
        public int width;
        public int height;
    }
    
    [CreateAssetMenu(fileName = "Assets/Resources/PlayerResolutionSettings", menuName = "Cloud Rendering/PlayerResolutionSettings")]
    public class PlayerResolutionSettings : ScriptableObject
    {
        public PlayerResolution playerResolution;
        public RenderTexture renderTexture;
    }
}
