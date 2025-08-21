// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GUSD.Utils;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Script.CoreUObject;
using Script.Dynamic;
using Script.DynamicCodeGen;
using Script.Engine;
using Script.Library;
using Script.UMG;
using Script.UnrealCSharp;
using Script.UtuRuntime;
using UnityEngineInternal;
using UnityEngine.SceneManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UI;


namespace UnityEngine
{
    [ExcludeFromPreset]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Export/Scripting/GameObject.bindings.h")]
    public sealed partial class GameObject : Object
    {
        private static readonly Lazy<HashSet<string>> U3ExportedSet = new Lazy<HashSet<string>>(() =>
            new HashSet<string>(
                Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(type =>
                        type.GetCustomAttribute<U3ExportedAttribute>() != null ||
                        type.GetCustomAttribute<U3TmpBaseExportedAttribute>() != null
                    )
                    .Select(type => type.FullName)
            )
        );

        public static bool IsU3ExportedClass(Type type)
        {
            return U3ExportedSet.Value.Contains(type.FullName);
        }

        public static UClass GetU3Class(Type type)
        {
            string nameSpaceOri = type.Namespace;
            string typeNameU3 = "";
            if (string.IsNullOrEmpty(nameSpaceOri))
                typeNameU3 = "Script.CoreUObject." + type.FullName + "U3_C";
            else
                typeNameU3 = type.FullName + "U3_C";
            Type typeU3 = Type.GetType(typeNameU3);
            var methodInfo = typeU3?.GetMethod("StaticClass", BindingFlags.Static | BindingFlags.Public);
            if (methodInfo == null) return null;
            UClass staticClass = (UClass)methodInfo.Invoke(null, null);
            return staticClass;
        }
        
        public static bool isUiGO(TArray<FName> tags)
        {
            return tags.Contains("Ui") || tags.Contains("UI_Canvas");
        }

        public static Object CloneObjectWithTransform(Object data, Vector3 position, Quaternion rotation)
        {
            GameObject fromGO = (GameObject)data;
            FVector traslation = U3VectorUtil.GetU1LocationFromU3(position);
            FQuat u1Qua = U3QuaternionUtil.ConvertU3QuatToU1(rotation);
            FTransform transform = new FTransform(u1Qua.Rotator(), traslation, FVector.OneVector);
            AActor cloneActor;
            cloneActor = CloneActor(fromGO.actor, transform);
            CloneChildActors(fromGO.actor, cloneActor);

            GameObject newGO = GetFromActorOrCreate(cloneActor);
            TArray<FName> tags = UGUSDActorUtil.GetTags(fromGO.actor);
            newGO.name = FindTagByPrefix("U3Name_", ref tags) + "(Clone)";
            bool goIsUI = isUiGO(tags);
            if (newGO.transform == null && !goIsUI)
            {
                newGO.AddComponent<Transform>();
            }
            return newGO;
        }

        public static Object CloneObject(Object data)
        {
            return CloneObjectWithTransform(data, Vector3.zero, Quaternion.identity);
        }
        public static AActor CloneActor(AActor actor, FTransform actorTransform)
        {
            //When cloning an actor, clone the original actor components together
            FActorSpawnParameters SpawnParams = new FActorSpawnParameters();
            SpawnParams.SpawnCollisionHandlingOverride =
                ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn;
            // 使用深拷贝解决浅拷贝会拷贝引用的问题
            var cleanTemplate = Unreal.DuplicateObject<AActor>(actor, Unreal.GetTransientPackage());
            SpawnParams.Template = cleanTemplate; // Specify constructor template as actor
            // TODO: SpawnActor概率性失效，在SpawnActor方法中崩掉，不会走到 if (cloneActor == null) 的判断行
            // TODO: 进一步定位，发现可能是actor.GetClass()为null
            AActor cloneActor = ASceneUtil.GetCurWorld().SpawnActor<AActor>(actor.GetClass(), actorTransform.IsValid() ? actorTransform : FTransform.Identity, SpawnParams);
            if (cloneActor == null)
            {
                Debug.LogWarning("SpawnActor fail!");
                return null;
            }
            return cloneActor;
        }
        
        public static AActor CloneFromClass(UClass blueClass, FTransform actorTransform)
        {
            FActorSpawnParameters SpawnParams = new FActorSpawnParameters();
            SpawnParams.SpawnCollisionHandlingOverride =
                ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn;
            AActor cloneActor = ASceneUtil.GetCurWorld().SpawnActor<AActor>(blueClass, actorTransform.IsValid() ? actorTransform : FTransform.Identity, SpawnParams);
            if (cloneActor == null)
            {
                Debug.LogWarning("CloneFromClass fail!");
                return null;
            }
            return cloneActor;
        }
        
        public static void CloneChildActors(AActor originalActor, AActor cloneActor) {

            TArray<AActor> childActors = new TArray<AActor>();
            originalActor.GetAttachedActors(ref childActors);
            if (childActors.Count() == 0)
                return;
            //Deep priority cloning of each sub actor
            foreach (var Child in childActors)
            {
                AActor clonedChild = CloneActor(Child, FTransform.Identity);
                // Attach to the parent node of the new clone
                clonedChild.K2_AttachToActor(cloneActor, FName.NAME_None, EAttachmentRule.KeepWorld,
                    EAttachmentRule.KeepWorld, EAttachmentRule.KeepWorld, false);
                GameObject newGO = GetFromActorOrCreate(clonedChild);
                TArray<FName> tags = UGUSDActorUtil.GetTags(Child);
                newGO.name = FindTagByPrefix("U3Name_", ref tags) + "(Clone)";
                bool goIsUI = isUiGO(tags);
                if (newGO.transform == null && !goIsUI)
                {
                    newGO.AddComponent<Transform>();
                }
                CloneChildActors(Child, clonedChild);
            }
        }

        public static Object Load(string path)
        {
            string prefabName = path.Substring(path.LastIndexOf("/")+1);
            string prefix = path.Substring(0,path.Length-prefabName.Length);
            string bpName = "BP_" + prefabName;
            string bpPath = "/Game/"+prefix+bpName+"."+bpName+"_C";
            UClass blueprintClass = Unreal.LoadClass(null, bpPath);
            if (blueprintClass == null)
            {
                Debug.LogWarning("fail to load blueprint: " + path);
                return null;
            }

            GameObject prefab = new GameObject(blueprintClass, bpPath);
            prefab.SetBlueClassAllNodes(blueprintClass);
            return prefab;
        }

        public void SetBlueClassAllNodes(UClass blueprintClass)
        {
            if (blueprintClass == null || !blueprintClass.IsA<UBlueprintGeneratedClass>()) return;
            var blueprintGeneratedClass = (UBlueprintGeneratedClass)blueprintClass;
            var allNodes = blueprintGeneratedClass.SimpleConstructionScript.AllNodes;
            m_Nodes = allNodes;
        }

        private AActor _actor;

        public AActor actor
        {
            get
            {
                if (_actor != null) return _actor;
                if (bpPath != "")
                {
                    bpClass = Unreal.LoadClass(null, bpPath);
                }
                if (bpClass != null)
                    _actor = (AActor)bpClass.ClassDefaultObject;
                return _actor;
            }
            set => _actor = value;
        }

        private UClass bpClass;
        private string bpPath="";
        public UCanvasPanel canvasPanel { get; set; }
        
        [Obsolete("This method is obsolete. Please use GetFromActorOrCreate instead.")]
        public static GameObject MakeFromActor(AActor actor)
        {
            GameObject go = new GameObject(actor);
            go.actor = actor;
            return go;
        }
        
        public static GameObject GetFromActorOrCreate(AActor actor)
        {
            if (actor == null) return null;
            GameObject go;
            if (actor.gameObject == null)
            {
                go = new GameObject(actor);
            }
            else
            {
                go = actor.gameObject as GameObject;
            }

            actor.gameObject = go;
            return go;
        }

        [FreeFunction("GameObjectBindings::CreatePrimitive")]
        public extern static GameObject CreatePrimitive(PrimitiveType type);

        [System.Security.SecuritySafeCritical]
        public unsafe T GetComponent<T>()
        {
            Component component = GetComponent(typeof(T));
            if (component is T ret)
            {
                return ret;
            }

            return default(T);
        }

        // To retrieve a U3 component, use gameobject.GetComponent(xxx). 
        // This method fetches the xxxU3_C component attached to the actor and returns its proxy xxx to the user.

        // To retrieve a U1 component, use gameobject.actor.GetComponentByClass().
        public Component GetComponent(Type type)
        {
            if (actor == null || actor.GetWorld() == null)
            {
                if (m_Nodes == null) return null;
                foreach (var uscsNode in m_Nodes)
                {
                    var template = uscsNode.ComponentTemplate;
                    if (template == null || !template.IsA<U3ComponentU3_C>()) continue;
                    var component = template as U3ComponentU3_C;
                    if (component != null && CheckType(template.GetType(), type))
                    {
                        if (component.proxy == null)
                        {
                            component.LoadAddProxy((Component)Activator.CreateInstance(type));
                        }
                        return component.proxy; 
                    }
                }
                return null;
            }
            
            // actor is ChildActor and childActor form bp
            if (actor.IsChildActor() && !actor.GetClass().IsA<UBlueprintGeneratedClass>())
            {
                return GetChildComponent(actor.GetParentComponent(), type);    
            }
            return GetChildComponent(actor.RootComponent, type);
        }
        
        /*seek component from direct children components in one scenecomponent*/
        private static int _currentPoolCount = 0;
        private const int MaxPoolSize = 1000; 
        private static ConcurrentBag<TArray<USceneComponent>> _childrenComponentsPool = 
            new ConcurrentBag<TArray<USceneComponent>>();
        
        public static void ReturnToPool(ref TArray<USceneComponent> componentArray)
        {
            componentArray.Reset();
            if (Interlocked.Increment(ref _currentPoolCount) <= MaxPoolSize)
            {
                _childrenComponentsPool.Add(componentArray);
            }
            else
            {
                Interlocked.Decrement(ref _currentPoolCount);
                (componentArray as IDisposable)?.Dispose();
            }
        }
        
        [FreeFunction(Name = "GameObjectBindings::GetComponentFastPath", HasExplicitThis = true, ThrowsException = true)]
        [NativeWritableSelf]
        internal extern void GetComponentFastPath(Type type, IntPtr oneFurtherThanResultValue);

        [FreeFunction(Name = "Scripting::GetScriptingWrapperOfComponentOfGameObject", HasExplicitThis = true)]
        internal extern Component GetComponentByName(string type);

        [FreeFunction(Name = "Scripting::GetScriptingWrapperOfComponentOfGameObjectWithCase", HasExplicitThis = true)]
        internal extern Component GetComponentByNameWithCase(string type, bool caseSensitive);

        public Component GetComponent(string type)
        {
            Type targetType = ResolveType(type);
            return targetType == null ? null : GetComponent(targetType);
        }

		private static Type ResolveType(string typeName)
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            var types = currentAssembly.GetTypes();
            foreach (var type in types)
            {
                if (type.Name == typeName)
                {
                    return type;
                }
            }
            return Type.GetType(typeName);
        }

        public Component GetComponentInChildren(Type type, bool includeInactive)
        {
            var component = GetComponent(type);
            if (component != null) return component;
            
            if (actor.IsChildActor() && !actor.GetClass().IsA<UBlueprintGeneratedClass>())
            {
                var allChildActorComponent = GetAllU1ChildComponent<UChildActorComponent>(actor.GetParentComponent());
                if (allChildActorComponent != null)
                {
                    foreach (var childActorComponent in allChildActorComponent)
                    {
                        component = GetComponentInChildActorComponent(childActorComponent, type);
                        if (component != null) return component;
                    }   
                }    
            }
            else
            {
                // 获取attach的Actor
                var attachActors = new TArray<AActor>();
                actor.GetAttachedActors(ref attachActors, false);
                // 获取子Actor
                var childActors = new TArray<AActor>();
                actor.GetAllChildActors(ref childActors, false);
                var allChildActors = childActors.Union(attachActors);
                foreach (var childActor in allChildActors)
                {
                    var childGO = GetFromActorOrCreate(childActor);
                    component = childGO.GetComponentInChildren(type);
                    if (component != null) return component;
                }
            }

            return null;
        }

        private Component GetComponentInChildActorComponent(UChildActorComponent childActorComponentIn, Type type)
        {
            var component = GetChildComponent(childActorComponentIn, type);
            if (component != null) return component;
            var allChildActorComponent = GetAllU1ChildComponent<UChildActorComponent>(childActorComponentIn);
            foreach (var childActorComponent in allChildActorComponent)
            {
                if (childActorComponent == childActorComponentIn) continue;
                component = GetComponentInChildActorComponent(childActorComponent, type);
                if (component != null) return component;
            }

            return null;
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInChildren(Type type)
        {
            return GetComponentInChildren(type, false);
        }

        [uei.ExcludeFromDocs]
        public T GetComponentInChildren<T>()
        {
            bool includeInactive = false;
            return GetComponentInChildren<T>(includeInactive);
        }

        public T GetComponentInChildren<T>([uei.DefaultValue("false")] bool includeInactive)
        {
            return (T)(object)GetComponentInChildren(typeof(T), includeInactive);
        }

        public Component GetComponentInParent(Type type, bool includeInactive)
        {
            var component = GetComponent(type);
            if (component != null) return component;
            var parent = transform.parent;
            if (parent == null) return null;
            return GetFromActorOrCreate(parent.owner).GetComponentInParent(type);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInParent(Type type)
        {
            return GetComponentInParent(type, false);
        }

        [uei.ExcludeFromDocs]
        public T GetComponentInParent<T>()
        {
            bool includeInactive = false;
            return GetComponentInParent<T>(includeInactive);
        }

        public T GetComponentInParent<T>([uei.DefaultValue("false")] bool includeInactive)
        {
            return (T)(object)GetComponentInParent(typeof(T), includeInactive);
        }

        [FreeFunction(Name = "GameObjectBindings::GetComponentsInternal", HasExplicitThis = true, ThrowsException = true)]
        private extern System.Array GetComponentsInternal(Type type, bool useSearchTypeAsArrayReturnType, bool recursive, bool includeInactive, bool reverse, object resultList);

        public Component[] GetComponents(Type type)
        {
            USceneComponent parentComponent = actor.RootComponent;
            if (actor.IsChildActor() && !actor.GetClass().IsA<UBlueprintGeneratedClass>())
            {
                parentComponent = actor.GetParentComponent();
            }
            
            List<Component> list = new List<Component>();

            if (parentComponent == null)
            {
                return list.ToArray();
            }
            
            var u3Component = parentComponent as U3ComponentU3_C;
            if (u3Component != null && type.IsInstanceOfType(u3Component.proxy))
            {
                list.Add(u3Component.proxy);
            }
            
            if (!_childrenComponentsPool.TryTake(out var childrenComponents))
            {
                childrenComponents = new TArray<USceneComponent>();
            }    
            else
            {
                Interlocked.Decrement(ref _currentPoolCount); // Decrement the count when successfully retrieved
            }

            try
            {
                parentComponent.GetChildrenComponents(false, ref childrenComponents);
                foreach (var childComponent in childrenComponents)
                {
                    var u3ComponentIn = childComponent as U3ComponentU3_C;
                    if (u3ComponentIn != null && type.IsInstanceOfType(u3ComponentIn.proxy))
                    {
                        list.Add(u3ComponentIn.proxy);
                    }
                }

                return list.ToArray();
            }
            finally
            {
                ReturnToPool(ref childrenComponents);
            }
        }

        public T[] GetComponents<T>()
        {
            // return (T[])GetComponentsInternal(typeof(T), true, false, true, false, null);
            var temp = GetComponents(typeof(T));
            if (temp == null || temp.Length == 0) return new T [0];
            var result = new T[temp.Length];
            var i = 0;
            foreach (var component in temp)
                if (component is T castedComponent)
                    result[i++] = castedComponent;

            return result;
        }

        public void GetComponents(Type type, List<Component> results)
        {
            // GetComponentsInternal(type, false, false, true, false, results);
            var components = GetComponents(type)?.OfType<Component>();
            if (components != null)
            {
                results.AddRange(components);
            }
        }

        public void GetComponents<T>(List<T> results)
        {
            // GetComponentsInternal(typeof(T), true, false, true, false, results);
			var components = GetComponents(typeof(T))?.OfType<T>();
            if (components != null)
            {
                results.AddRange(components);
            }
        }

        [uei.ExcludeFromDocs]
        public Component[] GetComponentsInChildren(Type type)
        {
            bool includeInactive = false;
            return GetComponentsInChildren(type, includeInactive);
        }

        public Component[] GetComponentsInChildren(Type type, [uei.DefaultValue("false")] bool includeInactive)
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, false, null);
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            // return (T[])GetComponentsInternal(typeof(T), true, true, includeInactive, false, null);
            var components = GetComponents<T>();
            if (components != null) return components;

            var childActors = new TArray<AActor>();
            actor.GetAttachedActors(ref childActors);
            foreach (var childActor in childActors)
            {
                var childGO = GetFromActorOrCreate(childActor);
                components = childGO.GetComponentsInChildren<T>();
                if (components != null) return components;
            }

            return null;
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results)
        {
            GetComponentsInternal(typeof(T), true, true, includeInactive, false, results);
        }

        public T[] GetComponentsInChildren<T>()
        {
            return GetComponentsInChildren<T>(false);
        }

        public void GetComponentsInChildren<T>(List<T> results)
        {
            GetComponentsInChildren<T>(false, results);
        }

        [uei.ExcludeFromDocs]
        public Component[] GetComponentsInParent(Type type)
        {
            bool includeInactive = false;
            return GetComponentsInParent(type, includeInactive);
        }

        public Component[] GetComponentsInParent(Type type, [uei.DefaultValue("false")] bool includeInactive)
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, true, null);
        }

        public void GetComponentsInParent<T>(bool includeInactive, List<T> results)
        {
            // GetComponentsInternal(typeof(T), true, true, includeInactive, true, results);
            var findResults = GetComponentsInParent<T>(includeInactive);
            if (findResults != null)
            {
                results.AddRange(findResults);
            }
        }

        public T[] GetComponentsInParent<T>(bool includeInactive)
        {
            var components = GetComponents<T>();
            if (components != null && components.Length > 0) return components;
            var parent = transform.parent;
            if (parent == null) return null;
            return GetFromActorOrCreate(parent.owner).GetComponentsInParent<T>();
        }

        public T[] GetComponentsInParent<T>()
        {
            return GetComponentsInParent<T>(false);
        }
        
        public static T GetU1ChildComponent<T>(USceneComponent component) where T : UObject, IStaticClass
        {
            var parentComponent = GetUParentComponent(component);
            if (parentComponent == null)
            {
                return null;
            }
            
            if (parentComponent.IsA<T>())
            {
                return (T)(UObject)parentComponent;
            }
            
            if (!_childrenComponentsPool.TryTake(out var childrenComponents))
            {
                childrenComponents = new TArray<USceneComponent>();
            }    
            else
            {
                Interlocked.Decrement(ref _currentPoolCount); // Decrement the count when successfully retrieved
            }

            try
            {
                parentComponent.GetChildrenComponents(false, ref childrenComponents);
                foreach (var childComponent in childrenComponents)
                {
                    if (childComponent.IsA<T>())
                        return (T)(UObject)childComponent;
                }

                return null;
            }
            finally
            {
                ReturnToPool(ref childrenComponents);
            }
        }
        
        public static List<T> GetAllU1ChildComponent<T>(USceneComponent component) where T : UObject, IStaticClass
        {
            var parentComponent = GetUParentComponent(component);
            if (parentComponent == null)
            {
                return null;
            }
            
            List<T> list = new List<T>();
            if (parentComponent.IsA<T>())
            {
                list.Add((T)(UObject)parentComponent);
            }
            if (!_childrenComponentsPool.TryTake(out var childrenComponents))
            {
                childrenComponents = new TArray<USceneComponent>();
            }    
            else
            {
                Interlocked.Decrement(ref _currentPoolCount); // Decrement the count when successfully retrieved
            }

            try
            {
                parentComponent.GetChildrenComponents(false, ref childrenComponents);
                foreach (var childComponent in childrenComponents)
                {
                    if (childComponent.IsA<T>())
                        list.Add((T)(UObject)childComponent);
                }

                return list;
            }
            finally
            {
                ReturnToPool(ref childrenComponents);
            }
        }
        
        public static T GetChildComponent<T>(USceneComponent component)
        {
            var parentComponent = GetUParentComponent(component);
            if (parentComponent == null)
            {
                return default;
            }
            var u3Component = parentComponent as U3ComponentU3_C;
            if (u3Component != null && u3Component.proxy is T proxy)
            {
                return proxy;
            }
            
            if (!_childrenComponentsPool.TryTake(out var childrenComponents))
            {
                childrenComponents = new TArray<USceneComponent>();
            }    
            else
            {
                Interlocked.Decrement(ref _currentPoolCount); // Decrement the count when successfully retrieved
            }

            try
            {
                parentComponent.GetChildrenComponents(false, ref childrenComponents);
                foreach (var childComponent in childrenComponents)
                {
                    var u3ComponentIn = childComponent as U3ComponentU3_C;
                    if (u3ComponentIn != null && u3ComponentIn.proxy is T proxyIn)
                    {
                        return proxyIn;
                    }
                }

                return default;
            }
            finally
            {
                ReturnToPool(ref childrenComponents);
            }
        }
        
        public static Component GetChildComponent(USceneComponent component, Type type)
        {
            var parentComponent = GetUParentComponent(component);
            if (parentComponent == null)
            {
                return null;
            }
            
            var u3Component = parentComponent as U3ComponentU3_C;
            ;
            if (u3Component != null && type.IsInstanceOfType(u3Component.proxy))
            {
                return u3Component.proxy;
            }
            
            if (!_childrenComponentsPool.TryTake(out var childrenComponents))
            {
                childrenComponents = new TArray<USceneComponent>();
            }    
            else
            {
                Interlocked.Decrement(ref _currentPoolCount); // Decrement the count when successfully retrieved
            }

            try
            {
                parentComponent.GetChildrenComponents(false, ref childrenComponents);
                foreach (var childComponent in childrenComponents)
                {
                    var u3ComponentIn = childComponent as U3ComponentU3_C;
                    if (u3ComponentIn != null && type.IsInstanceOfType(u3ComponentIn.proxy))
                    {
                        return u3ComponentIn.proxy;
                    }
                }

                return null;
            }
            finally
            {
                ReturnToPool(ref childrenComponents);
            }
        }

        private static USceneComponent GetUParentComponent(USceneComponent component)
        {
            if (component == null)
            {
                return null;
            }
            USceneComponent parentComponent;
            if (component.IsA<UChildActorComponent>())
            {
                parentComponent = component;
            }
            else
            {
                var owner = component.GetOwner();
                if (owner != null && component == owner.RootComponent)
                {
                    parentComponent = component;
                }
                else
                {
                    parentComponent = component.GetAttachParent();
                }
            }
            return parentComponent;
        }

        [System.Security.SecuritySafeCritical]
        public unsafe bool TryGetComponent<T>(out T component)
        {
            component = GetComponent<T>();
            return component != null;
        }

        public bool TryGetComponent(Type type, out Component component)
        {
            component = TryGetComponentInternal(type);
            return component != null;
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::TryGetComponentFromType", HasExplicitThis = true, ThrowsException = true)]
        internal extern Component TryGetComponentInternal(Type type);

        [FreeFunction(Name = "GameObjectBindings::TryGetComponentFastPath", HasExplicitThis = true, ThrowsException = true)]
        [NativeWritableSelf]
        internal extern void TryGetComponentFastPath(Type type, IntPtr oneFurtherThanResultValue);

        public static GameObject FindWithTag(string tag)
        {
            return FindGameObjectWithTag(tag);
        }

        public void SendMessageUpwards(string methodName, SendMessageOptions options)
        {
            SendMessageUpwards(methodName, null, options);
        }

        public void SendMessage(string methodName, SendMessageOptions options)
        {
            SendMessage(methodName, null, options);
        }

        public void BroadcastMessage(string methodName, SendMessageOptions options)
        {
            BroadcastMessage(methodName, null, options);
        }

        [FreeFunction(Name = "MonoAddComponent", HasExplicitThis = true)]
        internal extern Component AddComponentInternal(string className);

        private Component Internal_AddComponentWithType(Type componentType)
        {
            // Original path follows this branch
            if (!U3ExportedSet.Value.Contains(componentType.FullName))
            {
                var methodInfo = componentType.GetMethod("StaticClass", BindingFlags.Static | BindingFlags.Public);

                if (methodInfo?.Invoke(null, null) is UClass instance)
                {
                    var actorComponent = actor.AddComponentByClass(instance, false, FTransform.Identity, false);
                    var u3Component = actorComponent as U3ComponentU3_C;
                    if (u3Component == null)
                    {
                        return null;
                    }
                    Component component = u3Component.proxy;
                    component.InitializeCorrespondU1Component();
                    return component;
                }
            }
            else
            {
                UClass staticClass = GetU3Class(componentType);
                if (staticClass == null)
                {
                    throw new ArgumentException($"Could not find type: {componentType.FullName}");
                }
                UActorComponent actorComponent =
                    actor.AddComponentByClass(staticClass, false, FTransform.Identity, false);

                if (actorComponent is U3ComponentU3_C u3cComponent)
                {
                    var t = u3cComponent.proxy;
                    t.InitializeCorrespondU1Component();
                    return  t;
                }
            }
            
            return null;
        }
        

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component AddComponent(Type componentType)
        {
            var comp = Internal_AddComponentWithType(componentType);
            if (componentType.Namespace == "UnityEngine.UI")
            {
                var comParent = transform?.parent;
                var parentRect = comParent?.GetComponent<RectTransform>();
                AddRectTransform(parentRect);
            }
            return comp;
        }

        public T AddComponent<T>() where T : Component
        {
            return AddComponent(typeof(T)) as T;
        }

        public extern int GetComponentCount();

        [NativeName("QueryComponentAtIndex<Unity::Component>")]
        internal extern Component QueryComponentAtIndex(int index);

        public Component GetComponentAtIndex(int index)
        {
            if (index < 0 || index >= GetComponentCount()) throw new ArgumentOutOfRangeException(nameof(index), "Valid range is 0 to GetComponentCount() - 1.");
            return QueryComponentAtIndex(index);
        }

        public T GetComponentAtIndex<T>(int index) where T : Component
        {
            T component = (T)GetComponentAtIndex(index);
            if(component == null) throw new InvalidCastException();
            return component;
        }

        public extern int GetComponentIndex(Component component);

        private Transform m_transform;
        public Transform transform
        {
            get
            {
                if (m_transform == null)
                {
                    m_transform = GetComponent<Transform>();
                }
                return m_transform;
            }
        }

        public int layer
        {
            get
            {
                return -1;
            }
            set
            {    
                if (actor == null) return;
                // 直接使用值作为通道（因为枚举值匹配层索引）
                ECollisionChannel channel = (ECollisionChannel)Math.Clamp(value, 0, 32);
              
                TArray<UActorComponent> allComps =  actor.K2_GetComponentsByClass(UActorComponent.StaticClass());
         
                // 设置根组件碰撞类型（确保至少设置一个）
                if (allComps.Num() == 0)
                {
                    USceneComponent root = actor.K2_GetRootComponent();
                    if (root != null && root is UPrimitiveComponent primitiveRoot)
                    {
                        primitiveRoot.SetCollisionObjectType(channel);
                    }
                }
                else
                {
                    // 设置所有原始组件的碰撞类型
                    foreach (var comp in allComps)
                    {
                        if(comp is UPrimitiveComponent primitive)
                            primitive.SetCollisionObjectType(channel);
                    }

                }

                // 递归处理子Actor
                TArray<AActor> childActors = new TArray<AActor>();
                actor.GetAttachedActors(ref childActors);
                foreach (var child in childActors)
                {
                    var childGO = GetFromActorOrCreate(child);
                    childGO.layer = value;
                }
            }
        } 

        [Obsolete(
            "GameObject.active is obsolete. Use GameObject.SetActive(), GameObject.activeSelf or GameObject.activeInHierarchy.")]
        public bool active { [NativeMethod(Name = "IsActive")] get; [NativeMethod(Name = "SetSelfActive")] set; } = true;

        [CanBeNull] private TArray<USCS_Node> m_Nodes;

        private void SetSelfActive(bool value)
        {
            active = value;
            DoActive(actor, value);
            if (transform is RectTransform rectTransform)
            {
                if (rectTransform.targetCanvas != null)
                {
                    rectTransform.targetCanvas.SetVisibility(value ? ESlateVisibility.SelfHitTestInvisible : ESlateVisibility.Collapsed);
                    // 支持VerticalLayoutGroup动态布局功能
                    var verticalLayoutGroup = rectTransform.targetCanvas.GetParent()?.GetParent();
                    if (verticalLayoutGroup != null && verticalLayoutGroup is UUtuVerticalLayoutGroup)
                        rectTransform.targetCanvas.GetParent().SetVisibility(value ? ESlateVisibility.SelfHitTestInvisible : ESlateVisibility.Collapsed);
                }
            }
        }
        
        private static void DoActive(AActor aActor, bool ifActive)
        {
            if (ifActive)
            {
                DoActiveInternal(aActor, true);
            }
            var childActs = new TArray<AActor>();
            aActor.GetAttachedActors(ref childActs, true, true);
            if (childActs != null && !childActs.IsEmpty())
            {
                foreach (var childAct in childActs)
                {
                    DoActiveInternal(childAct, ifActive);
                }
            }

            if (ifActive)
            {
                return;
            }
            DoActiveInternal(aActor, false);
        }

        private static void DoActiveInternal(AActor aActor, bool ifActive)
        {
            aActor.SetActorHiddenInGame(!ifActive);
            aActor.SetActorEnableCollision(ifActive);
            aActor.SetActorTickEnabled(ifActive);
            foreach (UActorComponent uActorComponent in aActor.K2_GetComponentsByClass(USceneComponent.StaticClass()))
            {
                if (uActorComponent != null && uActorComponent is U3ComponentU3_C u3Component)
                {
                    u3Component.SetActive(ifActive,true);
                }
                
            }
        }
        
        [NativeMethod(Name = "SetSelfActive")]
        public void SetActive(bool value)
        {
            active = value;
            SetSelfActive(value);
        }

        public bool activeSelf
        {
            [NativeMethod(Name = "IsSelfActive")]
            get => active;
        }

        public bool activeInHierarchy
        {
            [NativeMethod(Name = "IsActive")]
            get => active;
        }

        [Obsolete(
            "gameObject.SetActiveRecursively() is obsolete. Use GameObject.SetActive(), which is now inherited by children.")]
        [NativeMethod(Name = "SetActiveRecursivelyDeprecated")]
        public void SetActiveRecursively(bool state)
        {
            SetActive(state);
        }

        public bool isStatic
        {
            [NativeMethod(Name = "GetIsStaticDeprecated")]
            get
            {
                if (actor == null) return false;
                USceneComponent root = actor.K2_GetRootComponent();
                return root != null && root.Mobility == EComponentMobility.Static;
            }
            [NativeMethod(Name = "SetIsStaticDeprecated")]
            set
            {
                
            }
        }


        internal extern bool isStaticBatchable
        {
            [NativeMethod(Name = "IsStaticBatchable")]
            get;
        }

        public string tag
        {
            get
            {
                var actorTags = UGUSDActorUtil.GetTags(actor);
                if (FindTagByPrefix("U3Tag_", ref actorTags) == "")
                {
                    actorTags.Add("U3Tag_Untagged");
                }
                return FindTagByPrefix("U3Tag_", ref actorTags);
            }
            set
            {
                var actorTags = UGUSDActorUtil.GetTags(actor);
                actorTags.Remove("U3Tag_" + FindTagByPrefix("U3Tag_", ref actorTags));
                actorTags.Add("U3Tag_" + value);
                UGUSDActorUtil.SetTags(actor, actorTags);
            }
        }

        public bool CompareTag(string tag)
        {
            return this.tag.Equals(tag);
        }

        [FreeFunction(Name = "GameObjectBindings::FindGameObjectWithTag", ThrowsException = true)]
        public static GameObject FindGameObjectWithTag(string tag)
        {
            foreach (AActor act in CurrentWorldAllAActors)
            {
                if (act == null) continue;
                GameObject go = GameObject.GetFromActorOrCreate(act);
                if (go.tag.Equals(tag))
                {
                    return go;
                }
            }

            return null;
        }

        [FreeFunction(Name = "GameObjectBindings::FindGameObjectsWithTag", ThrowsException = true)]
        public static GameObject[] FindGameObjectsWithTag(string tag)
        {
            List<GameObject> ret = new List<GameObject>();
            foreach (AActor act in CurrentWorldAllAActors)
            {
                GameObject go = GameObject.GetFromActorOrCreate(act);
                if (go.tag.Equals(tag))
                {
                    ret.Add(go);
                }
            }
            return ret.ToArray();
        }

        [FreeFunction(Name = "Scripting::SendScriptingMessageUpwards", HasExplicitThis = true)]
        extern public void SendMessageUpwards(string methodName, [uei.DefaultValue("null")]  object value , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void SendMessageUpwards(string methodName, object value)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            SendMessageUpwards(methodName, value, options);
        }

        [uei.ExcludeFromDocs]
        public void SendMessageUpwards(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object value = null;
            SendMessageUpwards(methodName, value, options);
        }

        [FreeFunction(Name = "Scripting::SendScriptingMessage", HasExplicitThis = true)]
        extern public void SendMessage(string methodName, [uei.DefaultValue("null")]  object value , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void SendMessage(string methodName, object value)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            SendMessage(methodName, value, options);
        }

        [uei.ExcludeFromDocs]
        public void SendMessage(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object value = null;
            SendMessage(methodName, value, options);
        }

        [FreeFunction(Name = "Scripting::BroadcastScriptingMessage", HasExplicitThis = true)]
        extern public void BroadcastMessage(string methodName, [uei.DefaultValue("null")]  object parameter , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void BroadcastMessage(string methodName, object parameter)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            BroadcastMessage(methodName, parameter, options);
        }

        [uei.ExcludeFromDocs]
        public void BroadcastMessage(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object parameter = null;
            BroadcastMessage(methodName, parameter, options);
        }

        public GameObject(string name)
        {
            Internal_CreateGameObject(this, name);
        }

        public GameObject(AActor actor = null)
        {
            this.actor = actor;
            Internal_CreateGameObject(this, null);
        }
        
        public GameObject(UClass blueClass, string bpPath)
        {
            this.bpClass = blueClass;
            this.bpPath = bpPath;
        }
        public GameObject(UClass blueClass)
        {
            this.bpClass = blueClass;
        }
        
        public GameObject()
        {
            Internal_CreateGameObject(this, null);
        }

        public GameObject(string name, params Type[] components)
        {
            Internal_CreateGameObject(this, name);
            foreach (Type t in components)
                AddComponent(t);
        }

        void Internal_CreateGameObject(GameObject self, string name)
        {
            // If it is a GameObject generated by game code, we need to create an Actor for it
            if (self.actor == null)
            {
                AActor tmp = UGUSDWorldUtil.SpawnDefaultRootActor();
                self.actor = tmp;
                if (name != null)
                {
                    self.name = name;
                }

                self.AddComponent<Transform>();
            }
        }
        
        public static TArray<AActor> CurrentWorldRootAActors
        {
            get
            {
                TArray<AActor> ret = new TArray<AActor>();
                foreach (AActor act in CurrentWorldAllAActors)
                {
                    if (act.GetAttachParentActor() == null)
                    {
                        ret.Add(act);
                    }
                }

                return ret;
            }
        }

        [FreeFunction(Name = "GameObjectBindings::Find")]
        public static GameObject Find(string name)
        {
            if (name == null)
            {
                return null;
            }

            AActor act;
            if (!name.Contains("/"))
            {
                act = SearchTargetTagActor(CurrentWorldAllAActors, name);
                return act == null ? null : GameObject.GetFromActorOrCreate(act);
            }

            bool isStartFromRoot = false;
            if (name[0].Equals('/'))
            {
                name = name.Substring(1);
                isStartFromRoot = true;
            }
            
            string[] actTags = name.Split('/');
            act = SearchTargetTagActor(isStartFromRoot ? CurrentWorldRootAActors : CurrentWorldAllAActors,
                actTags[0]);
            if (actTags.Length == 1)
            {
                return act == null ? null : GameObject.GetFromActorOrCreate(act);
            }

            AActor find = act;
            for (var i = 1; i < actTags.Length; i++)
            {
                TArray<AActor> childActs = new TArray<AActor>();
                find.GetAttachedActors(ref childActs);
                act = SearchTargetTagActor(childActs, actTags[i]);
                if (act == null)
                {
                    return null;
                }

                find = act;
            }
            
            return GameObject.GetFromActorOrCreate(find);
        }
        
        private static AActor SearchTargetTagActor(TArray<AActor> sourceActors, string Tag)
        {
            if (sourceActors == null || Tag == null)
            {
                return null;
            }
            foreach (AActor act in sourceActors)
            {
                GameObject go = GetFromActorOrCreate(act);
                if (go == null) continue;
                if (go.name.Equals(Tag))
                {
                    return act;
                }
            }
            return null;
        }
        public static string FindTagByPrefix(string prefix, ref TArray<FName> tags) {
            for (int i = 0; i < tags.Num(); ++i) {
                if (tags[i].ToString().StartsWith(prefix))
                {
                    return tags[i].ToString().Substring(prefix.Length);
                }
            }
            return "";
        }
        
        [FreeFunction(Name = "GameObjectBindings::SetGameObjectsActiveByInstanceID")]
        extern private static void SetGameObjectsActive(IntPtr instanceIds, int instanceCount, bool active);

        public static unsafe void SetGameObjectsActive(NativeArray<int> instanceIDs, bool active)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));

            if (instanceIDs.Length == 0)
                return;

            SetGameObjectsActive((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), instanceIDs.Length, active);
        }

        public static unsafe void SetGameObjectsActive(ReadOnlySpan<int> instanceIDs, bool active)
        {
            if(instanceIDs.Length == 0)
                return;

            fixed(int* instanceIDsPtr = instanceIDs)
            {
                SetGameObjectsActive((IntPtr)instanceIDsPtr, instanceIDs.Length, active);
            }
        }

        [FreeFunction("GameObjectBindings::InstantiateGameObjectsByInstanceID")]
        extern private static void InstantiateGameObjects(int sourceInstanceID, IntPtr newInstanceIDs, IntPtr newTransformInstanceIDs, int count, Scene destinationScene);

        public static unsafe void InstantiateGameObjects(int sourceInstanceID, int count, NativeArray<int> newInstanceIDs, NativeArray<int> newTransformInstanceIDs, Scene destinationScene = default)
        {
            if (!newInstanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(newInstanceIDs));
            if (!newTransformInstanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(newTransformInstanceIDs));
            if (count == 0)
                return;
            if ((count != newInstanceIDs.Length) || (count != newTransformInstanceIDs.Length))
                throw new ArgumentException("Size mismatch! Both arrays must already be the size of count.");

            InstantiateGameObjects(sourceInstanceID, (IntPtr)newInstanceIDs.GetUnsafeReadOnlyPtr(), (IntPtr)newTransformInstanceIDs.GetUnsafeReadOnlyPtr(), newInstanceIDs.Length, destinationScene);
        }

        [FreeFunction(Name = "GameObjectBindings::GetSceneByInstanceID")]
        public static extern Scene GetScene(int instanceID);

        public extern Scene scene
        {
            [FreeFunction("GameObjectBindings::GetScene", HasExplicitThis = true)]
            get;
        }

        public extern ulong sceneCullingMask
        {
            [FreeFunction(Name = "GameObjectBindings::GetSceneCullingMask", HasExplicitThis = true)]
            get;
        }

        [FreeFunction(Name = "GameObjectBindings::CalculateBounds", HasExplicitThis = true)]
        internal extern Bounds CalculateBounds();

        internal extern int IsMarkedVisible();

        public GameObject gameObject { get { return this; } }

        public void AddRectTransform(RectTransform parentTransform)
        {
            if (parentTransform == null)
            {
                Debug.LogError("AddRectTransform Parent transform is null");
                return;
            }
            var trans = transform;
            if (trans is RectTransform)
            {
                return;
            }
            Destroy(trans);
            UCanvasPanel parentCanvas = parentTransform.targetCanvas;
            var rect = Internal_AddComponentWithType(typeof(RectTransform));
            if (rect is RectTransform r)
            {
                m_transform = r;
                var u1RectTransform = r.U1RectTransformInit();
                var uiCanvasPanel = UGUSDUiUtil.CreateUICanvasPanel(parentCanvas, u1RectTransform);
                r.InitCanvasPanel(uiCanvasPanel);
            }
        }
    }
}
