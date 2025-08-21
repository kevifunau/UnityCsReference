// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Script.UtuRuntime;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.IO;
using UnityEngine.Scripting;

namespace UnityEngine.Video
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/Video/Public/VideoClip.h")]
    public sealed class VideoClip : Object
    {
        private VideoClip() {}
        private FVideoClip CacheVideoClip;
        public VideoClip(FVideoClip videoClip)
        {
            originalPath = videoClip.OriginalPath.ToString();
            frameCount = videoClip.frameCount;
            frameRate = videoClip.frameRate;
            length = videoClip.Duration;
            width = videoClip.width;
            height = videoClip.height;
            pixelAspectRatioNumerator = videoClip.pixelAspectRatioNumerator;
            pixelAspectRatioDenominator = videoClip.pixelAspectRatioDenominator;
            sRGB = videoClip.sRGB;
            audioTrackCount = videoClip.audioTrackCount;
            CacheVideoClip = videoClip;
        }

        public string originalPath { get; }

        public ulong frameCount { get; }

        public double frameRate { get; }

        public double length { get; }

        public uint width { get; }

        public uint height { get; }

        public uint pixelAspectRatioNumerator { get; }

        public uint pixelAspectRatioDenominator { get; }

        public bool sRGB { get; }

        public ushort audioTrackCount { get; }

        public ushort GetAudioChannelCount(ushort audioTrackIdx)
        {
            return 0;
        }
        public uint GetAudioSampleRate(ushort audioTrackIdx)
        {
            return 0;
        }
        public string GetAudioLanguage(ushort audioTrackIdx)
        {
            return "";
        }
    }
}
