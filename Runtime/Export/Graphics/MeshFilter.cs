// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using GUSD.Utils;
using Script.AIModule;
using Script.Dynamic;
using Script.Engine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = System.Object;

namespace UnityEngine;
[U3Exported(false)]
[RequireComponent(typeof(Transform))]
[NativeHeader("Runtime/Graphics/Mesh/MeshFilter.h")]
public sealed partial class MeshFilter : Component
{
    [RequiredByNativeCode]  // MeshFilter is used in the VR Splash screen.
    private void DontStripMeshFilter() {}

    public Mesh sharedMesh
    {
        get
        {
            //这里unity定义的是获取shareMesh，UE改成了把他挂在到actor下面，获取自身的actor，这样就可以用actor上的staticmesh来实现对应的功能
            Mesh mesh = new Mesh();
            //Log("Component: " + name);
            mesh.actor = owner;
            //Log("Component.owner: " + owner);
            return mesh;
        }
        set
        {
                
        }
    }
    extern public Mesh mesh {[NativeName("GetInstantiatedMeshFromScript")] get; [NativeName("SetInstantiatedMesh")] set; }
}
