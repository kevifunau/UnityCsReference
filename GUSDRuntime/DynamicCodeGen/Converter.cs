using System;
using System.Collections.Generic;
using System.Reflection;
using Script.CoreUObject;
using Script.Engine;
using Script.UnrealCSharp;
using UnityEngine;

namespace Script.DynamicCodeGen;

public class Converter
{
    public static T ConvertDefault<T>(T o, U3ComponentU3_C _component)
    {
        return o;
    }

    public static T ConvertComponent<T>(FComponentParameter attrU1, U3ComponentU3_C _component) where T : Component
    {
        if (attrU1 == null) return null;
        UClass staticClass = GameObject.GetU3Class(typeof(T));
        if (staticClass == null) return null;
        
        UActorComponent comp = null;
        if (attrU1.ParamType == EComponentParameterType.InGO)
        {
            if (attrU1.InGOParam == null) return null;
            comp = attrU1.InGOParam.GetComponentByClass(staticClass);
        }
        else if (attrU1.ParamType == EComponentParameterType.InPrefab)
        {
            if (attrU1.InPrefabParam == null) return null;
            string refStr = attrU1.InPrefabParam.ToString();
            var containerComp = Utils.GetSceneComponentInBluePrint(_component, refStr);
            if (containerComp == null) return null;
            if (containerComp.IsA<UChildActorComponent>())
            {
                var childActorComponent = (UChildActorComponent)containerComp;
                var uClass = childActorComponent.ChildActor.GetClass();
                // child form bp
                if (uClass.IsA<UBlueprintGeneratedClass>())
                {
                    return GameObject.GetChildComponent<T>(childActorComponent.ChildActor.RootComponent);  
                }
            }
            return GameObject.GetChildComponent<T>(containerComp);
        }
        
        U3ComponentU3_C compU3 = comp as U3ComponentU3_C;
        if (compU3 == null) return null;
        return (T)compU3.proxy;
    }

    public static GameObject ConvertGameObject(FGameObjectParameter attrU1, U3ComponentU3_C _component)
    {
        if (attrU1 == null) return null;
        AActor act = null;
        if (attrU1.ParamType == EGameObjectParameterType.Blueprint) // Prefab asset in Unity, corresponding to Blueprints in UE
        {
            if (attrU1.BlueprintParam == null) return null;
            var blueprintClass = attrU1.BlueprintParam.Get();
            var go = new GameObject(blueprintClass);
            go.SetBlueClassAllNodes(blueprintClass);
            return go;
        }
        else if (attrU1.ParamType == EGameObjectParameterType.Actor)  // GO in Unity, corresponding to Actor in UE
        {
            if (attrU1.ActorParam == null) return null;
            act = attrU1.ActorParam;
        }
        else if (attrU1.ParamType ==  EGameObjectParameterType.ChildActorComponent)  // prefab's GO in Unity, corresponding to ChildActorComponent in UE
        {
            if (attrU1.ChildActorComponentParam == null) return null;
            string refStr = attrU1.ChildActorComponentParam.ToString();
            var comp = Utils.GetSceneComponentInBluePrint(_component, refStr);
            if (comp == null) return null;
            var childComp = comp as UChildActorComponent;
            if (childComp != null)
            {
                return GameObject.GetFromActorOrCreate(childComp.ChildActor);
            }
            else
            {
                act = comp.GetOwner(); // The root node of the prefab is not a ChildActorComponent   
            }
        }
        if (act == null) return null;
        return GameObject.GetFromActorOrCreate(act);
    }

    
    // 输入U1属性，输出U3属性
    // U1是class（使用[UStruct]标记），包含property
    // U3是struct，包含Field
    public static object ConvertStruct(object attrU1, U3ComponentU3_C _component)
    {
        Type typeU1 = attrU1.GetType();
        FieldInfo fieldInfo = typeU1.GetField("U3StructName", BindingFlags.Public | BindingFlags.Static);
        string typeU3Name = fieldInfo?.GetValue(null) as string;
        int lastDotIndex = typeU3Name.LastIndexOf('.');
        if (lastDotIndex != -1)
        {
            typeU3Name = typeU3Name.Substring(0, lastDotIndex) + "+" + typeU3Name.Substring(lastDotIndex + 1);
        }
        Type typeU3 = Type.GetType(typeU3Name);
        object result = Activator.CreateInstance(typeU3);
        
        // 对U1的所有属性进行遍历，用于初始化对应的U3字段
        PropertyInfo[] properties = typeU1.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo propertyU1 in properties)
        {
            FieldInfo fieldU3 = typeU3.GetField(propertyU1.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldU3 == null) continue;
            
            // 获取convert函数并调用，将U1属性转化为U3
            MethodInfo convertMethodInfo = typeU1.GetMethod($"convert{propertyU1.Name}");
            if (convertMethodInfo == null) continue;
            object valueU3 = convertMethodInfo.Invoke(attrU1, [_component]);
            fieldU3.SetValue(result, valueU3);
        }
        
        return result;
    }

    public static Vector3 ConvertVector3(FVector3f vector, U3ComponentU3_C _component)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    public static string ConvertString(FString s, U3ComponentU3_C _component)
    {
        return s.ToString();
    }
    

    public static Color ConvertColor(FLinearColor c, U3ComponentU3_C _component)
    {
        return new Color(c.R / 255, c.G / 255, c.B / 255, c.A / 255);
    }

    public static AudioClip ConvertUSoundWave(USoundWave wave, U3ComponentU3_C _component)
    {
        return new AudioClip(wave);
    }
    
    public static FLinearColor ConvertFColor(Color c)
    {
        return new FLinearColor(c.r, c.g, c.b, c.a);
    }
}