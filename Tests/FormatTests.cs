#if !UNITY_SIMULATION_SDK_DISABLED
using UnityEngine;

using Unity.Simulation;

using NUnit.Framework;
using NUnit.Framework.Internal;

public class FormatTests
{
    [Test]
    public void FormatFloatsProducesExpectedOutput()
    {
        var floats = new float[]
        {
            0.5168283117f,
            0.1059779524f,
            0.6119241998f,
            0.3220131802f,
            0.5126545982f,
            0.1220944873f,
            0.7932604766f,
            0.8110761667f,
            0.0694901928f,
            0.3618201420f,
        };

        int precision = 2;

        var formatted = Format.Floats(floats, 2, precision);

        var values = formatted.Split(',');

        Assert.True(values.Length == floats.Length);

        for (var i = 0; i < floats.Length; ++i)
        {
            var src = floats[i].ToString($"N{precision}");
            var dst = values[i].Trim();
            Assert.True(src.Equals(dst), $"FormatFloatsProducesExpectedOutput float[{i}] expected {src} but got {dst}");
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
