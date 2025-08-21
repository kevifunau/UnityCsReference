using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Script.DynamicCodeGen;
using Script.Engine;
using Script.UnrealCSharp;
using Script.UtuRuntime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Script.CoreUObject;

public class EventHandlerUtility
{
    public static void CreateSingleParamHandler<T>(UWorld world, ref T unityEvent, FUTUEventListener listener)
        where T : UnityEventBase
    {
        var result = CreateSingleParamHandlerInternal(world, ref unityEvent, listener);
        if (!result)
        {
            Debug.LogWarning($"Failed to bind {listener.MethodCompProperty.ComponentType}.{listener.MethodName}({listener.Type.ToString()}) for {unityEvent.GetType().Name}");
        }
    }

    private static bool AddStaticListener<T>(ref T unityEvent, Delegate action, object staticValue = null)
    {
        switch (unityEvent)
        {
            case Toggle.ToggleEvent onValueChanged:
            {
                onValueChanged.AddListener(_=>action.DynamicInvoke(staticValue));
                break;
            }
            case Button.ButtonClickedEvent buttonClickedEvent:
            {
                buttonClickedEvent.AddListener(()=>action.DynamicInvoke(staticValue));
                break;
            }
            case Dropdown.DropdownEvent dropdownEvent:
            {
                dropdownEvent.AddListener(_=>action.DynamicInvoke(staticValue));
                break;
            }
        }

        return true;
    }
    
    private static bool CreateSingleParamHandlerInternal<T>(UWorld world, ref T unityEvent, FUTUEventListener listener)
        where T : UnityEventBase
    {
        if (listener == null || world == null || unityEvent == null) return false;

        switch (listener.Type)
        {
            // 无参数。只需要找到对应的函数即可
            case EPersistentListenerMode.Void:
                var vAction = (UnityAction)GetByPath(world, listener, typeof(UnityAction));
                if (vAction == null) return false;
                if (vAction.Method.IsStatic) return AddStaticListener(ref unityEvent, vAction);
                unityEvent.AddVoidPersistentListener(vAction);
                break;
            // 动态参数。只需要找到对应的函数
            case EPersistentListenerMode.EventDefined:
                switch (unityEvent)
                {
                    case Toggle.ToggleEvent onValueChanged:
                    {
                        var edAction = (UnityAction<bool>)GetByPath(world, listener, typeof(UnityAction<bool>));
                        if (edAction == null) return false;
                        onValueChanged.AddListener(edAction);
                        break;
                    }
                    case Button.ButtonClickedEvent buttonClickedEvent:
                    {
                        var edAction = (UnityAction)GetByPath(world, listener, typeof(UnityAction));
                        if (edAction == null) return false;
                        buttonClickedEvent.AddListener(edAction);
                        break;
                    }
                    case Dropdown.DropdownEvent dropdownEvent:
                    {
                        var edAction = (UnityAction<int>)GetByPath(world, listener, typeof(UnityAction<int>));
                        if (edAction == null) return false;
                        dropdownEvent.AddListener(edAction);
                        break;
                    }
                }

                break;
            // 参数是一个Component或者GO
            case EPersistentListenerMode.Object:
                FString goPath = listener.ParamCompProperty.GOPath;
                if (goPath == null || goPath.ToString() == "") return false;
                TArray<AActor> actors = new TArray<AActor>();
                UGameplayStatics.GetAllActorsWithTag(world, "U3Path_" + goPath, ref actors);
                if (actors == null || actors.Num() != 1) return false;
                AActor actor = actors[0];
                GameObject go = GameObject.GetFromActorOrCreate(actor);
                if (go == null) return false;
                if (!listener.ParamCompProperty.isComponent) { //参数是一个GO
                    var goAction = (UnityAction<GameObject>)GetByPath(world, listener, typeof(UnityAction<GameObject>));
                    if (goAction == null) return false;
                    if (goAction.Method.IsStatic) return AddStaticListener(ref unityEvent, goAction, go);
                    unityEvent.AddObjectPersistentListener(goAction, go);
                } else {
                    FString paramCompTypeStr = listener.ParamCompProperty.ComponentType;
                    if (paramCompTypeStr == null || paramCompTypeStr.ToString() == "") return false;
                    Type paramCompType = GetType(paramCompTypeStr.ToString());
                    if (paramCompType == null) return false;
                    MethodInfo method = typeof(EventHandlerUtility).GetMethod("ComponentParamHandler", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (method == null) return false;
                    MethodInfo genericMethod = method.MakeGenericMethod(typeof(T), paramCompType);
                    object[] parameters = [world, go, unityEvent, listener];
                    bool result = (bool)genericMethod.Invoke(null, parameters);
                    return result;
                }
                break;
            case EPersistentListenerMode.Bool:
                var bAction = (UnityAction<bool>)GetByPath(world, listener, typeof(UnityAction<bool>));
                if (bAction == null) return false;
                var bValue = Convert.ToBoolean(listener.Value.ToString());
                if (bAction.Method.IsStatic) return AddStaticListener(ref unityEvent, bAction, bValue);
                unityEvent.AddBoolPersistentListener(bAction, bValue);
                break;
            case EPersistentListenerMode.Int:
                var iAction = (UnityAction<int>)GetByPath(world, listener, typeof(UnityAction<int>));
                if (iAction == null) return false;
                var iValue = Convert.ToInt32(listener.Value.ToString());
                if (iAction.Method.IsStatic) return AddStaticListener(ref unityEvent, iAction, iValue);
                unityEvent.AddIntPersistentListener(iAction, iValue);
                break;
            case EPersistentListenerMode.Float:
                var fAction = (UnityAction<float>)GetByPath(world, listener, typeof(UnityAction<float>));
                if (fAction == null) return false;
                var fValue = Convert.ToSingle(listener.Value.ToString());
                if (fAction.Method.IsStatic) return AddStaticListener(ref unityEvent, fAction, fValue);
                unityEvent.AddFloatPersistentListener(fAction, fValue);
                break;
            case EPersistentListenerMode.String:
                var sAction = (UnityAction<string>)GetByPath(world, listener, typeof(UnityAction<string>));
                if (sAction == null) return false;
                var sValue = listener.Value.ToString();
                if (sAction.Method.IsStatic) return AddStaticListener(ref unityEvent, sAction, sValue);
                unityEvent.AddStringPersistentListener(sAction, sValue);
                break;
        }

        return true;
    }
    private static bool ComponentParamHandler<T, W>(
        UWorld world, 
        GameObject go, 
        ref T unityEvent,
        FUTUEventListener listener
    ) where T : UnityEventBase where W : Component
    {
        W component = go.GetComponent<W>();
        if (component == null) return false;
        Type actionType = typeof(UnityAction<>).MakeGenericType(typeof(W));
        var compAction = GetByPath(world, listener, actionType);
        if (compAction == null) return false;
        if (compAction.Method.IsStatic) return AddStaticListener(ref unityEvent, compAction, component);
        unityEvent.AddObjectPersistentListener((UnityAction<W>)compAction, component);
        return true;
    }
    static Dictionary<string, Type> typeStrDict = new()
    {
        {"UnityEngine.UI.Text", typeof(Text)},
        {"UnityEngine.UI.Button", typeof(Button)},
    };
    private static Type GetType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        if (typeStrDict.ContainsKey(typeName)) return typeStrDict[typeName];
        Type type = Type.GetType("Script.CoreUObject."+typeName+"U3_C");
        if (type != null) return type;
        type = Type.GetType("Script.CoreUObject."+typeName+"_C");
        if (type != null) return type;
        type = Type.GetType(typeName+"_C");
        if (type != null) return type;
        type = Type.GetType(typeName+"U3_C");
        if (type != null) return type;
        return null;
    }

    private static Delegate GetByPath(UWorld world, FUTUEventListener listener, Type delegateType)
    {
        if (listener == null) return null;
        string actorPath = listener.MethodCompProperty.GOPath.ToString();
        string className = listener.MethodCompProperty.ComponentType.ToString();
        string functionName = listener.MethodName.ToString();
        Delegate action = null;
        
        // 首先尝试查找静态函数，如果能找到直接返回。
        var classType = listener.MethodCompProperty.isComponent ? GetType(className) : typeof(GameObject);
        if (classType != null)
        {
            MethodInfo methodInfo = classType.GetMethod(functionName, BindingFlags.Public | BindingFlags.Static);
            if (methodInfo != null && methodInfo.IsStatic)
            {
                action = Delegate.CreateDelegate(delegateType, methodInfo, false);
                if (action != null) return action;
            }
        }
        
        // 查找成员函数
        if (world == null || delegateType == null || !delegateType.IsSubclassOf(typeof(Delegate))) return null;
        TArray<AActor> actors = new TArray<AActor>();
        UGameplayStatics.GetAllActorsWithTag(world, "U3Path_" + actorPath, ref actors);
        if (actors == null || actors.Num() != 1) return null;
        AActor actor = actors[0];
        if (!listener.MethodCompProperty.isComponent)
        {
            // 查找GO的成员函数
            GameObject go = GameObject.GetFromActorOrCreate(actor);
            action = Delegate.CreateDelegate(delegateType, go, functionName, false, false);
            return action;
        }
        TArray<UActorComponent> components = UGUSDActorUtil.GetComponentsByU3Name(actor, className);
        if (components == null || components.IsEmpty()) return null;
        foreach (var component in components)
        {
            if (component == null || functionName == null || functionName == "") continue;
            
            if (component is U3ComponentU3_C u3ComponentU3C)
            { // 新架构
                if (u3ComponentU3C.proxy != null)
                    action = Delegate.CreateDelegate(delegateType, u3ComponentU3C.proxy,
                        functionName, false, false);
            }
            else
                action = Delegate.CreateDelegate(delegateType, component, functionName,
                    false, false);
            if (action != null) return action;
        }
        return null;
    }
}