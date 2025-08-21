// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using Script.UnrealCSharp;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Scripting/TextAsset.h")]
    public partial class TextAsset : Object
    {
        public string str;
        
        public static TextAsset Load(string path)
        {
            var str = UFileUtil.LoadFileToString(path);
            var context = str.ToString();
            return new TextAsset(context);
        }
        
        // The raw bytes of the text asset. (RO)
        public extern byte[] bytes { get; }

        extern byte[] GetPreviewBytes(int maxByteCount);

        static void Internal_CreateInstance([Writable] TextAsset self, string text)
        {
            self.str = text;
        }

        extern IntPtr GetDataPtr();
        extern long GetDataSize();

        static extern AtomicSafetyHandle GetSafetyHandle(TextAsset self);
    }
}
