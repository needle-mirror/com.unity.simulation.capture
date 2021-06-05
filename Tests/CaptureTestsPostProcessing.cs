#if URP_ENABLED
using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Collections;
using Unity.Simulation;

using UnityEngine.TestTools;
using NUnit.Framework;

public class CaptureTestsPostProcessing
{
    [UnityTest]
    public IEnumerator CaptureTestsPostProcessing_CheckPostProcessingHasBeenApplied()
    {
        var scene = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("PostProcessing"));

        var camera = Camera.main;
        Debug.Assert(camera != null);
        camera.depthTextureMode = DepthTextureMode.Depth;

        yield return null;

        var colorFormat  = GraphicsFormat.R8G8B8A8_UNorm;
        var depthFormat  = GraphicsFormat.R32_SFloat;

        var request = CaptureCamera.Capture
        (
            camera,
            r => AsyncRequest.Result.Completed,
            colorFormat,
            r => AsyncRequest.Result.Completed,
            depthFormat,
            forceFlip: ForceFlip.None
        );

        var count = 20;
        while (!request.completed && count-- > 0)
            yield return null;
        Assert.False(request.error);

        var colorData = ArrayUtilities.Cast<Color32>((Array)request.data.colorBuffer);
        var depthData = ArrayUtilities.Cast<float>((Array)request.data.depthBuffer);

        Assert.True(EdgesAroundBBAreCloserToRedThanBlack(colorData, depthData, camera.pixelWidth, camera.pixelHeight));

        UnityEngine.Object.DestroyImmediate(scene);
    }

    [UnityTest]
    public IEnumerator CaptureTestsPostProcessing_CheckPostProcessingHas_NOT_BeenApplied()
    {
        var scene = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("NoPostProcessing"));

        var camera = Camera.main;
        Debug.Assert(camera != null);
        camera.depthTextureMode = DepthTextureMode.Depth;

        yield return null;

        var colorFormat  = GraphicsFormat.R8G8B8A8_UNorm;
        var depthFormat  = GraphicsFormat.R32_SFloat;

        var request = CaptureCamera.Capture
        (
            camera,
            r => AsyncRequest.Result.Completed,
            colorFormat,
            r => AsyncRequest.Result.Completed,
            depthFormat,
            forceFlip: ForceFlip.None
        );

        var count = 20;
        while (!request.completed && count-- > 0)
            yield return null;
        Assert.False(request.error);

        var colorData = ArrayUtilities.Cast<Color32>((Array)request.data.colorBuffer);
        var depthData = ArrayUtilities.Cast<float>((Array)request.data.depthBuffer);

        Assert.False(EdgesAroundBBAreCloserToRedThanBlack(colorData, depthData, camera.pixelWidth, camera.pixelHeight));

        UnityEngine.Object.DestroyImmediate(scene);
    }

    bool EdgesAroundBBAreCloserToRedThanBlack(Color32[] colorData, float[] depthData, int w, int h)
    {
        var bb = CalculateBBFromDepthData(colorData, depthData, w, h);
        Debug.Log($"Expanded BB from depth: {bb}");
        return ExpectedEdgeColor(colorData, bb.x,            bb.y,             w, 1, bb.width,  Color.red, Color.black) &&
               ExpectedEdgeColor(colorData, bb.x,            bb.y,             w, w, bb.height, Color.red, Color.black) &&
               ExpectedEdgeColor(colorData, bb.x,            bb.y + bb.height, w, 1, bb.width,  Color.red, Color.black) &&
               ExpectedEdgeColor(colorData, bb.x + bb.width, bb.y,             w, w, bb.height, Color.red, Color.black);
    }

    bool ExpectedEdgeColor(Color32[] colorData, int x, int y, int w, int incr, int count, Color32 to, Color32 than)
    {
        var startIndex = y * w + x;
        for (var i = 0; i < count; ++i)
        {
            var index = startIndex + i * incr;
            x = startIndex % w;
            y = startIndex / w;
            var color = colorData[index];
            if (!ColorCloser(color, to, than))
            {
                Debug.Log($"ExpectedEdgeColor: expected {to} at index {index} ({x}, {y}), but found {color}");
                return false;
            }
        }
        return true;
    }

    RectInt CalculateBBFromDepthData(Color32[] colorData, float[] depthData, int width, int height)
    {
        int xmin = int.MaxValue, xmax = int.MinValue, ymin = int.MaxValue, ymax = int.MinValue;
        for (var y = 0; y < height; ++y)
        for (var x = 0; x < width;  ++x)
        if (depthData[y * width + x] < 1)
        {
            colorData[y * width + x] = Color.black;
            xmin = xmin > x ? x : xmin;
            xmax = xmax < x ? x : xmax;
            ymin = ymin > y ? y : ymin;
            ymax = ymax < y ? y : ymax;
        }
        return new RectInt(xmin - 1, ymin - 1, xmax - xmin + 2, ymax - ymin + 2);
    }

    // from https://www.compuphase.com/cmetric.htm
    double ColorDistance(Color32 e1, Color32 e2)
    {
        long rmean = ( (long)e1.r + (long)e2.r ) / 2;
        long r = (long)e1.r - (long)e2.r;
        long g = (long)e1.g - (long)e2.g;
        long b = (long)e1.b - (long)e2.b;
        return Math.Sqrt((((512+rmean)*r*r)>>8) + 4*g*g + (((767-rmean)*b*b)>>8));
    }

    bool ColorCloser(Color32 color, Color32 to, Color32 than)
    {
        return ColorDistance(color, to) < ColorDistance(color, than);
    }
}
#endif // URP_ENABLED