#if !USIM_USE_BUILTIN_PNG_ENCODER
using System;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class PngEncoder 
{
    [Flags]
    public enum PngParam : int
    {
        /**
        * No PNG parameters
        */
        None = 0,

        /**
        * Flip the image vertically
        */
        FlipY = 1
    }

    [Flags]
    public enum ColorType
    {
        /**
        * INVALID pixel format.
        */
        PNG_INVALID = -1,
        /**
        * Grayscale pixel format.  Each pixel represents a luminance
        * (brightness) level based on the bit depth.
        */
        PNG_GREY = 0,
        /**
        * RGB pixel format.  The red, green, and blue components in the image are
        * stored in 3 pixels in the order R, G, B.
        */
        PNG_RGB = 2,
        /**
        * Palette pixel format.  Each pixel represents a palette index
        * into a color look up table based on the bit depth.
        */
        PNG_PALETTE = 3,
        /**
        * Grayscale with alpha pixel format.  Each pixel represents a luminance
        * (brightness) level with an alpha component based on the bit depth.
        */
        PNG_GREY_ALPHA = 4,
        /**
        * RGBA pixel format.  The red, green, blue and alpha components 
        * in the image are stored in 4 pixels in the order R, G, B, A.
        */
        PNG_RGBA = 6
    }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("pngslz")]
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_CLOUD_RENDERING
    [DllImport("libpngslz.so")]
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libpngslz")]
#else
#error Unsupported Platform for PngEncoder
#endif
    private static extern int png_slz_compress(byte[] dstBuf, ref int outSize, byte[] srcBuf, int width, int height, int colorType, int bitDepth, int param);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("pngslz")]
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_CLOUD_RENDERING
    [DllImport("libpngslz.so")]
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libpngslz")]
#else
#error Unsupported Platform for PngEncoder
#endif
    private static extern int png_slz_inspect(byte[] srcBuf, int srcSize,
    ref int width, ref int height, ref int colorType, ref int bitDepth, ref int outsize);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("pngslz")]
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_CLOUD_RENDERING
    [DllImport("libpngslz.so")]
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("libpngslz")]
#else
#error Unsupported Platform for PngEncoder
#endif
    private static extern int png_slz_load(byte[] dstBuf, ref int outsize, byte [] srcBuf, int srcSize, ref int width, ref int height, ref int colorType, ref int bitDepth);

    public static ColorType GetTypeAndDepth(int blocksize, int componentCount, ref int bitDepth)
    {
        if (componentCount == 0)
            return ColorType.PNG_INVALID;

        bitDepth = (blocksize/componentCount) * 8;
        if (componentCount == 1) 
            return ColorType.PNG_GREY;
        if (componentCount == 2)
            return ColorType.PNG_GREY_ALPHA;
        if (componentCount == 3) 
            return ColorType.PNG_RGB;
        if (componentCount == 4)
            return ColorType.PNG_RGBA;
        return ColorType.PNG_INVALID;
    }

    public static byte[] Encode(byte[] raw, int width, int height, ColorType colorType, int bitDepth, PngParam pngParam = PngParam.None)
    {
        Debug.Assert(raw != null, "Input array cannot be null");
        Debug.Assert(raw.Length != 0, "Array cannot be empty");
        
        if (bitDepth == 0)
        {
            throw new ArgumentException("Pixel size is not supported.");
        }

        int size = 0;
        var temp = new byte[raw.Length*2 + 1024];

        var result = png_slz_compress(temp, ref size, raw, width, height, (int)colorType, bitDepth, (int)pngParam);

        if (result < 0)
            return null;

        var pngData = new byte[size];
        Array.Copy(temp, 0, pngData, 0, size);

        return pngData;
    }
    
    public static int Inspect(byte[] raw, ref int width, ref int height, ref ColorType colorType, ref int bitDepth, ref int outsize)
    {
        int nColorType = -1;
        var result = png_slz_inspect(raw, raw.Length, ref width, ref height, ref nColorType, ref bitDepth, ref outsize);
        colorType = (ColorType)nColorType;
        return result;
    }

    public static byte[] Decode(byte[] raw, ref int width, ref int height, ref ColorType colorType, ref int bitDepth)
    {
        Debug.Assert(raw != null, "Input array cannot be null");
        Debug.Assert(raw.Length != 0, "Array cannot be empty");
        int size = 0;
        int nColorType = -1;

        var result = png_slz_inspect(raw, raw.Length, ref width, ref height, ref nColorType, ref bitDepth, ref size);
        if (result < 0)
            return null;
        var imgData = new byte[size];

        result = png_slz_load(imgData, ref size, raw, raw.Length, ref width, ref height, ref nColorType, ref bitDepth);
        if (result < 0)
            return null;

        colorType = (ColorType)nColorType;
        return imgData;
    }
}
#endif // !USIM_USE_BUILTIN_PNG_ENCODER