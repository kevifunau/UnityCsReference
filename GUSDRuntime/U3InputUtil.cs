using Script.CoreUObject;
using Script.UnrealCSharp;
using UnityEngine;

namespace GUSD.Utils;

public class U3InputUtil
{
    public static FString GetU1KeyNameByKeyCode(KeyCode keyCode)
    {
        return keyCode switch
        {
            // 特殊键
            KeyCode.None => string.Empty,
            
            // 基础功能键
            KeyCode.Backspace => "BackSpace",
            KeyCode.Delete => "Delete",
            KeyCode.Tab => "Tab",
            KeyCode.Clear => "Clear",
            KeyCode.Return => "Enter",
            KeyCode.Pause => "Pause",
            KeyCode.Escape => "Escape",
            KeyCode.Space => "SpaceBar",
            
            // 导航键
            KeyCode.PageUp => "PageUp",
            KeyCode.PageDown => "PageDown",
            KeyCode.End => "End",
            KeyCode.Home => "Home",
            KeyCode.LeftArrow => "Left",
            KeyCode.UpArrow => "Up",
            KeyCode.RightArrow => "Right",
            KeyCode.DownArrow => "Down",
            KeyCode.Insert => "Insert",
            
            // 字母键
            KeyCode.A => "A",
            KeyCode.B => "B",
            KeyCode.C => "C",
            KeyCode.D => "D",
            KeyCode.E => "E",
            KeyCode.F => "F",
            KeyCode.G => "G",
            KeyCode.H => "H",
            KeyCode.I => "I",
            KeyCode.J => "J",
            KeyCode.K => "K",
            KeyCode.L => "L",
            KeyCode.M => "M",
            KeyCode.N => "N",
            KeyCode.O => "O",
            KeyCode.P => "P",
            KeyCode.Q => "Q",
            KeyCode.R => "R",
            KeyCode.S => "S",
            KeyCode.T => "T",
            KeyCode.U => "U",
            KeyCode.V => "V",
            KeyCode.W => "W",
            KeyCode.X => "X",
            KeyCode.Y => "Y",
            KeyCode.Z => "Z",
            
            // 数字键
            KeyCode.Alpha0 => "Zero",
            KeyCode.Alpha1 => "One",
            KeyCode.Alpha2 => "Two",
            KeyCode.Alpha3 => "Three",
            KeyCode.Alpha4 => "Four",
            KeyCode.Alpha5 => "Five",
            KeyCode.Alpha6 => "Six",
            KeyCode.Alpha7 => "Seven",
            KeyCode.Alpha8 => "Eight",
            KeyCode.Alpha9 => "Nine",
            
            // 小键盘
            KeyCode.Keypad0 => "NumPadZero",
            KeyCode.Keypad1 => "NumPadOne",
            KeyCode.Keypad2 => "NumPadTwo",
            KeyCode.Keypad3 => "NumPadThree",
            KeyCode.Keypad4 => "NumPadFour",
            KeyCode.Keypad5 => "NumPadFive",
            KeyCode.Keypad6 => "NumPadSix",
            KeyCode.Keypad7 => "NumPadSeven",
            KeyCode.Keypad8 => "NumPadEight",
            KeyCode.Keypad9 => "NumPadNine",
            KeyCode.KeypadPeriod => "Decimal",
            KeyCode.KeypadDivide => "Divide",
            KeyCode.KeypadMultiply => "Multiply",
            KeyCode.KeypadMinus => "Subtract",
            KeyCode.KeypadPlus => "Add",
            KeyCode.KeypadEnter => "Enter",
            KeyCode.KeypadEquals => "Equals",
            
            // 功能键
            KeyCode.F1 => "F1",
            KeyCode.F2 => "F2",
            KeyCode.F3 => "F3",
            KeyCode.F4 => "F4",
            KeyCode.F5 => "F5",
            KeyCode.F6 => "F6",
            KeyCode.F7 => "F7",
            KeyCode.F8 => "F8",
            KeyCode.F9 => "F9",
            KeyCode.F10 => "F10",
            KeyCode.F11 => "F11",
            KeyCode.F12 => "F12",
            KeyCode.F13 => "F11",
            KeyCode.F14 => "F12",
            KeyCode.F15 => "F12",
            
            // 修饰键
            KeyCode.RightShift => "RightShift",
            KeyCode.LeftShift => "LeftShift",
            KeyCode.RightControl => "RightControl",
            KeyCode.LeftControl => "LeftControl",
            KeyCode.RightAlt => "RightAlt",
            KeyCode.LeftAlt => "LeftAlt",
            KeyCode.RightCommand => "RightCommand",
            KeyCode.LeftCommand => "LeftCommand",
            KeyCode.LeftWindows => "LeftCommand",
            KeyCode.RightWindows => "RightCommand",
            
            // 鼠标按钮
            KeyCode.Mouse0 => "LeftMouseButton",
            KeyCode.Mouse1 => "RightMouseButton",
            KeyCode.Mouse2 => "MiddleMouseButton",
            KeyCode.Mouse3 => "ThumbMouseButton",
            KeyCode.Mouse4 => "ThumbMouseButton2",
            
            // 符号键
            KeyCode.Comma => "Comma",
            KeyCode.Period => "Period",
            KeyCode.Slash => "Slash",
            KeyCode.BackQuote => "Tilde",
            KeyCode.LeftBracket => "LeftBracket",
            KeyCode.Backslash => "Backslash",
            KeyCode.RightBracket => "RightBracket",
            KeyCode.Quote => "Quote",
            KeyCode.Semicolon => "Semicolon",
            KeyCode.Equals => "Equals",
            KeyCode.Minus => "Hyphen",
            
            // 游戏手柄通用按钮
            KeyCode.JoystickButton0 => "Gamepad_FaceButton_Bottom",
            KeyCode.JoystickButton1 => "Gamepad_FaceButton_Right",
            KeyCode.JoystickButton2 => "Gamepad_FaceButton_Left",
            KeyCode.JoystickButton3 => "Gamepad_FaceButton_Top",
            KeyCode.JoystickButton4 => "Gamepad_LeftShoulder",
            KeyCode.JoystickButton5 => "Gamepad_RightShoulder",
            KeyCode.JoystickButton6 => "Gamepad_LeftTrigger",
            KeyCode.JoystickButton7 => "Gamepad_RightTrigger",
            KeyCode.JoystickButton8 => "Gamepad_Special_Left",
            KeyCode.JoystickButton9 => "Gamepad_Special_Right",
            KeyCode.JoystickButton10 => "Gamepad_LeftThumbstick",
            KeyCode.JoystickButton11 => "Gamepad_RightThumbstick",
            
            // 默认情况
            _ => keyCode.ToString()
        };
    }

    public static FString GetU1KeyNameByKeyString(string keyName)
    {
        return keyName switch
        {
            // 特殊键
            "" => string.Empty,
            
            // 基础功能键
            "backspace" => "BackSpace",
            "delete" => "Delete",
            "tab" => "Tab",
            "clear" => "Clear",
            "return" => "Enter",
            "pause" => "Pause",
            "escape" => "Escape",
            "space" => "SpaceBar",
            
            // 导航键
            "page up" => "PageUp",
            "page down" => "PageDown",
            "end" => "End",
            "home" => "Home",
            "left" => "Left",
            "up" => "Up",
            "right" => "Right",
            "down" => "Down",
            "insert" => "Insert",
            
            // 字母键
            "a" => "A",
            "b" => "B",
            "c" => "C",
            "d" => "D",
            "e" => "E",
            "f" => "F",
            "g" => "G",
            "h" => "H",
            "i" => "I",
            "j" => "J",
            "k" => "K",
            "l" => "L",
            "m" => "M",
            "n" => "N",
            "o" => "O",
            "p" => "P",
            "q" => "Q",
            "r" => "R",
            "s" => "S",
            "t" => "T",
            "u" => "U",
            "v" => "V",
            "w" => "W",
            "x" => "X",
            "y" => "Y",
            "z" => "Z",
            
            // 数字键
            "0" => "Zero",
            "1" => "One",
            "2" => "Two",
            "3" => "Three",
            "4" => "Four",
            "5" => "Five",
            "6" => "Six",
            "7" => "Seven",
            "8" => "Eight",
            "9" => "Nine",
            
            // 小键盘
            "[0]" => "NumPadZero",
            "[1]" => "NumPadOne",
            "[2]" => "NumPadTwo",
            "[3]" => "NumPadThree",
            "[4]" => "NumPadFour",
            "[5]" => "NumPadFive",
            "[6]" => "NumPadSix",
            "[7]" => "NumPadSeven",
            "[8]" => "NumPadEight",
            "[9]" => "NumPadNine",
            "[.]" => "Decimal",
            "[/]" => "Divide",
            "[*]" => "Multiply",
            "[-]" => "Subtract",
            "[+]" => "Add",
            "enter" => "Enter",
            "[=]" => "Equals",
            
            // 功能键
            "f1 "=> "F1",
            "f2 "=> "F2",
            "f3 "=> "F3",
            "f4 "=> "F4",
            "f5 "=> "F5",
            "f6 "=> "F6",
            "f7 "=> "F7",
            "f8 "=> "F8",
            "f9 "=> "F9",
            "f10" => "F10",
            "f11" => "F11",
            "f12" => "F12",
            "f13" => "F11",
            "f14" => "F12",
            "f15" => "F12",
            
            // 修饰键
            "right shift" => "RightShift",
            "left shift" => "LeftShift",
            "right ctrl" => "RightControl",
            "left ctrl" => "LeftControl",
            "right alt" => "RightAlt",
            "left alt" => "LeftAlt",
            "right cmd" => "RightCommand",
            "left cmd" => "LeftCommand",
            "left windows" => "LeftCommand",
            "right windows" => "RightCommand",
            
            // 鼠标按钮
            "mouse 0" => "LeftMouseButton",
            "mouse 1" => "RightMouseButton",
            "mouse 2" => "MiddleMouseButton",
            "mouse 3" => "ThumbMouseButton",
            "mouse 4" => "ThumbMouseButton2",
            
            // 符号键
            "," => "Comma",
            "." => "Period",
            "/" => "Slash",
            "`" => "Tilde",
            "[" => "LeftBracket",
            "\\" => "Backslash",
            "]" => "RightBracket",
            "'"=> "Quote",
            ";" => "Semicolon",
            "=" => "Equals",
            "-" => "Hyphen",
            
            // 默认情况
            _ => keyName.ToString()
        };
    }

    public static FString GetU1KeyNameByMouseButton(int button)
    {
        return button switch
        {
            0 => "LeftMouseButton",
            1 => "RightMouseButton",
            2 => "MiddleMouseButton",
            3 => "ThumbMouseButton",
            4 => "ThumbMouseButton2",

            // 默认情况
            _ => button.ToString()
        };
    }

    public static FString GetU1KeyNameByButtonString(string buttonName)
    {
        return buttonName switch
        {
            // 特殊键
            "Jump" => "SpaceBar",
            "Submit" => "Enter",
            "Cancel" => "Escape",
            "Fire1" => "LeftControl",
            "Fire2" => "LeftAlt",
            "Fire3" => "LeftShift",

            // 默认情况
            _ => buttonName.ToString()
        };
    }
    
    public static Touch ConvertToTouch(FTouchData touchData)
    {
        return new Touch
        {
            fingerId = touchData.FingerIndex,
            position = U3VectorUtil.GetU3ScreenPointFromU1(touchData.Position),
            rawPosition = U3VectorUtil.GetU3ScreenPointFromU1(touchData.Position), 
            deltaPosition = new Vector2((float)touchData.DeltaPosition.X, (float)touchData.DeltaPosition.Y),
            deltaTime = Time.deltaTime,
            phase = (TouchPhase)touchData.Phase,
            pressure = 1.0f,
            type = TouchType.Direct
        };
    }
}