// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Script.CoreUObject;
using Script.Engine;
using Script.UnrealCSharp;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.SceneManagement
{
    [NativeHeader("Runtime/Export/SceneManager/SceneManager.bindings.h")]
    [NativeHeader("Runtime/SceneManager/SceneManager.h")]
    [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
    internal static class SceneManagerAPIInternal
    {
        // Slim players do not contain scenes, so build settings will always return the wrong values, need to remap these to asset bundle sceneName lookup
        public static extern int GetNumScenesInBuildSettings();

        [NativeThrows]
        public static extern Scene GetSceneByBuildIndex(int buildIndex); 
        
        [NativeThrows]
        public static AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex,
            LoadSceneParameters parameters, bool mustCompleteNextFrame)
        { 
            Debug.Log($"LoadSceneAsyncNameIndexInternal: sceneName={sceneName}, buildIndex={sceneBuildIndex}");
            if (SceneManager.World == null)
            {
                Debug.LogError("SceneManager.World is not set!");
                return CreateFailedAsyncOperation();
            }
            try
            {
                string fullPath = sceneName;
                // 确保路径格式正确
                if (!string.IsNullOrEmpty(sceneName))
                {
                    if (!sceneName.StartsWith("/Game/"))
                    {
                        fullPath = "/Game/Scenes/Scenes/" + sceneName;
                    }
                    // 添加文件扩展名
                    if (!fullPath.EndsWith(".umap"))
                    {
                        fullPath += ".umap";
                    }
                }
                // 创建操作对象
                AsyncOperation asyncLoad = new AsyncOperation();
                int sceneId = ASceneUtil.loadScene(
                    SceneManager.World,
                    fullPath,
                    parameters.loadSceneMode == LoadSceneMode.Single);
                // 设置场景ID
                asyncLoad.setID(sceneId);
                asyncLoad.loadSceneName = fullPath;
              
                return asyncLoad;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return CreateFailedAsyncOperation();
            }
        }
      
        // 创建标记为失败的异步操作
        private static AsyncOperation CreateFailedAsyncOperation()
        {
            AsyncOperation op = new AsyncOperation();
            op.MarkAsFailed();  
            return op;
        }
        [NativeThrows]
        public static AsyncOperation UnloadSceneNameIndexInternal(string sceneName, int sceneBuildIndex,
            bool immediately, UnloadSceneOptions options, out bool outSuccess)
        {
            AsyncOperation asyncUnload = new AsyncOperation();
            if (sceneBuildIndex != -1 || sceneName == UGameplayStatics.GetCurrentLevelName(SceneManager.World).ToString())
            {
                outSuccess = false;
                UKismetSystemLibrary.PrintString(SceneManager.World, "unload failed");
                return asyncUnload;
            }
           
            
            FName name = new FName(sceneName);
            FLatentActionInfo actionInfo = new FLatentActionInfo();
            if (immediately == false)
            {
                //actionInfo.ExecutionFunction = "finished";
                //actionInfo.CallbackTarget = asyncUnload;
                actionInfo.UUID = FGuid.NewGuid().A;
            }
            outSuccess = true;
            UGameplayStatics.UnloadStreamLevel(SceneManager.World, name, actionInfo, immediately);
            return null;
        }
    }

    public class SceneManagerAPI
    {
        static SceneManagerAPI s_DefaultAPI = new SceneManagerAPI();
        // Internal code must use ActiveAPI over overrideAPI to properly fallback to default api handling
        internal static SceneManagerAPI ActiveAPI => overrideAPI ?? s_DefaultAPI;

        public static SceneManagerAPI overrideAPI { get; set; }

        protected internal SceneManagerAPI() {}
        protected internal virtual int GetNumScenesInBuildSettings() => SceneManagerAPIInternal.GetNumScenesInBuildSettings();
        protected internal virtual Scene GetSceneByBuildIndex(int buildIndex) => SceneManagerAPIInternal.GetSceneByBuildIndex(buildIndex);
        protected internal virtual AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame) =>
            SceneManagerAPIInternal.LoadSceneAsyncNameIndexInternal(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);
        protected internal virtual AsyncOperation UnloadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess) =>
            SceneManagerAPIInternal.UnloadSceneNameIndexInternal(sceneName, sceneBuildIndex, immediately, options, out outSuccess);
        protected internal virtual AsyncOperation LoadFirstScene(bool mustLoadAsync) => null;
    }

    [NativeHeader("Runtime/Export/SceneManager/SceneManager.bindings.h")]
    [RequiredByNativeCode]
    public partial class SceneManager
    {
        static internal bool s_AllowLoadScene = true;

        public static int sceneCount
        {
            [NativeHeader("Runtime/SceneManager/SceneManager.h")]
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetSceneCount")]
            set;
            get;
        }

        public extern static int loadedSceneCount
        {
            [NativeHeader("Runtime/SceneManager/SceneManager.h")]
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetLoadedSceneCount")]
            get;
        }

        public static int sceneCountInBuildSettings
        {
            get { return SceneManagerAPI.ActiveAPI.GetNumScenesInBuildSettings(); }
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        internal static extern bool CanSetAsActiveScene(Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static Scene GetActiveScene()
        {
            foreach (var pair in sence2LevelDictionary)
            {
                if (pair.Value == Unreal.GWorld.PersistentLevel)
                {
                    return pair.Key;
                }
            }
            
            int identifier = sceneCount++;
            Scene result = new Scene(identifier);
            result.name = ASceneUtil.GetLevelName(Unreal.GWorld.PersistentLevel, Unreal.GWorld.StreamingLevelsPrefix).ToString();
            result.path = ASceneUtil.GetLevelPathInGame(Unreal.GWorld.PersistentLevel).ToString() + "/" + ASceneUtil.GetLevelName(Unreal.GWorld.PersistentLevel, Unreal.GWorld.StreamingLevelsPrefix).ToString();
            if (!sence2LevelDictionary.TryAdd(result, Unreal.GWorld.PersistentLevel))
            {
                UKismetSystemLibrary.PrintString(Unreal.GWorld, "record current Scene failed!");
            }
            return result;
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static bool SetActiveScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }
            // 验证场景是否已加载（通过场景-关卡映射表确认）
            if (!sence2LevelDictionary.TryGetValue(scene, out ULevel targetLevel))
            {
                return false;
            }
            // 当前活动场景无需重复设置
            Scene currentActiveScene = GetActiveScene();
            if (currentActiveScene.name == scene.name)
            {
                return true;
            }
            try
            {
                // 获取当前世界上下文并验证
                if (World == null)
                {
                    return false;
                }
                // 通过UWorld.CurrentLevel属性设置活动关卡
                World.CurrentLevel = targetLevel;
                // 触发活动场景变更事件
                Internal_ActiveSceneChanged(currentActiveScene, scene);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByPath(string scenePath);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static Scene GetSceneByName(string name)
        {
            foreach (Scene scene in sence2LevelDictionary.Keys)
            {
                if (scene.name == name)
                {
                    return scene;
                }
            }

            int identifier = -1;
            string scenePath = String.Empty;
            ULevel targetLevel = null;
            foreach (ULevel level in ASceneUtil.GetLevels(World))
            {
                if (ASceneUtil.GetLevelName(level, World.StreamingLevelsPrefix).ToString() == name)
                {
                    identifier = sceneCount++;
                    scenePath = ASceneUtil.GetLevelPathInGame(level).ToString() + "/" + ASceneUtil.GetLevelName(level, World.StreamingLevelsPrefix).ToString();
                    targetLevel = level;
                    break;
                }
            }

            if (identifier == -1)
            {
                UKismetSystemLibrary.PrintString(World, "Scene not found");
                return new Scene(-1);
            }
            Scene result = new Scene(identifier);
            result.name = name;
            result.path = scenePath;
            
            if (!sence2LevelDictionary.TryAdd(result, targetLevel))
            {
                UKismetSystemLibrary.PrintString(World, "record current Scene failed!");
            }
            return result;
        }

        public static Scene GetSceneByBuildIndex(int buildIndex)
        {
            return SceneManagerAPI.ActiveAPI.GetSceneByBuildIndex(buildIndex);
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static extern Scene GetSceneAt(int index);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static extern Scene CreateScene([NotNull] string sceneName, CreateSceneParameters parameters);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        private static extern bool UnloadSceneInternal(Scene scene, UnloadSceneOptions options);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        private static AsyncOperation UnloadSceneAsyncInternal(Scene scene, UnloadSceneOptions options)
        {
            if (!scene.IsValid())
            {
                Debug.LogError("Cannot unload invalid scene.");
                return CreateFailedAsyncOperation();
            }

            if (!sence2LevelDictionary.TryGetValue(scene, out ULevel targetLevel))
            {
                Debug.LogError($"Scene '{scene.name}' not found in level dictionary.");
                return CreateFailedAsyncOperation();
            }

            // 防止卸载当前活动场景
            if (targetLevel.GetName() == World.CurrentLevel.GetName())
            {
                Debug.LogError("Cannot unload the active scene (CurrentLevel).");
                return CreateFailedAsyncOperation();
            }
            try
            {
                String levelName = ASceneUtil.GetLevelName(targetLevel, World.StreamingLevelsPrefix).ToString();
               
                // 创建异步操作对象
                AsyncOperation asyncOp = new AsyncOperation();
                asyncOp.loadSceneName = levelName;
                
                // 直接执行卸载操作
                FLatentActionInfo latentInfo = new FLatentActionInfo
                {
                    UUID = FGuid.NewGuid().A
                };

                // 异步卸载关卡
                UGameplayStatics.UnloadStreamLevel(World, levelName, latentInfo, false);
        
                asyncOp.InvokeCompletionEvent();
                
                sence2LevelDictionary.TryRemove(scene, out _);
                Internal_SceneUnloaded(scene);

                return asyncOp;
                
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return CreateFailedAsyncOperation();
            }
        }
        private static AsyncOperation CreateFailedAsyncOperation()
        {
            AsyncOperation op = new AsyncOperation();
            op.MarkAsFailed();
            return op;
        }
        private static AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
        {
            
            if (!s_AllowLoadScene)
            {
                return null;
            }

            return SceneManagerAPI.ActiveAPI.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters,
                mustCompleteNextFrame);
        }

        private static AsyncOperation UnloadSceneNameIndexInternal(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess)
        {
            if (!s_AllowLoadScene)
            {
                outSuccess = false;
                return null;
            }

            return SceneManagerAPI.ActiveAPI.UnloadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, immediately, options, out outSuccess);
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static extern void MergeScenes(Scene sourceScene, Scene destinationScene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static void MoveGameObjectToScene([NotNull] GameObject go, Scene scene)
        {
            AActor actor = go.actor;
            ULevel targetLevel = sence2LevelDictionary[scene];
            ASceneUtil.RemoveActorFromLevel(actor.GetLevel(), actor);
            ASceneUtil.AddActorToLevel(targetLevel, actor);
        }

            [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        private extern static void MoveGameObjectsToSceneByInstanceId(IntPtr instanceIds, int instanceCount, Scene scene);

        public static unsafe void MoveGameObjectsToScene(NativeArray<int> instanceIDs, Scene scene)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));

            if (instanceIDs.Length == 0)
                return;

            MoveGameObjectsToSceneByInstanceId((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), instanceIDs.Length, scene);
        }

        [RequiredByNativeCode]
        internal static AsyncOperation LoadFirstScene_Internal(bool async)
        {
            return SceneManagerAPI.ActiveAPI.LoadFirstScene(async);
        }

    }
}