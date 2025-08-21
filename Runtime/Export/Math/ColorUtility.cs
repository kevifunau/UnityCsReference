// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Script.CoreUObject;
using System.Drawing;

namespace UnityEngine
{
    public partial class ColorUtility
    {
        public static bool TryParseHtmlString(string htmlString, out Color color)
        {
            // default color
            color = new Color(0, 0, 0, 0);
            if (string.IsNullOrEmpty(htmlString))
            {
                return false;
            }

            try
            {
                if (htmlString[0] == '#')
                {
                    var fColor = FColor.FromHex(htmlString);
                    color = new Color(
                        fColor.R / 255f,
                        fColor.G / 255f,
                        fColor.B / 255f,
                        fColor.A / 255f
                    );
                    return true;
                }

                if (ColorTranslator.FromHtml(htmlString) is { IsKnownColor: true } systemColor)
                {
                    color = new Color(
                        systemColor.R / 255f,
                        systemColor.G / 255f,
                        systemColor.B / 255f,
                        systemColor.A / 255f
                    );
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Color parse error: {htmlString} - {ex.Message}");
            }

            return false;
        }

        public static string ToHtmlStringRGB(Color color)
        {
            // Round to int to prevent precision issues that, for example cause values very close to 1 to become FE instead of FF (case 770904).
            Color32 col32 = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255), 0, 255),
                1);

            return UnityString.Format("{0:X2}{1:X2}{2:X2}", col32.r, col32.g, col32.b);
        }

        public static string ToHtmlStringRGBA(Color color)
        {
            // Round to int to prevent precision issues that, for example cause values very close to 1 to become FE instead of FF (case 770904).
            Color32 col32 = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * 255), 0, 255));

            return UnityString.Format("{0:X2}{1:X2}{2:X2}{3:X2}", col32.r, col32.g, col32.b, col32.a);
        }
    }
}
