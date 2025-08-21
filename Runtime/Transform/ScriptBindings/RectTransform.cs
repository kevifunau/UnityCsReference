using Script.Dynamic;
using Script.UMG;
using Script.Slate;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Script.UnrealCSharp;
using GUSD.Utils;
using Script.CoreUObject;
using Script.UtuRuntime;

namespace UnityEngine;

[U3Exported(false)]
public sealed partial class RectTransform : Transform
{
    private UCanvasPanel m_targetCanvas;

    public UCanvasPanel targetCanvas
    {
        get
        {
            if (m_targetCanvas == null)
            {
                var curActor = u1Component.GetOwner();
                var uCanvasPanel = UGUSDUiUtil.GetInstance().FindActorMatchedCanvasPanel(ref curActor);
                InitCanvasPanel(uCanvasPanel);
            }
            return m_targetCanvas;
        }
    }

    public UCanvasPanelSlot m_canvasPanelSlot;

    public UCanvasPanelSlot canvasPanelSlot
    {
        get
        {
            if (m_canvasPanelSlot == null)
            {
                var curActor = u1Component.GetOwner();
                var uCanvasPanel = UGUSDUiUtil.GetInstance().FindActorMatchedCanvasPanel(ref curActor);
                InitCanvasPanel(uCanvasPanel);
            }
            return m_canvasPanelSlot;
        }
    } 
    public FWidgetTransform renderTransform { get; set; }
    public ESlateVisibility visibility;

    public bool m_transform_dirty = false;

    public void InitCanvasPanel(UCanvasPanel canvasPanel)
    {
        if (canvasPanel == null)
        {
            return;
        }
        
        m_targetCanvas = canvasPanel;
        if (m_targetCanvas.Slot is UCanvasPanelSlot slot)
            m_canvasPanelSlot = slot;
        renderTransform = m_targetCanvas.RenderTransform;
        visibility = m_targetCanvas.GetVisibility() == ESlateVisibility.Hidden ? ESlateVisibility.Visible : m_targetCanvas.GetVisibility();
    }
    
    void Update()
    {
        if (m_transform_dirty && targetCanvas != null)
        {
            m_transform_dirty = false;
            var u1RectTransform = ConvertU1RectTransform();
            UGUSDUiUtil.UpdateUICanvasPanel(targetCanvas, u1RectTransform);
        }
    }

    public FGUSDRectTransform U1RectTransformInit()
    {
        m_anchoredPosition = Vector2.zero;
        m_anchorMin = new Vector2(0.5f, 0.5f);
        m_anchorMax = new Vector2(0.5f, 0.5f);
        m_pivot = new Vector2(0.5f, 0.5f);
        m_sizeDelta = new Vector2(100f, 100f);
        m_localScale = Vector3.one;
        offsetMin = Vector2.zero;
        offsetMax = Vector2.zero;
        return ConvertU1RectTransform();
    }

    public FGUSDRectTransform ConvertU1RectTransform()
    {
        var u1RectTransform = new FGUSDRectTransform
        {
            AnchoredPosition =
            {
                X = anchoredPosition.x,
                Y = anchoredPosition.y
            },
            AnchorMin =
            {
                X = anchorMin.x,
                Y = anchorMin.y
            },
            AnchorMax =
            {
                X = anchorMax.x,
                Y = anchorMax.y
            },
            Pivot =
            {
                X = pivot.x,
                Y = pivot.y
            },
            SizeDelta =
            {
                X = sizeDelta.x,
                Y = sizeDelta.y
            },
            Scale =
            {
                X = localScale.x,
                Y = localScale.y,
                Z = localScale.z
            },
            RotationZ = -localEulerAngles.z
        };
        return u1RectTransform;
    }

    private Vector3 m_localPosition;
    public override Vector3 localPosition
    {
        get => m_localPosition;
        set
        {
            m_localPosition = value;
            m_transform_dirty = true;
        }
    }

    private Vector3 m_localScale;
    public override Vector3 localScale
    {
        get => m_localScale;
        set
        {
            m_localScale = value;
            m_transform_dirty = true;
        }
    }

    private Quaternion m_localRotation;
    public override Quaternion localRotation
    {
        get => m_localRotation;
        set
        {
            m_localRotation = value;
            m_transform_dirty = true;
        }
    }
    
    public void SetRenderTransformTranslation()
    {
        renderTransform.Translation = U3VectorUtil.GetFVector2DFromVector2(anchoredPosition);
        targetCanvas.SetRenderTransform(renderTransform);
    }
    
    public void SetAnchors()
    {
        FAnchors anchors = new FAnchors();
        anchors.Maximum = U3VectorUtil.GetFVector2DFromVector2(anchorMax);
        anchors.Minimum = U3VectorUtil.GetFVector2DFromVector2(anchorMin);
        canvasPanelSlot.SetAnchors(anchors);
    }

    public FAnchors GetAnchors()
    {
        return canvasPanelSlot.GetAnchors();
    }
    
    public void SetSize()
    {
        canvasPanelSlot.SetSize(U3VectorUtil.GetFVector2DFromVector2(sizeDelta));
    }
    
    public FVector2D GetSize()
    {
        return canvasPanelSlot.GetSize();
    }
    
    public enum Edge { Left = 0, Right = 1, Top = 2, Bottom = 3 }
    public enum Axis { Horizontal = 0, Vertical = 1 }

    public delegate void ReapplyDrivenProperties(RectTransform driven);
    public static event ReapplyDrivenProperties reapplyDrivenProperties;

    public Rect rect
    {
        get
        {
            // TODO: 当前仅支持宽和高
            UCanvasPanelSlot slot = targetCanvas.Slot as UCanvasPanelSlot;
            if (slot != null)
                return new Rect(0, 0, slot.LayoutData.Offsets.Right, slot.LayoutData.Offsets.Bottom);
            return new Rect(0, 0, 0, 0);
        }
    }

    private Vector2 m_anchorMin;

    public Vector2 anchorMin
    {
        get => m_anchorMin;
        set
        {
            m_anchorMin = value;
            m_transform_dirty = true;
        }
    }
    
    private Vector2 m_anchorMax;

    public Vector2 anchorMax
    {
        get => m_anchorMax;
        set
        {
            m_anchorMax = value;
            m_transform_dirty = true;
        }
    }

    private Vector2 m_anchoredPosition;
    public Vector2 anchoredPosition
    {
        get
        {
            if (anchorMin == anchorMax && pivot == Vector2.one * 0.5f)
                return localPosition;
            
            var curParent = parent;
            if (curParent is RectTransform curParentTransform)
            {
                Vector2 parentSize = curParentTransform.rect.size;
                Vector2 anchorCenter = new Vector2(
                    parentSize.x * (anchorMin.x + anchorMax.x) * 0.5f,
                    parentSize.y * (anchorMin.y + anchorMax.y) * 0.5f
                );
                Vector2 pivotOffset = new Vector2(
                    rect.width * (pivot.x - 0.5f),
                    rect.height * (pivot.y - 0.5f)
                );
                return (Vector2)localPosition - anchorCenter + pivotOffset;
            }
            return  Vector2.zero;
        }
        set
        {
            m_anchoredPosition = value;
            m_transform_dirty = true;
        }
    }
    private Vector2 m_sizeDelta;
    public Vector2 sizeDelta
    {
        get => m_sizeDelta;
        set
        {
            m_sizeDelta = value;
            m_transform_dirty = true;
        }
    }
    private Vector2 m_pivot;

    public Vector2 pivot
    {
        get => m_pivot;
        set
        {
            m_pivot = value;
            m_transform_dirty = true;
        }
    }

    public Vector3 anchoredPosition3D
    {
        get
        {
            Vector2 pos2 = anchoredPosition;
            return new Vector3(pos2.x, pos2.y, localPosition.z);
        }
        set
        {
            anchoredPosition = new Vector2(value.x, value.y);
            Vector3 pos3 = localPosition;
            pos3.z = value.z;
            localPosition = pos3;
        }
    }

    public Vector2 offsetMin
    {
        get
        {
            return anchoredPosition - Vector2.Scale(sizeDelta, pivot);
        }
        set
        {
            Vector2 offset = value - (anchoredPosition - Vector2.Scale(sizeDelta, pivot));
            sizeDelta -= offset;
            anchoredPosition += Vector2.Scale(offset, Vector2.one - pivot);
        }
    }

    public Vector2 offsetMax
    {
        get
        {
            return anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
        }
        set
        {
            Vector2 offset = value - (anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot));
            sizeDelta += offset;
            anchoredPosition += Vector2.Scale(offset, pivot);
        }
    }

    public override void SetSiblingIndex(int index)
    {
        canvasPanelSlot.SetZOrder(index);
    }

    public override int GetSiblingIndex()
    {
        return canvasPanelSlot.GetZOrder();
    }

    extern public Object drivenByObject { get; internal set; } 
    extern internal DrivenTransformProperties drivenProperties { get; set; }

    [NativeMethod("UpdateIfTransformDispatchIsDirty")] public extern void ForceUpdateRectTransforms();

    public void GetLocalCorners(Vector3[] fourCornersArray)
    {
        if (fourCornersArray == null || fourCornersArray.Length < 4)
        {
            Debug.LogError("Calling GetLocalCorners with an array that is null or has less than 4 elements.");
            return;
        }

        Rect tmpRect = rect;
        float x0 = tmpRect.x;
        float y0 = tmpRect.y;
        float x1 = tmpRect.xMax;
        float y1 = tmpRect.yMax;

        fourCornersArray[0] = new Vector3(x0, y0, 0f);
        fourCornersArray[1] = new Vector3(x0, y1, 0f);
        fourCornersArray[2] = new Vector3(x1, y1, 0f);
        fourCornersArray[3] = new Vector3(x1, y0, 0f);
    }

    public void GetWorldCorners(Vector3[] fourCornersArray)
    {
        if (fourCornersArray == null || fourCornersArray.Length < 4)
        {
            Debug.LogError("Calling GetWorldCorners with an array that is null or has less than 4 elements.");
            return;
        }

        GetLocalCorners(fourCornersArray);

        Matrix4x4 mat = transform.localToWorldMatrix;
        for (int i = 0; i < 4; i++)
            fourCornersArray[i] = mat.MultiplyPoint(fourCornersArray[i]);
    }

    public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size)
    {
        int axis = (edge == Edge.Top || edge == Edge.Bottom) ? 1 : 0;
        bool end = (edge == Edge.Top || edge == Edge.Right);

        // Set anchorMin and anchorMax to be anchored to the chosen edge.
        float anchorValue = end ? 1 : 0;
        Vector2 anchor = anchorMin;
        anchor[axis] = anchorValue;
        anchorMin = anchor;
        anchor = anchorMax;
        anchor[axis] = anchorValue;
        anchorMax = anchor;

        // Set size. Since anchors are together, size and sizeDelta means the same in this case.
        Vector2 sizeD = sizeDelta;
        sizeD[axis] = size;
        sizeDelta = sizeD;

        // Set inset.
        Vector2 positionCopy = anchoredPosition;
        positionCopy[axis] = end ? -inset - size * (1 - pivot[axis]) : inset + size * pivot[axis];
        anchoredPosition = positionCopy;
    }

    public void SetSizeWithCurrentAnchors(Axis axis, float size)
    {
        UCanvasPanelSlot slot = targetCanvas.Slot as UCanvasPanelSlot;
        if (slot == null)
            return;
        var targetSize = slot.GetSize();
        if (axis == Axis.Horizontal)
            targetSize.X = size;
        else if (axis == Axis.Vertical)
            targetSize.Y = size;
        slot.SetSize(targetSize);
            
        // 适配VerticalLayoutGroup，如果子节点为UtuVerticalLayoutGroup，也要对子节点相应修改
        foreach (var widget in targetCanvas.GetAllChildren())
        {
            if (widget is UUtuVerticalLayoutGroup && widget.Slot is UCanvasPanelSlot parentSlot)
            {
                parentSlot.SetSize(targetSize);
                return;
            }
        }
    }

    [RequiredByNativeCode]
    internal static void SendReapplyDrivenProperties(RectTransform driven)
    {
        reapplyDrivenProperties?.Invoke(driven);
    }

    // Return rect relative to lower left corner of parent rect
    internal Rect GetRectInParentSpace()
    {
        Rect rectResult = rect;
        Vector2 offset = offsetMin + Vector2.Scale(pivot, rectResult.size);
        if (transform.parent)
        {
            RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
            if (parentRect)
                offset += Vector2.Scale(anchorMin, parentRect.rect.size);
        }

        rectResult.x += offset.x;
        rectResult.y += offset.y;
        return rectResult;
    }

    private Vector2 GetParentSize()
    {
        RectTransform parentRect = parent as RectTransform;
        if (!parentRect)
            return Vector2.zero;
        return parentRect.rect.size;
    }
}