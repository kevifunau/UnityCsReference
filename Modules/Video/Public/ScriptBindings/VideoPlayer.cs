
using System;
using Script.Dynamic;
using Script.Engine;
using Script.MediaAssets;
using Script.UtuRuntime;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Video;
using Script.CoreUObject;

namespace UnityEngine.Video;

[U3Exported(false)]
[RequiredByNativeCode]
[RequireComponent(typeof(Transform))]
[NativeHeader("Modules/Video/Public/VideoPlayer.h")]
public sealed partial class VideoPlayer : Behaviour
{
    private UVideoPlayer componentU1 => (UVideoPlayer) u1Component.GetOwner().GetComponentByClass(UVideoPlayer.StaticClass());

    public VideoSource source { get; set; }
    public VideoTimeUpdateMode timeUpdateMode { get; set; }

    [NativeName("VideoUrl")]
    public string url { get; set; }

    private VideoClip CacheClip;  // multi thread problem
    public VideoClip clip
    {
        get
        {
            componentU1.MediaPlayer.OnEndReached = new FOnMediaPlayerMediaEvent();
            
            if (CacheClip != null) return CacheClip;
            CacheClip = new VideoClip(componentU1.VideoClip);
            return CacheClip;
        }
        set => CacheClip = value;
    }

    public VideoRenderMode renderMode { get; set; }

    public bool canSetTimeUpdateMode
    {
        [NativeName("CanSetTimeUpdateMode")]
        get;
    }

    [NativeHeader("Runtime/Camera/Camera.h")]
    public Camera targetCamera { get; set; }

    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    public RenderTexture targetTexture { get; set; }

    [NativeHeader("Runtime/Graphics/Renderer.h")]
    public UnityEngine.Renderer targetMaterialRenderer { get; set; }

    public string targetMaterialProperty { get; set; }

    internal string effectiveTargetMaterialProperty { get; }

    public VideoAspectRatio aspectRatio { get; set; }

    public float targetCameraAlpha { get; set; }

    public Video3DLayout targetCamera3DLayout { get; set; }

    [NativeHeader("Runtime/Graphics/Texture.h")]
    public Texture texture { get; }

    public void Prepare() { }

    public bool isPrepared
    {
        [NativeName("IsPrepared")]
        get;
    }


    public bool waitForFirstFrame { get; set; }

    public bool playOnAwake { get; set; }

    public void Play()
    {
        componentU1.Play();
    }

    public void Pause()
    {
        componentU1.Pause();
    }

    public void Stop()
    {
        componentU1.Stop();
    }

    public bool isPlaying => componentU1.GetIsPlaying();

    public bool isPaused
    {
        [NativeName("IsPaused")]
        get;
    }

    public bool canSetTime
    {
        [NativeName("CanSetTime")]
        get;
    }

    public double time
    {
        get => componentU1.GetTime();
        set => componentU1.SetTime(value);
    }

    [NativeName("FramePosition")]
    public long frame { get; set; }

    public double clockTime { get; }

    public bool canStep
    {
        [NativeName("CanStep")]
        get;
    }

    public void StepForward() { }

    public bool canSetPlaybackSpeed
    {
        [NativeName("CanSetPlaybackSpeed")]
        get;
    }

    public float playbackSpeed {
        get => componentU1.FRate;
        set => componentU1.SetRate(value);
    }

    [NativeName("Loop")]
    public bool isLooping { get; set; }

    [System.Obsolete("VideoPlayer.canSetTimeSource is deprecated. Use canSetTimeUpdateMode instead. (UnityUpgradable) -> canSetTimeUpdateMode")]
    public bool canSetTimeSource
    {
        [NativeName("CanSetTimeSource")]
        get;
    }

    [System.Obsolete("VideoPlayer.timeSource is deprecated. Use timeUpdateMode instead. (UnityUpgradable) -> timeUpdateMode")]
    public VideoTimeSource timeSource { get; set; }

    public VideoTimeReference timeReference { get; set; }

    public double externalReferenceTime { get; set; }

    public bool canSetSkipOnDrop
    {
        [NativeName("CanSetSkipOnDrop")]
        get;
    }

    public bool skipOnDrop { get; set; }

    public ulong frameCount { get; }

    public float frameRate { get; }

    public double length
    {
        get => componentU1.VideoClip.Duration;
    }

    public uint width { get; }

    public uint height { get; }

    public uint pixelAspectRatioNumerator { get; }

    public uint pixelAspectRatioDenominator { get; }

    public ushort audioTrackCount { get; }

    public string GetAudioLanguageCode(ushort trackIndex)
    {
        return "";
    }

    public ushort GetAudioChannelCount(ushort trackIndex)
    {
        return 0;
    }

    public uint GetAudioSampleRate(ushort trackIndex)
    {
        return 0;
    }

    public static ushort controlledAudioTrackMaxCount { get; }

    public ushort controlledAudioTrackCount
    {
        get
        {
            return GetControlledAudioTrackCount();
        }

        set
        {
            int maxNumTracks = controlledAudioTrackMaxCount;
            if (value > maxNumTracks)
                throw new ArgumentException(string.Format("Cannot control more than {0} tracks.", maxNumTracks), "value");

            SetControlledAudioTrackCount(value);
        }
    }

    private ushort GetControlledAudioTrackCount()
    {
        return 0;
    }

    private void SetControlledAudioTrackCount(ushort value) { }

    public void EnableAudioTrack(ushort trackIndex, bool enabled) { }

    public bool IsAudioTrackEnabled(ushort trackIndex)
    {
        return false;
    }

    public VideoAudioOutputMode audioOutputMode { get; set; }

    public bool canSetDirectAudioVolume
    {
        [NativeName("CanSetDirectAudioVolume")]
        get;
    }

    public float GetDirectAudioVolume(ushort trackIndex)
    {
        return 0;
    }

    public void SetDirectAudioVolume(ushort trackIndex, float volume)
    {
        componentU1.SetVolume(volume);
    }

    public bool GetDirectAudioMute(ushort trackIndex)
    {
        return false;
    }

    public void SetDirectAudioMute(ushort trackIndex, bool mute) { }

    [NativeHeader("Modules/Audio/Public/AudioSource.h")]
    public AudioSource GetTargetAudioSource(ushort trackIndex)
    {
        return null;
    }

    public void SetTargetAudioSource(ushort trackIndex, AudioSource source) { }

    public delegate void EventHandler(VideoPlayer source);
    public delegate void ErrorEventHandler(VideoPlayer source, string message);
    public delegate void FrameReadyEventHandler(VideoPlayer source, long frameIdx);
    public delegate void TimeEventHandler(VideoPlayer source, double seconds);
    
    public event EventHandler prepareCompleted;
    public event EventHandler loopPointReached;
    public event EventHandler started;
    public event EventHandler frameDropped;
    public event ErrorEventHandler errorReceived;
    public event EventHandler seekCompleted;
    public event TimeEventHandler clockResyncOccurred;

    public bool sendFrameReadyEvents
    {
        [NativeName("AreFrameReadyEventsEnabled")]
        get;
        [NativeName("EnableFrameReadyEvents")]
        set;
    }

    public FrameReadyEventHandler frameReady;






    #region RequiredByNativeCode

    
    [RequiredByNativeCode]
    private static void InvokePrepareCompletedCallback_Internal(VideoPlayer source)
    {
        if (source.prepareCompleted != null)
            source.prepareCompleted(source);
    }

    [RequiredByNativeCode]
    private static void InvokeFrameReadyCallback_Internal(VideoPlayer source, long frameIdx)
    {
        if (source.frameReady != null)
            source.frameReady(source, frameIdx);
    }

    [RequiredByNativeCode]
    private static void InvokeLoopPointReachedCallback_Internal(VideoPlayer source)
    {
        if (source.loopPointReached != null)
            source.loopPointReached(source);
    }

    [RequiredByNativeCode]
    private static void InvokeStartedCallback_Internal(VideoPlayer source)
    {
        if (source.started != null)
            source.started(source);
    }

    [RequiredByNativeCode]
    private static void InvokeFrameDroppedCallback_Internal(VideoPlayer source)
    {
        if (source.frameDropped != null)
            source.frameDropped(source);
    }

    [RequiredByNativeCode]
    private static void InvokeErrorReceivedCallback_Internal(VideoPlayer source, string errorStr)
    {
        if (source.errorReceived != null)
            source.errorReceived(source, errorStr);
    }

    [RequiredByNativeCode]
    private static void InvokeSeekCompletedCallback_Internal(VideoPlayer source)
    {
        if (source.seekCompleted != null)
            source.seekCompleted(source);
    }

    [RequiredByNativeCode]
    private static void InvokeClockResyncOccurredCallback_Internal(VideoPlayer source, double seconds)
    {
        if (source.clockResyncOccurred != null)
            source.clockResyncOccurred(source, seconds);
    }

    internal static event Action<string> analyticsSent;

    [RequiredByNativeCode]
    private static void InvokeAnalyticsSentCallback_Internal(string analytics)
    {
        if (analyticsSent != null)
            analyticsSent(analytics);
    }

    [RequiredByNativeCode]
    private static bool AnalyticsEventHandlerAttached_Internal()
    {
        return analyticsSent != null;
    }

    #endregion
}