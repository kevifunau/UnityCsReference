using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Script.Engine;
using Script.UnrealCSharp;
using UnityEngine;
using Object = System.Object;

namespace Script.DynamicCodeGen;

public class Utils
{
    public static void SetPrivateField<U1FieldType>(Component proxy, string u3FieldName, U1FieldType u1Value)
    {
		// Get the current type
        Type type = proxy.GetType();

        // Recursively search for fields
        FieldInfo fieldInfo = null;
        while (type != null && type != typeof(Component))
        {
            fieldInfo = type.GetField(u3FieldName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo != null)
            {
                break;
            }

			// If not found in the current type, try searching in the parent class
            type = type.BaseType;
        }

        if (fieldInfo == null)
        {
            Console.WriteLine($"{proxy.GetType()} and its base classes did not find field {u3FieldName}");
            return;
        }

        try
        {
            fieldInfo.SetValue(proxy, u1Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set field {u3FieldName}: {ex.Message}");
        }
    }


    public static void SetPrivateProperty<U1FieldType>(Component proxy, string u3FieldName, U1FieldType u1Value)
    {
        PropertyInfo propertyInfo =
            proxy.GetType().GetProperty(u3FieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (propertyInfo == null)
        {
            throw new Exception($"{proxy.GetType()} did not find field {u3FieldName}");
        }

        propertyInfo.SetValue(proxy, u1Value);
    }

    public static MethodInfo InvokeMethod(Component proxy, string u3MethodName, object[] parameters = null)
    {
        var parameterCount = parameters?.Length ?? 0;
        Type currentType = proxy.GetType();
        while (currentType != null)
        {
            MethodInfo method = currentType.GetMethod(u3MethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
            if (method != null && method.GetParameters().Length == parameterCount)
            {
                method.Invoke(proxy, parameters);
                return method;
            }
        
            currentType = currentType.BaseType;
        }

        return null;
    }

    public static U3Class InvokeConstructor<U3Class>()
    {
        // 1. Get the Type
        Type type = typeof(U3Class);
        
        // 2. Get the protected constructor
        ConstructorInfo ctor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [],
            null
        );

        if (ctor == null)
        {
            throw new Exception($"{typeof(U3Class)} did not find constructor");
        }

        // 3. Invoke the constructor
        return (U3Class)ctor.Invoke(null);
    }
    
    // 在Class里找Filed
    public static List<TField> GetFieldsByType<TField>(object obj, bool includeStatic = false)
    {
        if (obj == null) return [];
        var results = new List<TField>();
        Type classType = obj.GetType();
        Type targetType = typeof(TField);
    
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        bindingFlags |= includeStatic ? BindingFlags.Static : BindingFlags.Instance;

        foreach (FieldInfo field in classType.GetFields(bindingFlags))
        {
            if (targetType.IsAssignableFrom(field.FieldType))
            {
                results.Add((TField)field.GetValue(obj));
            }
        }
    
        return results;
    }

    /**
     * @bref Used for scripts in blueprint, to find component with refTag
     * @param _component The root component of components to serach
     * @param refTag Tag name to locate component, e.g. U3Path_prefabNested/Cube
     */
    public static USceneComponent GetSceneComponentInBluePrint(USceneComponent _component, string refTag)
    {
        if (_component == null || refTag == null) return null;
        var outActor = _component.GetOwner();
        if (outActor == null) return null;
        int tagIdx = refTag.IndexOf("_");
        string tagPrefix = refTag.Substring(0, tagIdx+1);
        string strPath = refTag.Substring(tagIdx+1);
        var paths = strPath.Split('/').ToList();
        int pathCnt = 1;
        USceneComponent comp = null;
        string curTag = null;
        string nextTag = tagPrefix + string.Join('/', paths.Take(pathCnt));
        var comps = outActor.GetComponentsByTag(USceneComponent.StaticClass(), nextTag);
        if (comps == null || comps.Num() != 1) return null;
        comp = (USceneComponent)comps[0];
        curTag = nextTag;
        // Consider the condition of nested prefab, we need to search path layer by layer
        while (!(comps == null || comps.Num() != 1) && refTag != curTag)
        {
            comp = (USceneComponent)comps[0];
            curTag = nextTag;
            pathCnt += 1;
            nextTag = tagPrefix + string.Join('/', paths.Take(pathCnt));
            comps = outActor.GetComponentsByTag(USceneComponent.StaticClass(), nextTag);
        }
        if (comp == null) return null;
        // Nested prefab
        if (refTag != curTag)
        {
            if (comp is not UChildActorComponent) return null;
            var prefabComp = ((UChildActorComponent)comp).ChildActor.RootComponent;
            if (prefabComp == null) return null;
            string strPrefabRoot = "";
            foreach (var tag in prefabComp.ComponentTags)
            {
                if (tag.ToString().Contains(tagPrefix))
                    strPrefabRoot = tag.ToString();
            }
            if (strPrefabRoot == "") return null;
            string subPath = refTag.Substring(curTag.Length);
            comp = GetSceneComponentInBluePrint(prefabComp, strPrefabRoot + subPath);
        }
        return comp;
    }
}