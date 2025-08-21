// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Math/ColorUtility.h")]
    public partial class ColorUtility
    {
        [FreeFunction("TryParseHtmlColor", true)]
        // todo:当前定义一个简单接口规避未实现报错。具体实现后，可以修改以下内容
        public static bool DoTryParseHtmlColor(string htmlString, out Color32 color)
        {
            if (TryParseCustomColor(htmlString, out color))
            {
                return true;
            }

            color = Color.magenta;
            return false;
        }
        private static bool TryParseCustomColor(string name, out Color32 color)
        {
            switch (name.ToLower())
            {
                case "gold": color = new Color(1f, 0.84f, 0f); return true;
                case "pink": color = new Color(1f, 0.75f, 0.8f); return true;
                default: color = Color.clear; return false;
            }
        }
    }
}
