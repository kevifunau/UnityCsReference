// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Script.Engine;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Video;

namespace UnityEngine.Video
{
    [RequiredByNativeCode]
    public enum VideoRenderMode
    {
        CameraFarPlane   = 0,
        CameraNearPlane  = 1,
        RenderTexture    = 2,
        MaterialOverride = 3,
        APIOnly          = 4
    }

    [RequiredByNativeCode]
    public enum Video3DLayout
    {
        No3D         = 0,
        SideBySide3D = 1,
        OverUnder3D  = 2
    }

    [RequiredByNativeCode]
    public enum VideoAspectRatio
    {
        NoScaling       = 0,
        FitVertically   = 1,
        FitHorizontally = 2,
        FitInside       = 3,
        FitOutside      = 4,
        Stretch         = 5
    }

    [RequiredByNativeCode]
    [System.Obsolete("VideoTimeSource is deprecated. Use TimeUpdateMode instead. (UnityUpgradable) -> VideoTimeUpdateMode")]
    public enum VideoTimeSource
    {
        [System.Obsolete("AudioDSPTimeSource is deprecated. Use DSPTime instead. (UnityUpgradable) -> DSPTime")]
        AudioDSPTimeSource = 0,
        [System.Obsolete("GameTimeSource is deprecated. Use GameTime instead. (UnityUpgradable) -> GameTime")]
        GameTimeSource     = 1
    }

    [RequiredByNativeCode]
    public enum VideoTimeReference
    {
        Freerun         = 0,
        InternalTime    = 1,
        ExternalTime    = 2
    }

    [RequiredByNativeCode]
    public enum VideoSource
    {
        VideoClip = 0,
        Url       = 1
    }

    [RequiredByNativeCode]
    public enum VideoTimeUpdateMode
    {
        DSPTime          = 0,
        GameTime         = 1,
        UnscaledGameTime = 2
    }

    [RequiredByNativeCode]
    public enum VideoAudioOutputMode
    {
        None        = 0,
        AudioSource = 1,
        Direct      = 2,
        APIOnly     = 3
    }
}

