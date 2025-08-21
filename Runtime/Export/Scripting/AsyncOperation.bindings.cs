// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Script.UnrealCSharp;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Export/Scripting/AsyncOperation.bindings.h")]
    [NativeHeader("Runtime/Misc/AsyncOperation.h")]
    public partial class AsyncOperation : YieldInstruction
    {
        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("AsyncOperationBindings", StaticAccessorType.DoubleColon)]
        private static extern void InternalDestroy(IntPtr ptr);

        private bool _allowSceneActivation = false;
        
        private float _simulatedProgress = 0f;
        private bool _hasInvokedCompletion = false;
        public bool _isMarkedComplete = false;
        public string loadSceneName = "";
        public void MarkAsFailed()
        {
            _simulatedProgress = 1.0f; // 立即标记为完成
            _isMarkedComplete = true;
            Debug.Log($"[MarkedAsFailed] ID:{sceneId}");
            InvokeCompletionEvent(); // 立即触发完成事件
        }

        public bool isDone 
        {
            get 
            {
                return _isMarkedComplete || _hasInvokedCompletion;
            }
            set
            {
                
            }
        }
        public float progress
        {
            get
            {  
                UpdateProgress();
                return _simulatedProgress;
            }
        }

        public void UpdateProgress()
        {
            if (_isMarkedComplete)
            {
                _simulatedProgress = 1.0f;
                InvokeCompletionEvent();
                return;
            }
            _simulatedProgress = ASceneUtil.UpdateProgress(sceneId, _hasInvokedCompletion, _isMarkedComplete, loadSceneName);
            if (Math.Abs(_simulatedProgress - 1) == 0)
            {
                InvokeCompletionEvent();
            }
        }

        public extern int priority
        {
            [NativeMethod("GetPriority")]
            get;
            [NativeMethod("SetPriority")]
            set;
        }

        public bool allowSceneActivation
        {
            get => _allowSceneActivation; 
            set 
            { 
                _allowSceneActivation = value; 
                if (value) UpdateProgress(); // 允许激活后触发最终进度
            } 
        }
        
        public static bool AreEqual(float a, float b, float epsilon = 1e-5f)
        {
            return Math.Abs(a - b) < epsilon;
        }
        
        public void setID(int value)
        {
            sceneId = value;
            Debug.Log($"Setting scene ID: {value}");
        }
    }
}
