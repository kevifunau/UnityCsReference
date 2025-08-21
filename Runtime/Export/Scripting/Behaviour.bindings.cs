// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Script.Engine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Behaviours are Components that can be enabled or disabled.
    [UsedByNativeCode]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    public class Behaviour : Component
    {
        // Enabled Behaviours are Updated, disabled Behaviours are not.
        [RequiredByNativeCode] // GetFixedBehaviourManager is directly used by fixed update in the player loop
        [NativeProperty]
        public virtual bool enabled
        {
            get
            {
                return u1Component.PrimaryComponentTick.bCanEverTick;
            }
            set
            {
                // gameObject.SetActive(value);
                // u1Component.SetComponentTickEnabled(value);
                // u1Component.SetVisibility(value);
                u1Component.SetActive(value, true);
            }
        }

        [NativeProperty]
        public bool isActiveAndEnabled
        {
            [NativeMethod("IsAddedToManager")]
            get
            {
                return enabled && gameObject.active;
            }
        }
    }
}
