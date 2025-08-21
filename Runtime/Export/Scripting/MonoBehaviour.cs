// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using GUSD.Coroutine;
using Script.CoreUObject;
using Script.Dynamic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;
using Object = System.Object;
using uei = UnityEngine.Internal;
using Script.Engine;
using Script.UnrealCSharp;

namespace UnityEngine;

// MonoBehaviour is the base class every script derives from.
[RequiredByNativeCode]
[ExtensionOfNativeClass]
[NativeHeader("Runtime/Mono/MonoBehaviour.h")]
[NativeHeader("Runtime/Scripting/DelayedCallUtility.h")]
public partial class MonoBehaviour : Behaviour
{
     public readonly List<(float time, Action action)> _delayedActions = new();
     public float _timer;
     private double timeStamp = 0;
     
    public CoroutineManager coroutineManager = new ();
    public MonoBehaviour()
    {
        ConstructorCheck(this);
    }

    public void InvokeInstanceFunction(string functionName, Object[] parameters = null)
    {
        Type currentType = this.GetType();
        while (currentType != null)
        {
            MethodInfo method = currentType.GetMethod(functionName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    
            if (method != null)
            {
                method.Invoke(this, parameters);
                return;
            }
    
            currentType = currentType.BaseType;
            if (currentType != null && currentType.Name.Contains("MonoBehaviourU3_C"))
            {
                break;
            }
        }
    }
    
    public void DuringUpdate(float DeltaTime)
    {
        Time.deltaTime = DeltaTime;
        double currentTimeStamp = UGameplayStatics.GetAudioTimeSeconds(Unreal.GWorld);
        Time.frameCount++;
        if (Time.frameCount >= 2)
        {
            Time.unscaledDeltaTime = Convert.ToSingle(currentTimeStamp - timeStamp);
        }

        timeStamp = currentTimeStamp;

        // update Invoke timer
        _timer += DeltaTime;
        
        // handle delay operator��Invoke��
        for (int i = _delayedActions.Count - 1; i >= 0; i--)
        {
            var (time, action) = _delayedActions[i];
            if (_timer >= time)
            {
                action?.Invoke();
                _delayedActions.RemoveAt(i);
            }
        }
    }

    private CancellationTokenSource m_CancellationTokenSource;
    public CancellationToken destroyCancellationToken
    {
        get
        {
            if (this == null)
                throw new MissingReferenceException("DestroyCancellation token should be called atleast once before destroying the monobehaviour object");
            if (m_CancellationTokenSource == null)
            {
                m_CancellationTokenSource = new CancellationTokenSource();
                OnCancellationTokenCreated();
            }
            return m_CancellationTokenSource.Token;
        }
    }

    [RequiredByNativeCode]
    private void RaiseCancellation()
    {
        m_CancellationTokenSource?.Cancel();
    }

    // Is any invoke pending on this MonoBehaviour?
    public bool IsInvoking()
    {
        return Internal_IsInvokingAll(this);
    }

    public void CancelInvoke()
    {
        // _delayedActions.Clear();
        // Internal_CancelInvokeAll(this);
		StopAllCoroutines();
    }

    // Invokes the method /methodName/ in time seconds.
    public void Invoke(string methodName, float time)
    {        
        // _delayedActions.Add((_timer + time, () => InvokeInstanceFunction(methodName)));
        StartCoroutine(InvokeTime(methodName, time), methodName);
    }

    IEnumerator InvokeTime(string methodName,float time, float repeatRate=0.0f)
    {
        yield return new WaitForSeconds(time);
        InvokeInstanceFunction(methodName);
        if (repeatRate > 0)
        {
            while (IsActiveCoroutineManaged(methodName))
            {
                yield return new WaitForSeconds(repeatRate);
                InvokeInstanceFunction(methodName);
            }
        }
    }

    // Invokes the method /methodName/ in /time/ seconds.
    public void InvokeRepeating(string methodName, float time, float repeatRate)
    {
        if (repeatRate <= 0.00001f && repeatRate != 0.0f)
            throw new UnityException("Invoke repeat rate has to be larger than 0.00001F)");

        // InvokeDelayed(this, methodName, time, repeatRate);
		StartCoroutine(InvokeTime(methodName, time, repeatRate), methodName);
    }

    // Cancels all Invoke calls with name /methodName/ on this behaviour.
    public void CancelInvoke(string methodName)
    {
        // CancelInvoke(this, methodName);
		StopCoroutine(methodName);
    }

    // Is any invoke on /methodName/ pending?
    public bool IsInvoking(string methodName)
    {
        return IsInvoking(this, methodName);
    }

    [uei.ExcludeFromDocs]
    public Coroutine StartCoroutine(string methodName)
    {
        object value = null;
        return StartCoroutine(methodName, value);
    }

    // Starts a coroutine named /methodName/.
    public Coroutine StartCoroutine(string methodName, [uei.DefaultValue("null")] object value)
    {
        if (string.IsNullOrEmpty(methodName))
            throw new NullReferenceException("methodName is null or empty");

        if (!IsObjectMonoBehaviour(this))
            throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

        return StartCoroutineManaged(methodName, value);
    }

    // Starts a coroutine.
    public Coroutine StartCoroutine(IEnumerator routine, string methodName=null)
    {
        if (routine == null)
            throw new NullReferenceException("routine is null");

        if (!IsObjectMonoBehaviour(this))
            throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

        return StartCoroutineManaged2(routine, methodName);
    }

    //*undocumented*
    [Obsolete("StartCoroutine_Auto has been deprecated. Use StartCoroutine instead (UnityUpgradable) -> StartCoroutine([mscorlib] System.Collections.IEnumerator)", false)]
    public Coroutine StartCoroutine_Auto(IEnumerator routine, string methodName=null)
    {
        return StartCoroutine(routine, methodName);
    }

    // Stop a coroutine.
    public void StopCoroutine(IEnumerator routine)
    {
        if (routine == null)
            throw new NullReferenceException("routine is null");

        if (!IsObjectMonoBehaviour(this))
            throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

        StopCoroutineFromEnumeratorManaged(routine);
    }

    // Stop a coroutine.
    public void StopCoroutine(Coroutine routine)
    {
        if (routine == null)
            throw new NullReferenceException("routine is null");

        if (!IsObjectMonoBehaviour(this))
            throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

        StopCoroutineManaged(routine);
    }

    // Stops all coroutines named /methodName/ running on this behaviour.
    public void StopCoroutine(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            throw new NullReferenceException("methodName is null or empty");
        
        if (!IsObjectMonoBehaviour(this))
            throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");
        StopCoroutineManaged(methodName);
    }

    // Stops all coroutines running on this behaviour.
    public void StopAllCoroutines()
    {
        coroutineManager.StopAll();
    }

    public extern bool useGUILayout { get; set; }

    // Allow a specific instance of a MonoBehaviour to run in edit mode (only available in the editor)
    public extern bool runInEditMode { get; set; }
    internal extern bool allowPrefabModeInPlayMode { get; }

    // Logs message to the Unity Console. This function is identical to [[Debug.Log]].
    public static void print(object message)
    {
        Debug.Log(message);
    }

    [NativeMethod(IsThreadSafe = true)]
    static void ConstructorCheck([Writable] Object self)
    {
        
    }

    [FreeFunction("CancelInvoke")]
    extern static void Internal_CancelInvokeAll([NotNull("NullExceptionObject")] MonoBehaviour self);

    [FreeFunction("IsInvoking")]
    extern static bool Internal_IsInvokingAll([NotNull("NullExceptionObject")] MonoBehaviour self);

    [FreeFunction]
    extern static void InvokeDelayed([NotNull("NullExceptionObject")] MonoBehaviour self, string methodName, float time, float repeatRate);

    [FreeFunction]
    extern static void CancelInvoke([NotNull("NullExceptionObject")] MonoBehaviour self, string methodName);

    [FreeFunction]
    extern static bool IsInvoking([NotNull("NullExceptionObject")] MonoBehaviour self, string methodName);

    [FreeFunction]
    static bool IsObjectMonoBehaviour([NotNull("NullExceptionObject")] Object obj)
    {
        return obj is MonoBehaviour;
    }

    extern Coroutine StartCoroutineManaged(string methodName, object value);

    Coroutine StartCoroutineManaged2(IEnumerator enumerator, string methodName=null)
    {
        return coroutineManager.Start(enumerator, methodName);
    }

    void StopCoroutineManaged(Coroutine routine)
    {
        coroutineManager.Stop(routine);
    }

    void StopCoroutineFromEnumeratorManaged(IEnumerator ie)
    {
        coroutineManager.Stop(ie);
    }
	void StopCoroutineManaged(string methodName)
    {
        coroutineManager.Stop(methodName);
    }
    
    bool IsActiveCoroutineManaged(string methodName)
    { 
        return coroutineManager.IsActive(methodName);
    }

    extern internal string GetScriptClassName();

    extern void OnCancellationTokenCreated();
}
