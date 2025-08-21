// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [Flags]
    public enum DrivenTransformProperties
    {
        None = 0,
        All = ~None,
        AnchoredPositionX = 1 << 1,
        AnchoredPositionY = 1 << 2,

        AnchoredPositionZ = 1 << 3,
        Rotation = 1 << 4,
        ScaleX = 1 << 5,
        ScaleY = 1 << 6,
        ScaleZ = 1 << 7,
        AnchorMinX = 1 << 8,
        AnchorMinY = 1 << 9,
        AnchorMaxX = 1 << 10,
        AnchorMaxY = 1 << 11,
        SizeDeltaX = 1 << 12,
        SizeDeltaY = 1 << 13,
        PivotX = 1 << 14,
        PivotY = 1 << 15,

        AnchoredPosition = AnchoredPositionX | AnchoredPositionY,

        AnchoredPosition3D = AnchoredPositionX | AnchoredPositionY | AnchoredPositionZ,
        Scale = ScaleX | ScaleY | ScaleZ,
        AnchorMin = AnchorMinX | AnchorMinY,
        AnchorMax = AnchorMaxX | AnchorMaxY,
        Anchors = AnchorMin | AnchorMax,
        SizeDelta = SizeDeltaX | SizeDeltaY,
        Pivot = PivotX | PivotY
    }

    [NativeHeader("Editor/Src/Animation/AnimationModeSnapshot.h")]
    [NativeHeader("Editor/Src/Undo/PropertyUndoManager.h")]
    public struct DrivenRectTransformTracker
    {
        private List<RectTransform> m_Tracked;

        internal static bool CanRecordModifications()
        {
            // The DrivenRectTransformTracker should not record undo by itself but always
            // as part of another undoable action. We therefore prevent recording undo if there
            // is no undo recordings yet. This fixes many situations where the main scene is
            // marked as dirty without user interaction as a side effect the Add() and Clear()
            // below being called either when rebuilding auto layouted during ui rendering or when
            // layoutgroup is being disabled when merging Prefab instances. Fixes case 1268783.
            return !IsInAnimationMode() && (IsUndoingOrRedoing() || HasUndoRecordObjects());
        }

        [FreeFunction("GetAnimationModeSnapshot().IsInAnimationMode")]
        static extern bool IsInAnimationMode();

        [FreeFunction("GetPropertyUndoManager().HasRecordings")]
        static extern bool HasUndoRecordObjects();

        [FreeFunction("GetPropertyUndoManager().IsUndoingOrRedoing")]
        static extern bool IsUndoingOrRedoing();

        private static bool s_BlockUndo;

        public static void StopRecordingUndo() { s_BlockUndo = true; }

        public static void StartRecordingUndo() { s_BlockUndo = false; }

        public void Add(Object driver, RectTransform rectTransform, DrivenTransformProperties drivenProperties)
        {
            if (m_Tracked == null)
                m_Tracked = new List<RectTransform>();

            if (!Application.isPlaying && CanRecordModifications() && !s_BlockUndo)
                RuntimeUndo.RecordObject(rectTransform, "Driving RectTransform");

            // Ensure the driven properties are cleared if the driver is different.
            if (rectTransform.drivenByObject != driver)
                rectTransform.drivenProperties = DrivenTransformProperties.None;

            rectTransform.drivenByObject = driver;
            rectTransform.drivenProperties = rectTransform.drivenProperties | drivenProperties;

            m_Tracked.Add(rectTransform);
        }

        [Obsolete("revertValues parameter is ignored. Please use Clear() instead.")]
        public void Clear(bool revertValues)
        {
            Clear();
        }

        public void Clear()
        {
            if (m_Tracked != null)
            {
                for (int i = 0; i < m_Tracked.Count; i++)
                {
                    if (m_Tracked[i] != null)
                    {
                        if (!Application.isPlaying && CanRecordModifications() && !s_BlockUndo)
                            RuntimeUndo.RecordObject(m_Tracked[i], "Driving RectTransform");

                        m_Tracked[i].drivenByObject = null;
                        m_Tracked[i].drivenProperties = DrivenTransformProperties.None;
                    }
                }
                m_Tracked.Clear();
            }
        }
    }
}
