// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // MonoBehaviour.StartCoroutine returns a Coroutine. Instances of this class are only used to reference these coroutines and do not hold any exposed properties or functions.
    [NativeHeader("Runtime/Mono/Coroutine.h")]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public sealed class Coroutine : YieldInstruction
    {
        public MonoBehaviour m_MonoBehaviour;
        public IEnumerator m_routine;
        public string m_methodName;
        internal IntPtr m_Ptr;
        public Stack<IEnumerator> CallStack { get; } = new Stack<IEnumerator>();
        internal LinkedListNode<Coroutine> ListNode;
        public bool IsDone => ListNode.List == null;
        
        ~Coroutine()
        {
            ReleaseCoroutine(m_Ptr);
        }

        [FreeFunction("Coroutine::CleanupCoroutineGC", true)]
        static void ReleaseCoroutine(IntPtr ptr)
        {
            
        }
    }
}
