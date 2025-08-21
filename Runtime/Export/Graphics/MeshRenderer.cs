// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using GUSD.Utils;
using Script.Dynamic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine;
[U3Exported(false)]
[NativeHeader("Runtime/Graphics/Mesh/MeshRenderer.h")]
public partial class MeshRenderer : UnityEngine.Renderer
{
    [RequiredByNativeCode]  // MeshRenderer is used in the VR Splash screen.
    private void DontStripMeshRenderer() {}

    extern public Mesh additionalVertexStreams { get; set; }
    extern public Mesh enlightenVertexStream { get; set; }
    extern public int subMeshStartIndex {[NativeName("GetSubMeshStartIndex")] get; }
    extern public float scaleInLightmap { get; set; }
    extern public ReceiveGI receiveGI { get; set; }
    extern public bool stitchLightmapSeams { get; set; }
}