// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GUSD.Utils;
using Script.CoreUObject;
using Script.Dynamic;
using Script.Engine;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using uei = UnityEngine.Internal;
using OpaqueSortMode = UnityEngine.Rendering.OpaqueSortMode;
using CameraEvent = UnityEngine.Rendering.CameraEvent;
using CommandBuffer = UnityEngine.Rendering.CommandBuffer;
using ComputeQueueType = UnityEngine.Rendering.ComputeQueueType;
using Experimental = UnityEngine.Experimental;
using Rendering = UnityEngine.Rendering;

namespace UnityEngine
{
    [U3Exported]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Camera/RenderManager.h")]
    [NativeHeader("Runtime/GfxDevice/GfxDeviceTypes.h")]
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [NativeHeader("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
    [NativeHeader("Runtime/Misc/GameObjectUtility.h")]
    [NativeHeader("Runtime/Shaders/Shader.h")]
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    public sealed class Camera : Behaviour
    {
        // private UCameraComponent componentU1_ = null;

        private UCameraComponent componentU1 = null;
        private UCameraComponent ueCamera;

        private USceneCaptureComponent2D sceneCaptureComponent2D = null;
    
        void Awake()
        {
            InitializeCorrespondU1Component();
        }

        private bool u1Initialized = false;

        public override void InitializeCorrespondU1Component()
        {
            if (u1Initialized) return;
            componentU1 = (UCameraComponent)gameObject.actor.GetComponentByClass(UCameraComponent.StaticClass());
            sceneCaptureComponent2D =
                (USceneCaptureComponent2D)gameObject.actor.AddComponentByClass(USceneCaptureComponent2D.StaticClass(),
                    false, FTransform.Identity, false);
            u1Initialized = true;
        }
        
        private static Camera main_;

        /// <summary>
        /// The minimum allowed aperture.
        /// </summary>
        public const float kMinAperture = 0.7f;

        /// <summary>
        /// The maximum allowed aperture.
        /// </summary>
        public const float kMaxAperture = 32f;

        /// <summary>
        /// The minimum blade count for the aperture diaphragm.
        /// </summary>
        public const int kMinBladeCount = 3;

        /// <summary>
        /// The maximum blade count for the aperture diaphragm.
        /// </summary>
        public const int kMaxBladeCount = 11;

        [NativeProperty("Near")] public float nearClipPlane { get; set; } = 0.1f;
        [NativeProperty("Far")] public float farClipPlane { get; set; } = 100;

        [NativeProperty("VerticalFieldOfView")]
        public float fieldOfView { get; set; } = 50;

        extern public RenderingPath renderingPath { get; set; }
        extern public RenderingPath actualRenderingPath { [NativeName("CalculateRenderingPath")] get; }

        extern public void Reset();

        public bool allowHDR 
        {
            get
            {
                return UKismetSystemLibrary.GetConsoleVariableIntValue("r.HDR.EnableHDROutput") == 1;
            }
            set
            {
                UGameUserSettings gameSettings = UGameUserSettings.GetGameUserSettings();
                if (value && !gameSettings.SupportsHDRDisplayOutput())
                {
                    Debug.LogWarning("HDR not supported on this device");
                    return;
                }
                int displayNits = gameSettings.GetCurrentHDRDisplayNits();
                gameSettings.EnableHDRDisplayOutput(value, displayNits);

                UKismetSystemLibrary.ExecuteConsoleCommand(
                    Unreal.GWorld, 
                    $"r.HDR.EnableHDROutput {(value ? 1 : 0)}"
                );
                UKismetSystemLibrary.ExecuteConsoleCommand(
                    Unreal.GWorld,
                    $"r.HDR.EnableHDROutput {(value ? 1 : 0)} -priority SetByGameSetting"
                );

                gameSettings.ApplySettings(true);
            }
        }
        public bool allowMSAA 
        {
            get
            {
                return UKismetSystemLibrary.GetConsoleVariableIntValue("r.AntiAliasingMethod") == 3;
            }
            set 
            { 
                if (value)
                {
                    UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld, 
                        "r.AntiAliasingMethod 3");
                    UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld, 
                        "r.MSAACount 4");
                }
                else
                {
                    UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld, 
                        "r.AntiAliasingMethod 0");
                    UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld,
                        "r.MSAACount 1");
                }

                UGameUserSettings gameSettings = UGameUserSettings.GetGameUserSettings();
                gameSettings.SetAntiAliasingQuality(value ? 3 : 0);
                gameSettings.ApplySettings(false);
            }
        }

        public bool allowDynamicResolution 
        {
            get
            {
                return  UKismetSystemLibrary.GetConsoleVariableIntValue("r.DynamicRes.OperationMode") == 1;
            }
            set 
            { 
                UGameUserSettings gameSettings = UGameUserSettings.GetGameUserSettings();
                gameSettings.SetDynamicResolutionEnabled(value);

                UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld, 
                    $"r.DynamicRes.OperationMode {(value ? 1 : 0)}");
                UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld, 
                    $"r.DynamicResolution {(value ? 1 : 0)}");
                gameSettings.ApplySettings(true);
            }
        }

        [NativeProperty("ForceIntoRT")] extern public bool forceIntoRenderTexture { get; set; }

        public float orthographicSize
        {
            get { return componentU1.OrthoWidth / componentU1.AspectRatio / 2; }
            set { componentU1.OrthoWidth = value * componentU1.AspectRatio * 2; }
        }

        public bool orthographic
        {
            get
            {
                if (componentU1 == null)
                {
                    InitializeCorrespondU1Component();
                }
                return componentU1.ProjectionMode == ECameraProjectionMode.Orthographic;
            }
            set
            {
                componentU1.ProjectionMode =
                    value ? ECameraProjectionMode.Orthographic : ECameraProjectionMode.Perspective;
                ;
            }
        }

        extern public OpaqueSortMode opaqueSortMode { get; set; }
        extern public TransparencySortMode transparencySortMode { get; set; }
        extern public Vector3 transparencySortAxis { get; set; }
        extern public void ResetTransparencySortSettings();

        extern public float depth { get; set; }
        public float aspect {
            get
            {
                return componentU1.AspectRatio;
            }
            set
            {
                componentU1.AspectRatio = value;
            } } 
        extern public void ResetAspect();

        extern public Vector3 velocity { get; }
        
        public int cullingMask = ~0;
        extern public int eventMask { get; set; }
        extern public bool layerCullSpherical { get; set; }
        extern public CameraType cameraType { get; set; }

        extern internal Material skyboxMaterial { get; }

        [NativeConditional("UNITY_EDITOR")]
        extern public ulong  overrideSceneCullingMask     { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        extern internal ulong sceneCullingMask { get; }

        [FreeFunction("CameraScripting::GetLayerCullDistances", HasExplicitThis = true)] extern private float[] GetLayerCullDistances();
        [FreeFunction("CameraScripting::SetLayerCullDistances", HasExplicitThis = true)] extern private void SetLayerCullDistances([NotNull] float[] d);
        public float[] layerCullDistances
        {
            get { return GetLayerCullDistances(); }
            set
            {
                if (value.Length != 32)
                    throw new UnityException("Array needs to contain exactly 32 floats for layerCullDistances.");
                SetLayerCullDistances(value);
            }
        }

        [Obsolete("PreviewCullingLayer is obsolete. Use scene culling masks instead.", false)]
        internal static int PreviewCullingLayer { get { return 31; } } // Return 31 because this used to be the PreviewCullingLayer stored in kPreviewLayer in Camera.h

        public bool useOcclusionCulling 
        {
            get
            {
                return  UKismetSystemLibrary.GetConsoleVariableIntValue("r.AllowOcclusionQueries") != 0 ;
            }
            set 
            {
                UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld, 
                    $"r.AllowOcclusionQueries {(value ? 1 : 0)}");
                UKismetSystemLibrary.ExecuteConsoleCommand(Unreal.GWorld, 
                    $"r.occlusion {(value ? 1 : 0)}");
            }
        }

        extern public Matrix4x4 cullingMatrix { get; set; }
        extern public void ResetCullingMatrix();

        extern public Color backgroundColor { get; set; }
        extern public CameraClearFlags clearFlags { get; set; }

        extern public DepthTextureMode depthTextureMode { get; set; }
        extern public bool clearStencilAfterLightingPass { get; set; }

        extern public void SetReplacementShader(Shader shader, string replacementTag);
        extern public void ResetReplacementShader();

        internal enum ProjectionMatrixMode{ Explicit, Implicit, PhysicalPropertiesBased }

        extern internal ProjectionMatrixMode projectionMatrixMode { get; }

        public enum GateFitMode{ Vertical = 1 , Horizontal = 2, Fill = 3, Overscan = 4, None = 0 }
        public bool usePhysicalProperties { get; set; } = false;


        extern public int iso { get; set; }
        extern public float shutterSpeed { get; set; }
        extern public float aperture { get; set; }
        extern public float focusDistance { get; set; }
        extern public float focalLength { get; set; }
        extern public int bladeCount { get; set; }
        extern public Vector2 curvature { get; set; }
        extern public float barrelClipping { get; set; }
        extern public float anamorphism { get; set; }
        public Vector2 sensorSize { get; set; } = new Vector2(36, 24);
        public Vector2 lensShift { get; set; } = new Vector2(0, 0);
        public GateFitMode gateFit { get; set; } = GateFitMode.Horizontal;

        public enum FieldOfViewAxis { Vertical, Horizontal }
        extern public float GetGateFittedFieldOfView();
        extern public Vector2 GetGateFittedLensShift();

        extern internal Vector3 GetLocalSpaceAim();
        [NativeProperty("NormalizedViewportRect")] extern public Rect rect      { get; set; }
        [NativeProperty("ScreenViewportRect")]     extern public Rect pixelRect { get; set; }

        extern public int pixelWidth  {[FreeFunction("CameraScripting::GetPixelWidth",  HasExplicitThis = true)] get; }
        extern public int pixelHeight {[FreeFunction("CameraScripting::GetPixelHeight", HasExplicitThis = true)] get; }

        extern public int scaledPixelWidth  {[FreeFunction("CameraScripting::GetScaledPixelWidth",  HasExplicitThis = true)] get; }
        extern public int scaledPixelHeight {[FreeFunction("CameraScripting::GetScaledPixelHeight", HasExplicitThis = true)] get; }

        extern public RenderTexture targetTexture { get; set; }
        extern public RenderTexture activeTexture {[NativeName("GetCurrentTargetTexture")] get; }
        extern public int targetDisplay { get; set; }

        [FreeFunction("CameraScripting::SetTargetBuffers",  HasExplicitThis = true)] extern private void SetTargetBuffersImpl(RenderBuffer color, RenderBuffer depth);
        public void SetTargetBuffers(RenderBuffer colorBuffer, RenderBuffer depthBuffer) { SetTargetBuffersImpl(colorBuffer, depthBuffer); }

        [FreeFunction("CameraScripting::SetTargetBuffers",  HasExplicitThis = true)] extern private void SetTargetBuffersMRTImpl(RenderBuffer[] color, RenderBuffer depth);
        public void SetTargetBuffers(RenderBuffer[] colorBuffer, RenderBuffer depthBuffer) { SetTargetBuffersMRTImpl(colorBuffer, depthBuffer); }

        extern internal string[] GetCameraBufferWarnings();


        extern public Matrix4x4 cameraToWorldMatrix { get; }
        extern public Matrix4x4 worldToCameraMatrix { get; set; }

        public Matrix4x4 projectionMatrix
        {
            get
            {
                if (orthographic)
                {
                    return CreateOrthographicMatrix(orthographicSize, aspect, nearClipPlane, farClipPlane);
                }

                return CreatePerspectiveMatrix(fieldOfView, aspect, nearClipPlane, farClipPlane);
            }
        }
        public static Matrix4x4 CreatePerspectiveMatrix(float fieldOfView, float aspectRatio, float nearClipPlane, float farClipPlane)
        {
            float fovRadians = Mathf.Deg2Rad * fieldOfView;
            float tanHalfFov = Mathf.Tan(fovRadians / 2);
    
            Matrix4x4 matrix = Matrix4x4.zero;
    
            // 填充透视投影矩阵
            matrix[0, 0] = 1.0f / (aspectRatio * tanHalfFov);
            matrix[1, 1] = 1.0f / tanHalfFov;
            matrix[2, 2] = -(farClipPlane + nearClipPlane) / (farClipPlane - nearClipPlane);
            matrix[2, 3] = -2.0f * farClipPlane * nearClipPlane / (farClipPlane - nearClipPlane);
            matrix[3, 2] = -1.0f;
    
            return matrix;
        }
        public static Matrix4x4 CreateOrthographicMatrix(float size, float aspectRatio, float nearClipPlane, float farClipPlane)
        {
            float halfHeight = size;
            float halfWidth = size * aspectRatio;
    
            Matrix4x4 matrix = Matrix4x4.zero;
    
            // 填充正交投影矩阵
            matrix[0, 0] = 1.0f / halfWidth;
            matrix[1, 1] = 1.0f / halfHeight;
            matrix[2, 2] = -2.0f / (farClipPlane - nearClipPlane);
            matrix[2, 3] = -(farClipPlane + nearClipPlane) / (farClipPlane - nearClipPlane);
            matrix[3, 3] = 1.0f;
    
            return matrix;
        }
        extern public Matrix4x4 nonJitteredProjectionMatrix { get; set; }
        [NativeProperty("UseJitteredProjectionMatrixForTransparent")] extern public bool useJitteredProjectionMatrixForTransparentRendering { get; set; }
        extern public Matrix4x4 previousViewProjectionMatrix { get; }
        extern public void ResetWorldToCameraMatrix();
        extern public void ResetProjectionMatrix();

        [FreeFunction("CameraScripting::CalculateObliqueMatrix", HasExplicitThis = true)] extern public Matrix4x4 CalculateObliqueMatrix(Vector4 clipPlane);
        public Vector3 WorldToScreenPoint(Vector3 position, MonoOrStereoscopicEye eye)
        {
            if (main == null)
            {
                Debug.LogError("Main camera is not initialized.");
                return Vector3.zero;
            }
            // 1. 世界坐标 → 视图坐标
            Vector3 viewPoint = main_.transform.InverseTransformPoint(position);    
            // 2. 视图坐标 → 裁剪坐标
            Vector4 clipPoint = main_.projectionMatrix * new Vector4(viewPoint.x, viewPoint.y, viewPoint.z, 1);
            // 3. 裁剪坐标 → NDC坐标
            Vector3 ndcPoint = new Vector3(
                clipPoint.x / clipPoint.w,
                clipPoint.y / clipPoint.w,
                clipPoint.z / clipPoint.w
            );
            // 4. NDC坐标 → 屏幕坐标
            Vector3 screenPoint = new Vector3(
                (ndcPoint.x + 1) * 0.5f * Screen.width,
                (ndcPoint.y + 1) * 0.5f * Screen.height,
                ndcPoint.z // Z值表示深度，可用于判断前后顺序
            );
            // 处理相机后方的点（Z为负值）
            if (viewPoint.z < 0)
            {
                // 相机后方的点可能会被镜像到屏幕上，需特殊处理
                screenPoint.z = -1; // 标记为相机后方
            }
            
            return screenPoint;
        }
        extern public Vector3 WorldToViewportPoint(Vector3 position, MonoOrStereoscopicEye eye);
        extern public Vector3 ViewportToWorldPoint(Vector3 position, MonoOrStereoscopicEye eye);
        extern public Vector3 ScreenToWorldPoint(Vector3 position, MonoOrStereoscopicEye eye);
        public Vector3 WorldToScreenPoint(Vector3 position) { return WorldToScreenPoint(position, MonoOrStereoscopicEye.Mono); }
        public Vector3 WorldToViewportPoint(Vector3 position) { return WorldToViewportPoint(position, MonoOrStereoscopicEye.Mono); }
        public Vector3 ViewportToWorldPoint(Vector3 position) { return ViewportToWorldPoint(position, MonoOrStereoscopicEye.Mono); }
        public Vector3 ScreenToWorldPoint(Vector3 position) { return ScreenToWorldPoint(position, MonoOrStereoscopicEye.Mono); }
        extern public Vector3 ScreenToViewportPoint(Vector3 position);

        extern public Vector3 ViewportToScreenPoint(Vector3 position);

        extern internal Vector2 GetFrustumPlaneSizeAt(float distance);

        extern private Ray ViewportPointToRay(Vector2 pos, MonoOrStereoscopicEye eye);
        public Ray ViewportPointToRay(Vector3 pos, MonoOrStereoscopicEye eye) { return ViewportPointToRay((Vector2)pos, eye); }
        public Ray ViewportPointToRay(Vector3 pos) { return ViewportPointToRay(pos, MonoOrStereoscopicEye.Mono); }

        private Ray ScreenPointToRay(Vector2 pos, MonoOrStereoscopicEye eye)
        {
            UWorld world = Unreal.GWorld;
            if (world == null) 
                return default;
    
            APlayerController playerController = UGameplayStatics.GetPlayerController(world, 0);
            if (playerController == null) 
                return default;
            
            float unrealY = Screen.height - pos.y;
            
    
            FVector worldLocation = FVector.ZeroVector;
            FVector worldDirection = FVector.ZeroVector;
    
            playerController.DeprojectScreenPositionToWorld(
                pos.x, 
                unrealY,
                ref worldLocation,
                ref worldDirection
            );
            return new Ray(
                U3VectorUtil.GetU3PositionFromU1(worldLocation),
                U3VectorUtil.GetU3DirectionFromU1(worldDirection) 
            );
        }
        
        public Ray ScreenPointToRay(Vector3 pos, MonoOrStereoscopicEye eye) { return ScreenPointToRay((Vector2)pos, eye); }
        public Ray ScreenPointToRay(Vector3 pos) { return ScreenPointToRay(pos, MonoOrStereoscopicEye.Mono); }

        [FreeFunction("CameraScripting::CalculateViewportRayVectors", HasExplicitThis = true)]
        extern private void CalculateFrustumCornersInternal(Rect viewport, float z, MonoOrStereoscopicEye eye, [Out] Vector3[] outCorners);

        public void CalculateFrustumCorners(Rect viewport, float z, MonoOrStereoscopicEye eye, Vector3[] outCorners)
        {
            if (outCorners == null)     throw new ArgumentNullException("outCorners");
            if (outCorners.Length < 4)  throw new ArgumentException("outCorners minimum size is 4", "outCorners");
            CalculateFrustumCornersInternal(viewport, z, eye, outCorners);
        }

        public struct GateFitParameters
        {
            public GateFitMode mode {get; set; }
            public float aspect {get; set; }

            public GateFitParameters(GateFitMode mode, float aspect)
            {
                this.mode = mode;
                this.aspect = aspect;
            }
        }

        [NativeName("CalculateProjectionMatrixFromPhysicalProperties")]
        extern private static void CalculateProjectionMatrixFromPhysicalPropertiesInternal(out Matrix4x4 output, float focalLength, Vector2 sensorSize, Vector2 lensShift, float nearClip, float farClip, float gateAspect, GateFitMode gateFitMode);

        public static void CalculateProjectionMatrixFromPhysicalProperties(out Matrix4x4 output, float focalLength, Vector2 sensorSize, Vector2 lensShift, float nearClip, float farClip, GateFitParameters gateFitParameters = default(GateFitParameters))
        {
            CalculateProjectionMatrixFromPhysicalPropertiesInternal(out output, focalLength, sensorSize, lensShift, nearClip, farClip, gateFitParameters.aspect, gateFitParameters.mode);
        }

        [NativeName("FocalLengthToFieldOfView_Safe")]
        extern public static float FocalLengthToFieldOfView(float focalLength, float sensorSize);

        [NativeName("FieldOfViewToFocalLength_Safe")]
        extern public static float FieldOfViewToFocalLength(float fieldOfView, float sensorSize);

        [NativeName("HorizontalToVerticalFieldOfView_Safe")]
        extern public static float HorizontalToVerticalFieldOfView(float horizontalFieldOfView, float aspectRatio);
        extern public static float VerticalToHorizontalFieldOfView(float verticalFieldOfView, float aspectRatio);

        public static Camera main
        {
            get
            {
                if (main_ == null || !main_.ueCamera.IsValid())
                {
                    GameObject mainCameraGO = GameObject.FindWithTag("MainCamera");
                    if (mainCameraGO != null) {
                        main_ = mainCameraGO.GetComponent<Camera>();
                        AActor cameraActor = UGameplayStatics.GetPlayerController(Unreal.GWorld, 0).PlayerCameraManager
                            .ViewTarget.Target;
                        if (cameraActor != null)
                        {
                            main_.ueCamera = (UCameraComponent)cameraActor.GetComponentByClass(UCameraComponent.StaticClass());
                        }
                    } else {
                        main_ = null;
                    }
                    // main_ = new Camera();
                }
                return main_;
            }
        }

        extern public static Camera current {[FreeFunction("GetCurrentCameraPPtr")] get; }

        extern public UnityEngine.SceneManagement.Scene scene
        {
            [FreeFunction("CameraScripting::GetScene", HasExplicitThis = true)] get;
            [FreeFunction("CameraScripting::SetScene", HasExplicitThis = true)] set;
        }


        public enum StereoscopicEye { Left, Right }
        public enum MonoOrStereoscopicEye { Left, Right, Mono }

        extern public bool stereoEnabled
        {
            [NativeMethod("GetStereoEnabledForBuiltInOrSRP")]
            get;
        }
        extern public float stereoSeparation  { get; set; }
        extern public float stereoConvergence { get; set; }
        extern public bool  areVRStereoViewMatricesWithinSingleCullTolerance {[NativeName("AreVRStereoViewMatricesWithinSingleCullTolerance")] get; }
        extern public StereoTargetEyeMask stereoTargetEye { get; set; }
        extern public MonoOrStereoscopicEye stereoActiveEye {[FreeFunction("CameraScripting::GetStereoActiveEye", HasExplicitThis = true)] get; }

        extern public Matrix4x4 GetStereoNonJitteredProjectionMatrix(StereoscopicEye eye);

        [FreeFunction("CameraScripting::GetStereoViewMatrix", HasExplicitThis = true)]
        extern public Matrix4x4 GetStereoViewMatrix(StereoscopicEye eye);
        extern public void CopyStereoDeviceProjectionMatrixToNonJittered(StereoscopicEye eye);

        [FreeFunction("CameraScripting::GetStereoProjectionMatrix", HasExplicitThis = true)]
        extern public Matrix4x4 GetStereoProjectionMatrix(StereoscopicEye eye);
        extern public void SetStereoProjectionMatrix(StereoscopicEye eye, Matrix4x4 matrix);
        extern public void ResetStereoProjectionMatrices();

        extern public void SetStereoViewMatrix(StereoscopicEye eye, Matrix4x4 matrix);
        extern public void ResetStereoViewMatrices();


        [FreeFunction("CameraScripting::GetAllCamerasCount")] extern private static int GetAllCamerasCount();
        [FreeFunction("CameraScripting::GetAllCameras")] extern private static int GetAllCamerasImpl([Out][NotNull] Camera[] cam);

        public static int allCamerasCount { get { return GetAllCamerasCount(); } }
        public static Camera[] allCameras
        {
            get { Camera[] cam = new Camera[allCamerasCount]; GetAllCamerasImpl(cam); return cam; }
        }
        public static int GetAllCameras(Camera[] cameras)
        {
            if (cameras == null)
                throw new NullReferenceException();

            if (cameras.Length < allCamerasCount)
                throw new ArgumentException("Passed in array to fill with cameras is to small to hold the number of cameras. Use Camera.allCamerasCount to get the needed size.");
            return GetAllCamerasImpl(cameras);
        }

        [FreeFunction("CameraScripting::RenderToCubemap", HasExplicitThis = true)] extern private bool RenderToCubemapImpl(Texture tex, [uei.DefaultValue("63")] int faceMask);
        public bool RenderToCubemap(Cubemap cubemap, int faceMask)          { return RenderToCubemapImpl(cubemap, faceMask); }
        public bool RenderToCubemap(Cubemap cubemap)                        { return RenderToCubemapImpl(cubemap, 63); }
        public bool RenderToCubemap(RenderTexture cubemap, int faceMask)    { return RenderToCubemapImpl(cubemap, faceMask); }
        public bool RenderToCubemap(RenderTexture cubemap)                  { return RenderToCubemapImpl(cubemap, 63); }

        public enum SceneViewFilterMode
        {
            Off = 0,
            ShowFiltered = 1
        }

        [NativeConditional("UNITY_EDITOR")]
        extern private int GetFilterMode();

        [NativeConditional("UNITY_EDITOR")]
        public SceneViewFilterMode sceneViewFilterMode
        {
            get
            {
                return (SceneViewFilterMode)GetFilterMode();
            }
        }


        // TODO: it should be collapsed with others
        [NativeName("RenderToCubemap")] extern private bool RenderToCubemapEyeImpl(RenderTexture cubemap, int faceMask, MonoOrStereoscopicEye stereoEye);
        public bool RenderToCubemap(RenderTexture cubemap, int faceMask, MonoOrStereoscopicEye stereoEye)
        {
            return RenderToCubemapEyeImpl(cubemap, faceMask, stereoEye);
        }

        [Obsolete("The RenderRequest struct is obsolete, use the function overload with RequestData of supported types such as RenderPipeline.StandardRequest", true)]
        public enum RenderRequestMode
        {
            None = 0,
            ObjectId = 1,
            Depth = 2,
            VertexNormal = 3,
            WorldPosition = 4,
            EntityId = 5,
            BaseColor = 6,
            SpecularColor = 7,
            Metallic = 8,
            Emission = 9,
            Normal = 10,
            Smoothness = 11,
            Occlusion = 12,
            DiffuseColor = 13
        }

        [Obsolete("The RenderRequest struct is obsolete, use the function overload with RequestData of supported types such as RenderPipeline.StandardRequest", true)]
        public enum RenderRequestOutputSpace
        {
            ScreenSpace = -1,
            UV0 = 0,
            UV1 = 1,
            UV2 = 2,
            UV3 = 3,
            UV4 = 4,
            UV5 = 5,
            UV6 = 6,
            UV7 = 7,
            UV8 = 8,
        }

        [Obsolete("The RenderRequest struct is obsolete, use the function overload with RequestData of supported types such as RenderPipeline.StandardRequest", true)]
        public struct RenderRequest
        {
            readonly RenderRequestMode m_CameraRenderMode;
            readonly RenderTexture m_ResultRT;

            private readonly RenderRequestOutputSpace m_OutputSpace;

            public RenderRequest(RenderRequestMode mode, RenderTexture rt)
            {
                m_CameraRenderMode = mode;
                m_ResultRT = rt;
                m_OutputSpace = RenderRequestOutputSpace.ScreenSpace;
            }

            public RenderRequest(RenderRequestMode mode, RenderRequestOutputSpace space, RenderTexture rt)
            {
                m_CameraRenderMode = mode;
                m_ResultRT = rt;
                m_OutputSpace = space;
            }

            public bool isValid => m_CameraRenderMode != 0 && m_ResultRT != null;

            public RenderRequestMode mode => m_CameraRenderMode;
            public RenderTexture result => m_ResultRT;
            public RenderRequestOutputSpace outputSpace => m_OutputSpace;
        }

        [FreeFunction("CameraScripting::Render", HasExplicitThis = true)]            extern public void Render();
        [FreeFunction("CameraScripting::RenderWithShader", HasExplicitThis = true)]  extern public void RenderWithShader(Shader shader, string replacementTag);
        [FreeFunction("CameraScripting::RenderDontRestore", HasExplicitThis = true)] extern public void RenderDontRestore();

        [Obsolete("SubmitRenderRequests is obsolete, use SubmitRenderRequest with RequestData of supported types such as RenderPipeline.StandardRequest", true)]
        public void SubmitRenderRequests(List<RenderRequest> renderRequests)
        {
            if (renderRequests == null || renderRequests.Count == 0)
                throw new ArgumentException($"{nameof(SubmitRenderRequests)} has been invoked with invalid renderRequests");

            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.LogWarning("Trying to invoke 'SubmitRenderRequests' when no SRP is set. A scriptable render pipeline is needed for this function call");
                return;
            }
            SubmitRenderRequestsInternal(renderRequests);
        }

        public void SubmitRenderRequest<RequestData>(RequestData renderRequest)
        {
            if (renderRequest == null)
                throw new ArgumentException($"{nameof(SubmitRenderRequests)} is invoked with invalid renderRequests");

            if (renderRequest is ObjectIdRequest objectIdRequest)
            {
                if (objectIdRequest.destination.depthStencilFormat == Experimental.Rendering.GraphicsFormat.None)
                {
                    Debug.LogWarning("ObjectId Render Request submitted without a depth stencil, which can produce results that are not depth tested correctly");
                }
                if (GraphicsSettings.currentRenderPipeline == null || !RenderPipelineManager.currentPipeline.IsRenderRequestSupported(this, objectIdRequest))
                {
                    // If the render pipeline supports object id rendering let the pipeline handle it.
                    // Otherwise rely on the built-in support through "magic", which also works with both hdrp/urp shaders
                    HandleBuiltInObjectIDRenderRequest(objectIdRequest);
                    return;
                }
            }
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.LogWarning("Trying to invoke 'SubmitRenderRequest' when no SRP is set. A scriptable render pipeline is needed for this function call");
                return;
            }
            SubmitRenderRequestsInternal(renderRequest);
        }

        void HandleBuiltInObjectIDRenderRequest(ObjectIdRequest renderRequest)
        {
            UnityEngine.Object[] objects;
            objects = SubmitBuiltInObjectIDRenderRequest(
                renderRequest.destination,
                renderRequest.mipLevel,
                renderRequest.face,
                renderRequest.slice);
            renderRequest.result = new ObjectIdResult(objects);
        }

        [FreeFunction("CameraScripting::SubmitRenderRequests", HasExplicitThis = true)]  extern private void SubmitRenderRequestsInternal(object requests);
        [FreeFunction("CameraScripting::SubmitBuiltInObjectIDRenderRequest", HasExplicitThis = true)] [NativeConditional("UNITY_EDITOR")]
        extern private UnityEngine.Object[] SubmitBuiltInObjectIDRenderRequest(
            RenderTexture target,
            int mipLevel,
            CubemapFace cubemapFace,
            int depthSlice);
        [FreeFunction("CameraScripting::SetupCurrent")] extern public static void SetupCurrent(Camera cur);
        [FreeFunction("CameraScripting::CopyFrom", HasExplicitThis = true)] extern public void CopyFrom(Camera other);

        extern public int  commandBufferCount { get; }
        extern public void RemoveCommandBuffers(CameraEvent evt);
        extern public void RemoveAllCommandBuffers();

        // in old bindings these functions code like this:
        //   self->AddCommandBuffer(evt, &*buffer);
        // this dereference generated null-ref exception (as opposed to "normal" argument-null exception)
        // we want to preserve this behaviour

        // extern public void AddCommandBuffer(CameraEvent evt, [NotNull] CommandBuffer buffer);
        // extern public void RemoveCommandBuffer(CameraEvent evt, [NotNull] CommandBuffer buffer);
        [NativeName("AddCommandBuffer")]      extern private void AddCommandBufferImpl(CameraEvent evt, [NotNull] CommandBuffer buffer);
        [NativeName("AddCommandBufferAsync")] extern private void AddCommandBufferAsyncImpl(CameraEvent evt, [NotNull] CommandBuffer buffer, ComputeQueueType queueType);
        [NativeName("RemoveCommandBuffer")]   extern private void RemoveCommandBufferImpl(CameraEvent evt, [NotNull] CommandBuffer buffer);

        public void AddCommandBuffer(CameraEvent evt, CommandBuffer buffer)
        {
            if (!Rendering.CameraEventUtils.IsValid(evt))
                throw new ArgumentException(string.Format(@"Invalid CameraEvent value ""{0}"".", (int)evt), "evt");
            if (buffer == null) throw new NullReferenceException("buffer is null");
            AddCommandBufferImpl(evt, buffer);
        }

        public void AddCommandBufferAsync(CameraEvent evt, CommandBuffer buffer, ComputeQueueType queueType)
        {
            if (!Rendering.CameraEventUtils.IsValid(evt))
                throw new ArgumentException(string.Format(@"Invalid CameraEvent value ""{0}"".", (int)evt), "evt");
            if (buffer == null) throw new NullReferenceException("buffer is null");
            AddCommandBufferAsyncImpl(evt, buffer, queueType);
        }

        public void RemoveCommandBuffer(CameraEvent evt, CommandBuffer buffer)
        {
            if (!Rendering.CameraEventUtils.IsValid(evt))
                throw new ArgumentException(string.Format(@"Invalid CameraEvent value ""{0}"".", (int)evt), "evt");
            if (buffer == null) throw new NullReferenceException("buffer is null");
            RemoveCommandBufferImpl(evt, buffer);
        }

        [FreeFunction("CameraScripting::GetCommandBuffers", HasExplicitThis = true)]
        extern public UnityEngine.Rendering.CommandBuffer[] GetCommandBuffers(UnityEngine.Rendering.CameraEvent evt);

    
        // called before a camera culls the scene.
        // void OnPreCull();

        // called before a camera starts rendering the scene.
        // void OnPreRender();

        // called after a camera has finished rendering the scene.
        // void OnPostRender();

        // called after all rendering is complete to render image
        // void OnRenderImage(RenderTexture source, RenderTexture destination);

        // called after camera has rendered the scene.
        // void OnRenderObject();

        // called once for each camera if the object is visible.
        // void OnWillRenderObject();

        public delegate void CameraCallback(Camera cam);

        public static CameraCallback onPreCull;
        public static CameraCallback onPreRender;
        public static CameraCallback onPostRender;

        [RequiredByNativeCode]
        private static void FireOnPreCull(Camera cam)
        {
            if (onPreCull != null)
                onPreCull(cam);
        }

        [RequiredByNativeCode]
        private static void FireOnPreRender(Camera cam)
        {
            if (onPreRender != null)
                onPreRender(cam);
        }

        [RequiredByNativeCode]
        private static void FireOnPostRender(Camera cam)
        {
            if (onPostRender != null)
                onPostRender(cam);
        }

        // These two empty internal methods (which will always be stripped) are required to make the EmptyBuildGotStrippedEnough test work.
        internal void OnlyUsedForTesting1()
        {
        }

        internal void OnlyUsedForTesting2()
        {
        }

        public unsafe bool TryGetCullingParameters(out Rendering.ScriptableCullingParameters cullingParameters)
        {
            return GetCullingParameters_Internal(this, false, out cullingParameters, sizeof(Rendering.ScriptableCullingParameters));
        }

        public unsafe bool TryGetCullingParameters(bool stereoAware, out Rendering.ScriptableCullingParameters cullingParameters)
        {
            return GetCullingParameters_Internal(this, stereoAware, out cullingParameters, sizeof(Rendering.ScriptableCullingParameters));
        }

        [NativeHeader("Runtime/Export/RenderPipeline/ScriptableRenderPipeline.bindings.h")]
        [FreeFunction("ScriptableRenderPipeline_Bindings::GetCullingParameters_Internal")]
        extern private static bool GetCullingParameters_Internal(Camera camera, bool stereoAware, out Rendering.ScriptableCullingParameters cullingParameters, int managedCullingParametersSize);
    }
}
