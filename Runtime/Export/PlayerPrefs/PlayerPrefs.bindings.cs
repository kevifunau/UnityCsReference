// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Script.Engine;
using Script.UnrealCSharp;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // This exception is thrown by the [[PlayerPrefs]] class in the Web player if the preference file would exceed the allotted storage space when setting a value.
    public class PlayerPrefsException : Exception
    {
        //*undocumented*
        public PlayerPrefsException(string error) : base(error)
        {
        }
    }

    // Stores and accesses player preferences between game sessions.
    [NativeHeader("Runtime/Utilities/PlayerPrefs.h")]
    public class PlayerPrefs
    {
        [NativeMethod("SetInt")]
        private static bool TrySetInt(string key, int value)
        {
            UGUSDPlayerSaveGame loadedGame = GetSaveGameByKey(key);
            if (loadedGame != null)
            {
                InitSaveGame(loadedGame);
                loadedGame.ToSaveInt = value;
                AddToSave(key);
                return true;
            }

            return false;
        }

        [NativeMethod("SetFloat")]
        private static bool TrySetFloat(string key, float value)
        {
            UGUSDPlayerSaveGame loadedGame = GetSaveGameByKey(key);
            if (loadedGame != null)
            {
                InitSaveGame(loadedGame);
                loadedGame.ToSaveFloat = value;
                AddToSave(key);
                return true;
            }

            return false;
        }

        [NativeMethod("SetString")]
        private static bool TrySetSetString(string key, string value)
        {
            UGUSDPlayerSaveGame loadedGame = GetSaveGameByKey(key);
            if (loadedGame != null)
            {
                InitSaveGame(loadedGame);
                loadedGame.ToSaveString = value;
                AddToSave(key);
                return true;
            }

            return false;
        }

        // Sets the value of the preference identified by /key/.
        public static void SetInt(string key, int value)
        {
            if (!TrySetInt(key, value)) throw new PlayerPrefsException("Could not store preference value");
        }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public static int GetInt(string key, int defaultValue)
        {
            UGUSDPlayerSaveGame loadedGame = GetSaveGameByKey(key);
            if (loadedGame != null)
            {
                return loadedGame.ToSaveInt;
            }

            return defaultValue;
        }

        public static int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        // Sets the value of the preference identified by /key/.
        public static void SetFloat(string key, float value)
        {
            if (!TrySetFloat(key, value)) throw new PlayerPrefsException("Could not store preference value");
        }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public static float GetFloat(string key, float defaultValue)
        {
            UGUSDPlayerSaveGame loadedGame = GetSaveGameByKey(key);
            if (loadedGame != null)
            {
                return loadedGame.ToSaveFloat;
            }

            return defaultValue;
        }

        public static float GetFloat(string key)
        {
            return GetFloat(key, 0.0f);
        }

        // Sets the value of the preference identified by /key/.
        public static void SetString(string key, string value)
        {
            if (!TrySetSetString(key, value)) throw new PlayerPrefsException("Could not store preference value");
        }


        // Returns the value corresponding to /key/ in the preference file if it exists.
        public static string GetString(string key, string defaultValue)
        {
            UGUSDPlayerSaveGame loadedGame = GetSaveGameByKey(key);
            if (loadedGame != null)
            {
                return loadedGame.ToSaveString.ToString();
            }

            return defaultValue;
        }

        public static string GetString(string key)
        {
            return GetString(key, "");
        }

        // Returns true if /key/ exists in the preferences.
        public static bool HasKey(string key)
        {
            GetKeys();
            return _mKeys.Contains(key);
        }


        private static List<string> _mToDelKeys = [];
        // Removes /key/ and its corresponding value from the preferences.
        public static void DeleteKey(string key)
        {
            if (HasKey(key))
            {
                _mKeys.Remove(key);
                _mToDelKeys.Add(key);
                _mKeysNeedSave = true;
                _mLoadedDic.Remove(key);
                _mToSaveKeys.Remove(key);
                Save();
            }
        }

        // Removes all keys and values from the preferences. Use with caution.
        [NativeMethod("DeleteAllWithCallback")]
        public static void DeleteAll()
        {
            GetKeys();
            if (_mKeys.Count > 0)
            {
                _mToDelKeys = _mKeys;
                _mKeys = [];
                _mKeysNeedSave = true;
                _mLoadedDic.Clear();
                _mToSaveKeys.Clear();
                Save();
            }
        }

        // Writes all modified preferences to disk.
        [NativeMethod("Sync")]
        public static void Save()
        {
            if (_mToSaveKeys.Count > 0)
            {
                foreach (var key in _mToSaveKeys)
                {
                    UGUSDPlayerSaveGame saveGame = GetSaveGameByKey(key);
                    SaveInst(saveGame,key);
                }

                _mToSaveKeys.Clear();
            }

            if (_mKeysNeedSave)
            {
                _mKeysNeedSave = false;
                if (_mKeysSaveGame != null)
                {
                    _mKeysSaveGame.ToSaveArrayString = [];
                    foreach (var key in _mKeys)
                    {
                        _mKeysSaveGame.ToSaveArrayString.Add(key);
                    }
                    SaveInst(_mKeysSaveGame,MKeyListKey);
                }
            }

            if (_mToDelKeys.Count > 0)
            {
                foreach (var key in _mToDelKeys)
                {
                    UGameplayStatics.DeleteGameInSlot(key, MSaveUserIndex);
                }

                _mToDelKeys.Clear();
            }
        }

        private static void SaveInst(UGUSDPlayerSaveGame saveGame, string key)
        {
            // 同步保存 可能导致卡顿
            // UGameplayStatics::SaveGameToSlot(saveGame, key, MSaveUserIndex);
            // 异步保存
            saveGame.Save(key,MSaveUserIndex);
        }

        // NOTE: DisposeSentinel requires access to EditorPrefs but from UnityEngine.dll
        //       (Which cant access UnityEditor.dll)
        //       So we expose the API here. Internal only, users should use the normal EditorPrefs class

        [StaticAccessor("EditorPrefs", StaticAccessorType.DoubleColon)]
        [NativeMethod("SetInt")]
        extern internal static void EditorPrefsSetInt(string key, int value);

        [StaticAccessor("EditorPrefs", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetInt")]
        extern internal static int EditorPrefsGetInt(string key, int defaultValue);

        private const int MSaveUserIndex = 100;

        private const string MKeyListKey = "__KeyList";

        private static List<string> _mKeys = [];

        private static bool _mKeysGot = false;

        private static bool _mKeysNeedSave = false;
        
        private static UGUSDPlayerSaveGame _mKeysSaveGame;

        private static void GetKeys()
        {
            if (_mKeysGot) return;
            _mKeysGot = true;

            UGUSDPlayerSaveGame loadedGame =
                (UGUSDPlayerSaveGame)UGameplayStatics.LoadGameFromSlot(MKeyListKey, MSaveUserIndex);
            if (loadedGame != null)
            {
                foreach (var key in loadedGame.ToSaveArrayString)
                {
                    _mKeys.Add(key.ToString());
                }
                    
                _mKeysSaveGame = loadedGame;
            }
            else
            {
                _mKeysSaveGame = (UGUSDPlayerSaveGame)UGameplayStatics.CreateSaveGameObject(UGUSDPlayerSaveGame.StaticClass());
                InitSaveGame(_mKeysSaveGame);
            }
        }

        private static Dictionary<string, UGUSDPlayerSaveGame> _mLoadedDic = new Dictionary<string, UGUSDPlayerSaveGame>();

        private static UGUSDPlayerSaveGame GetSaveGameByKey(string key)
        {
            GetKeys();
            // 字典里有
            if (_mLoadedDic.ContainsKey(key)) return _mLoadedDic[key];

            UGUSDPlayerSaveGame saveGane = null;
            if (_mKeys.Contains(key))
            {
                // 从本地读
                saveGane = (UGUSDPlayerSaveGame)UGameplayStatics.LoadGameFromSlot(MKeyListKey, MSaveUserIndex);
            }
            else
            {
                _mKeys.Add(key);
                _mKeysNeedSave = true;
            }

            if (saveGane == null)
            {
                // 都没有实例化一个新的
                saveGane = (UGUSDPlayerSaveGame)UGameplayStatics.CreateSaveGameObject(UGUSDPlayerSaveGame.StaticClass());
                InitSaveGame(saveGane);
            }

            _mLoadedDic[key] = saveGane;

            return saveGane;
        }

        private static void InitSaveGame(UGUSDPlayerSaveGame saveGane)
        {
            saveGane.ToSaveInt = 0;
            saveGane.ToSaveFloat = 0.0f;
            saveGane.ToSaveString = "";
            saveGane.ToSaveArrayString = [];
        }

        private static List<string> _mToSaveKeys = [];

        private static void AddToSave(string key)
        {
            if (!_mToSaveKeys.Contains(key))
            {
                _mToSaveKeys.Add(key);
            }
        }
    }
}