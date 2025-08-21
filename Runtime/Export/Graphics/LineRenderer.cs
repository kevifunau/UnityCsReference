using System;
using System.Runtime.InteropServices;
using Script.CoreUObject;
using Script.ProceduralMeshComponent;
using Script.UnrealCSharp;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using Script.Dynamic;

namespace UnityEngine;

[U3Exported]
[RequireComponent(typeof(Transform))]
[NativeHeader("Runtime/Graphics/LineRenderer.h")]
public partial class LineRenderer: Renderer
{
        public int U3PositionsCount;
        public AGUSDLineRenderUtil lineRenderUtil;
        public float startWidth {
            get
            {
                return getLineRenderUtil().StartWidth;
            }
            set
            {
                getLineRenderUtil().SetStartWidth(value * 100.0f);
            }
        }
        public float endWidth {
            get
            {
                return getLineRenderUtil().EndWidth;
            }
            set
            {
                getLineRenderUtil().SetEndWidth(value * 100.0f);
            }
        }
        extern public float widthMultiplier { get; set; }
        extern public int numCornerVertices { get; set; }
        extern public int numCapVertices { get; set; }
        extern public bool useWorldSpace { get; set; }
        extern public bool loop { get; set; }

        extern public Color startColor { get; set; }
        extern public Color endColor { get; set; }

        [NativeProperty("PositionsCount")]
        public int positionCount {
            get
            {
                return U3PositionsCount;
            }
            set
            {
                U3PositionsCount = value;
            }
        }

        public AGUSDLineRenderUtil getLineRenderUtil()
        {
            if (lineRenderUtil == null)
            {
                lineRenderUtil = Unreal.GWorld.SpawnActor<AGUSDLineRenderUtil>(FTransform.Identity);
            }
            return lineRenderUtil;
        }

        public void SetPosition(int index, Vector3 position)
        {
            getLineRenderUtil().SetPosition(index, new FVector(position.x * 100.0f, position.z * 100.0f, position.z * 100.0f));
            
        }

        extern public Vector3 GetPosition(int index);

        extern public Vector2 textureScale      { get; set; }
        extern public float shadowBias          { get; set; }

        extern public bool generateLightingData { get; set; }

        extern public LineTextureMode textureMode { get; set; }
        extern public LineAlignment   alignment   { get; set; }
        extern public SpriteMaskInteraction maskInteraction { get; set; }

        extern public void Simplify(float tolerance);

        public void BakeMesh(Mesh mesh, bool useTransform = false) { BakeMesh(mesh, Camera.main, useTransform); }
        extern public void BakeMesh([NotNull] Mesh mesh, [NotNull] Camera camera, bool useTransform = false);

        public AnimationCurve widthCurve    { get { return GetWidthCurveCopy(); }    set { SetWidthCurve(value); } }
        public Gradient       colorGradient { get { return GetColorGradientCopy(); } set { SetColorGradient(value); } }

        // these are direct glue to TrailRenderer methods to simplify properties code (and have null checks generated)

        extern private AnimationCurve GetWidthCurveCopy();
        extern private void SetWidthCurve([NotNull] AnimationCurve curve);

        extern private Gradient GetColorGradientCopy();
        extern private void SetColorGradient([NotNull] Gradient curve);

        [FreeFunction(Name = "LineRendererScripting::GetPositions", HasExplicitThis = true)]
        extern public int GetPositions([NotNull][Out] Vector3[] positions);

        [FreeFunction(Name = "LineRendererScripting::SetPositions", HasExplicitThis = true)]
        public void SetPositions([NotNull] Vector3[] positions)
        {
            AGUSDLineRenderUtil lineRenderUtil = getLineRenderUtil();
            UProceduralMeshComponent mesh = GameObject.GetChildComponent<UProceduralMeshComponent>(u1Component);
            lineRenderUtil.SetProceduralMeshComponent(mesh);
            if (positions.Length > positionCount)
            {
                Vector3[] tmp = new  Vector3[positionCount];
                for (int i = 0; i < positionCount; i++)
                {
                    tmp[i] = positions[i];
                }
                positions = tmp;
            }

            TArray<FVector> U1Positions = new TArray<FVector>();
            foreach (var position in positions)
            {
                U1Positions.Add(new FVector(position.x * 100.0f, position.z * 100.0f, position.y * 100.0f));
            }
            lineRenderUtil.SetPositions(U1Positions);
        }
        

        public void SetPositions(NativeArray<Vector3> positions) { unsafe { SetPositionsWithNativeContainer((IntPtr)positions.GetUnsafeReadOnlyPtr(), positions.Length); } }
        public void SetPositions(NativeSlice<Vector3> positions) { unsafe { SetPositionsWithNativeContainer((IntPtr)positions.GetUnsafeReadOnlyPtr(), positions.Length); } }

        public int GetPositions([Out] NativeArray<Vector3> positions) { unsafe { return GetPositionsWithNativeContainer((IntPtr)positions.GetUnsafePtr(), positions.Length); } }
        public int GetPositions([Out] NativeSlice<Vector3> positions) { unsafe { return GetPositionsWithNativeContainer((IntPtr)positions.GetUnsafePtr(), positions.Length); } }

        [FreeFunction(Name = "LineRendererScripting::SetPositionsWithNativeContainer", HasExplicitThis = true)]
        extern private void SetPositionsWithNativeContainer(IntPtr positions, int count);

        [FreeFunction(Name = "LineRendererScripting::GetPositionsWithNativeContainer", HasExplicitThis = true)]
        extern private int GetPositionsWithNativeContainer(IntPtr positions, int length);
    
}