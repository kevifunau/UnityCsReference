using System.Collections.Generic;
using JetBrains.Annotations;
using Script.UnrealCSharp;
using Script.CoreUObject;
using Script.Dynamic;
using Script.Engine;
using UnityEngine;
using System.Reflection;

namespace Script.DynamicCodeGen;

/**
 * Mimics Component behavior for generated IL classes.
 * To add new methods or modify existing functionality,
 * make changes in this class.
 */

[UClass]
[BlueprintSpawnableComponent]
public partial class U3ComponentU3_C : UGUSDBaseComponent
{
    // Initialize within the IL dynamic class
    public Component proxy;
    
    protected Dictionary<string, MethodInfo> InvokeMethodMap = new ();

    public U3ComponentU3_C(Component proxy)
    {
        this.proxy = proxy;
        this.proxy.u1Component = this;
        PrimaryComponentTick.bCanEverTick = true;
    }

    public void LoadAddProxy(Component proxy)
    {
        this.proxy = proxy;
        this.proxy.u1Component = this;
        PrimaryComponentTick.bCanEverTick = true;
        proxy.gameObject = GameObject.GetFromActorOrCreate(GetOwner());
    }
    
    [Override]
    public override void SetActive(bool bNewActive, bool bReset = false)
    {
        base.SetActive(bNewActive, bReset);
        SetComponentTickEnabled(bNewActive);
        SetVisibility(bNewActive);
        Utils.InvokeMethod(proxy, bNewActive ? "OnEnable" : "OnDisable");
    }

    [Override]
    public override void ReceiveBeginPlay()
    {
        base.ReceiveBeginPlay();
        OnProxyBeforeStart();
        OnProxyStart();
        OnProxyAfterStart();
    }

    protected virtual void OnProxyBeforeStart()
    {
    }

    protected virtual void OnProxyStart()
    {
        Utils.InvokeMethod(proxy, "Start");
    }

    protected virtual void OnProxyAfterStart()
    {
    }

    [Override]
    public override void ReceiveTick(float DeltaSeconds)
    {
        OnProxyBeforeUpdate(DeltaSeconds);
        OnProxyUpdate(DeltaSeconds);
        OnProxyAfterUpdate(DeltaSeconds);
    }

    protected virtual void OnProxyBeforeUpdate(float DeltaSeconds)
    {
    }

    protected virtual void OnProxyUpdate(float DeltaSeconds)
    {
        bool iscallable =  InvokeMethod("Update");
        if (!iscallable)
        {
            SetReceiveTickEnable(iscallable);
        }
    }

    protected virtual void OnProxyAfterUpdate(float DeltaSeconds)
    {
    }

    [Override]
    public override bool IsChildOf(FString ComponentClassName)
    {
        var ty = GetType().Assembly.GetType(ComponentClassName.ToString());

        return ty.IsAssignableFrom(proxy.GetType());
    }

    protected bool InvokeMethod(string u3MethodName, object[] parameters = null)
    {
        InvokeMethodMap.TryGetValue(u3MethodName, out var method);
        if (method != null)
        {
            method.Invoke(proxy, parameters);
            return true;
        }
        var methodInfo = Utils.InvokeMethod(proxy, u3MethodName, parameters);
        if (methodInfo != null)
        {
            InvokeMethodMap.TryAdd(u3MethodName, methodInfo);
            return true;
        }
        return false;
    }
}