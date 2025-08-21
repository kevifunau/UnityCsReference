using System.Collections.Generic;
using Script.CoreUObject;
using UnityEngine;
using UnityEngine.UIElements.Collections;

namespace GUSD.Utils;

public class U3TextureUtil
{
    private static readonly Dictionary<EPixelFormat, TextureFormat> Formats =
        new()
        {
            // TODO All
            {EPixelFormat.PF_R8G8B8A8, TextureFormat.RGBA32},
            {EPixelFormat.PF_B8G8R8A8, TextureFormat.ARGB32},
            {EPixelFormat.PF_G8, TextureFormat.R8},
            {EPixelFormat.PF_R8, TextureFormat.R8},
            {EPixelFormat.PF_DXT1, TextureFormat.DXT1},
            {EPixelFormat.PF_DXT5, TextureFormat.DXT5},
            {EPixelFormat.PF_BC6H, TextureFormat.BC6H},
            {EPixelFormat.PF_BC7, TextureFormat.BC7},
            {EPixelFormat.PF_ETC2_RGBA, TextureFormat.ETC2_RGBA8},
            {EPixelFormat.PF_ETC2_RGB, TextureFormat.ETC2_RGBA1},
            {EPixelFormat.PF_FloatRGBA, TextureFormat.RGBAHalf},
            {EPixelFormat.PF_A32B32G32R32F, TextureFormat.RGBAFloat},
            {EPixelFormat.PF_G16, TextureFormat.R16},
            {EPixelFormat.PF_PVRTC2, TextureFormat.PVRTC_RGBA2},
            {EPixelFormat.PF_PVRTC4, TextureFormat.PVRTC_RGBA4},
        };
    
    public static TextureFormat ConvertU1FormatToU3(EPixelFormat u1Format)
    {
        return Formats.Get(u1Format);
    }
}