// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Collections;
using GUSD.Utils;
using Script.Dynamic;
using Script.Engine;
using UnityEngine;
using Script.CoreUObject;

namespace UnityEngine;
//*undocumented
internal enum RotationOrder
{
    OrderXYZ,
    OrderXZY,
    OrderYZX,
    OrderYXZ,
    OrderZXY,
    OrderZYX
}

[U3Exported(false)]
// Position, rotation and scale of an object.
[NativeHeader("Configuration/UnityConfigure.h")]
[NativeHeader("Runtime/Transform/Transform.h")]
[NativeHeader("Runtime/Transform/ScriptBindings/TransformScriptBindings.h")]
[RequiredByNativeCode]
public partial class Transform : Component, IEnumerable
{
    public Transform()
    {
    }

    // The position of the transform in world space.
    public Vector3 position
    {
        get  
        {
            if (owner == null)
            {
                return U3VectorUtil.GetU3PositionFromU1(u1Component.RelativeLocation);
            }
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                // TODO: K2_GetActorLocation 的实现可能有问题，导致 transform.position行为与U3不一致
                return U3VectorUtil.GetU3PositionFromU1(owner.K2_GetActorLocation());
            } 
            else 
            {
                return U3VectorUtil.GetU3PositionFromU1(owner.GetParentComponent().K2_GetComponentLocation());;
            }
        }
        set
        {
            var location = U3VectorUtil.GetU1LocationFromU3(value);
            if (owner == null)
            {
                var hitResult = new FHitResult();
                u1Component.K2_SetRelativeLocation(location, false, ref hitResult, true);
                return;
            }
            var result = new FHitResult();
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                owner.K2_SetActorLocation(location, false, ref result, true);
            } 
            else
            {
                owner.GetParentComponent().K2_SetWorldLocation(location, false, ref result, true);
            }
        }
    }

    // Position of the transform relative to the parent transform.
    public virtual Vector3 localPosition
    {
        get
        {
            if (owner == null)
            {
                return U3VectorUtil.GetU3PositionFromU1(u1Component.RelativeLocation);
            }
            var relativeLocation = owner.K2_GetRootComponent().RelativeLocation;
            return U3VectorUtil.GetU3PositionFromU1(relativeLocation);
        }
        set
        {
            var newRelativeLocation = U3VectorUtil.GetU1LocationFromU3(value);
            if (owner == null)
            {
                var hitResult = new FHitResult();
                u1Component.K2_SetRelativeLocation(newRelativeLocation, false, ref hitResult, true);
                return;
            }
            var result = new FHitResult();
            owner.K2_GetRootComponent().K2_SetRelativeLocation(newRelativeLocation, false, ref result, true);
        }
    }

    // Get local euler angles with rotation order specified
    internal extern Vector3 GetLocalEulerAngles(RotationOrder order);

    // Set local euler angles with rotation order specified
    internal extern void SetLocalEulerAngles(Vector3 euler, RotationOrder order);

    // Set local euler hint
    [NativeConditional("UNITY_EDITOR")]
    internal extern void SetLocalEulerHint(Vector3 euler);

    // The rotation as Euler angles in degrees.
    public Vector3 eulerAngles
    {
        get { return rotation.eulerAngles; }
        set { rotation = Quaternion.Euler(value); }
    }

    // The rotation as Euler angles in degrees relative to the parent transform's rotation.
    public Vector3 localEulerAngles
    {
        get { return localRotation.eulerAngles; }
        set { localRotation = Quaternion.Euler(value); }
    }

    // The red axis of the transform in world space.
    public Vector3 right
    {
        get 
        {
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                return U3VectorUtil.GetU3DirectionFromU1(owner.GetActorRightVector());
            } 
            else
            { 
                return U3VectorUtil.GetU3DirectionFromU1(owner.GetParentComponent().GetRightVector());
            } 
        }
        set { rotation = Quaternion.FromToRotation(Vector3.right, value); }
    }

    // The green axis of the transform in world space.
    public Vector3 up
    {
        get
        {
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                return U3VectorUtil.GetU3DirectionFromU1(owner.GetActorUpVector());
            } 
            else
            {
                return U3VectorUtil.GetU3DirectionFromU1(owner.GetParentComponent().GetUpVector());
            }
        } 
        set { rotation = Quaternion.FromToRotation(Vector3.up, value); }
    }

    // The blue axis of the transform in world space.
    public Vector3 forward
    {
        get
        {
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                return U3VectorUtil.GetU3DirectionFromU1(owner.GetActorForwardVector());
            } 
            else
            {
                return U3VectorUtil.GetU3DirectionFromU1(owner.GetParentComponent().GetForwardVector());
            }
        }
        set
        {
            rotation = Quaternion.LookRotation(value);
        }
    }

    // The rotation of the transform in world space stored as a [[Quaternion]].
    public Quaternion rotation
    {
        get
        {
            if (owner == null)
            {
                var u1Quat = u1Component.RelativeRotation.Quaternion();
                return U3QuaternionUtil.ConvertU1QuatToU3(u1Quat);
            }
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                return U3QuaternionUtil.ConvertU1QuatToU3(owner.K2_GetActorRotation().Quaternion());    
            }
            else
            {
                return U3QuaternionUtil.ConvertU1QuatToU3(owner.GetParentComponent().K2_GetComponentRotation().Quaternion());   
            }
        }
        set
        {
            if (owner == null)
            {
                var hitResult = new FHitResult();
                u1Component.K2_SetRelativeRotation(U3QuaternionUtil.GetU1RotatorFromU3(value), false, ref hitResult, true);
                return;
            }
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            { 
                owner.K2_SetActorRotation(U3QuaternionUtil.GetU1RotatorFromU3(value), false);
            }
            else
            {  
                var result = new FHitResult();
                owner.GetParentComponent().K2_SetWorldRotation(U3QuaternionUtil.GetU1RotatorFromU3(value), true,ref result,false);
            }
        }
    }

    // The rotation of the transform relative to the parent transform's rotation.
    public virtual Quaternion localRotation
    {
        get
        {
            if (owner == null)
            {
                var u1Quat = u1Component.RelativeRotation.Quaternion();
                return U3QuaternionUtil.ConvertU1QuatToU3(u1Quat);
            }
            return U3QuaternionUtil.ConvertU1QuatToU3(owner.K2_GetRootComponent().RelativeRotation.Quaternion());
        }
        set
        {
            if (owner == null)
            {
                var hitResult = new FHitResult();
                u1Component.K2_SetRelativeRotation(U3QuaternionUtil.GetU1RotatorFromU3(value), false, ref hitResult, true);
                return;
            }
            var h = new FHitResult();
            owner.K2_GetRootComponent().K2_SetRelativeRotation(U3QuaternionUtil.GetU1RotatorFromU3(value), false, ref h, true);
        }
    }

    // The euler rotation order for this transform
    [NativeConditional("UNITY_EDITOR")]
    internal RotationOrder rotationOrder
    {
        get { return (RotationOrder)GetRotationOrderInternal(); }
        set { SetRotationOrderInternal(value); }
    }

    [NativeConditional("UNITY_EDITOR")]
    [NativeMethod("GetRotationOrder")]
    internal extern int GetRotationOrderInternal();

    [NativeConditional("UNITY_EDITOR")]
    [NativeMethod("SetRotationOrder")]
    internal extern void SetRotationOrderInternal(RotationOrder rotationOrder);

    // The scale of the transform relative to the parent.
    public virtual Vector3 localScale
    {
        get
        {
            if (owner == null)
            {
                return U3VectorUtil.GetU3ScaleFromU1(u1Component.RelativeScale3D);
            }
            var relativeScale3D = owner.K2_GetRootComponent().RelativeScale3D;
            return U3VectorUtil.GetU3ScaleFromU1(relativeScale3D);
        }
        set
        {
            var newRelativeScale = U3VectorUtil.GetU1ScaleFromU3(value);
            if (owner == null)
            {
                u1Component.SetRelativeScale3D(newRelativeScale);
                return;
            }
            owner.K2_GetRootComponent().SetRelativeScale3D(newRelativeScale);
        }
    }

    // The parent of the transform.
    private Transform m_parent;
    public Transform parent
    {
        get
        {
            if (m_parent == null)
            {
                m_parent = GetParent();
            }
            return m_parent;
        }
        set
        {
            if (this is RectTransform)
                Debug.LogWarning(
                    "Parent of RectTransform is being set with parent property. Consider using the SetParent method instead, with the worldPositionStays argument set to false. This will retain local orientation and scale rather than world orientation and scale, which can prevent common UI scaling issues.",
                    this);
            parentInternal = value;
            m_parent = value;
        }
    }

    internal Transform parentInternal
    {
        get { return GetParent(); }
        set { SetParent(value); }
    }

    private Transform GetParent()
    {
        UChildActorComponent childActorComponent = owner.GetParentComponent();
        //actor is not created by one ChildActorComponent
        if (childActorComponent == null)
        {
            var parentActor = owner.GetAttachParentActor();
            if (parentActor == null) return null;

            var parentGO = GameObject.MakeFromActor(parentActor);
            var parentTransform = parentGO.GetComponent<Transform>();
            if (parentTransform == null) throw new Exception(parentActor.GetName() + "must have C# Transform!");

            return parentTransform;
        }
        else
        {
            //actor is created by one ChildActorComponent
            USceneComponent attachParent = childActorComponent.GetAttachParent();
            if (attachParent == null) return null;
            if (!attachParent.IsA<UChildActorComponent>())
            {
                return null;
            }
            UChildActorComponent attachChildActorComponent = (UChildActorComponent)attachParent;
            return GameObject.GetFromActorOrCreate(attachChildActorComponent.ChildActor).transform;
        }
    }

    public void SetParent(Transform p)
    {
        SetParent(p, true);
    }

    public void SetParent(Transform parent, bool worldPositionStays)
    {
        owner.K2_DetachFromActor(EDetachmentRule.KeepWorld, EDetachmentRule.KeepWorld,
            EDetachmentRule.KeepWorld);
        if (parent == null) return;
        EAttachmentRule attachmentRule =
            worldPositionStays ? EAttachmentRule.KeepWorld : EAttachmentRule.SnapToTarget;
        owner.K2_AttachToActor(parent.owner, FName.NAME_None,
            attachmentRule, attachmentRule, EAttachmentRule.KeepWorld, false);
        if (parent is RectTransform parentTransform)
        {
            var parentRect = parent.GetComponent<RectTransform>();
            gameObject.AddRectTransform(parentRect);
        }
    }

    // Matrix that transforms a point from world space into local space (RO).
    public extern Matrix4x4 worldToLocalMatrix { get; }

    // Matrix that transforms a point from local space into world space (RO).
    public Matrix4x4 localToWorldMatrix
    {
        get
        {
            var transform = u1Component.K2_GetComponentToWorld();
            var mat = transform.ToMatrixWithScale();
            Matrix4x4 result = new Matrix4x4();
            result.m00 = (float)mat.XPlane.X;
            result.m01 = (float)mat.YPlane.X;
            result.m02 = (float)mat.ZPlane.X;
            result.m03 = (float)mat.WPlane.X;
            result.m10 = (float)mat.XPlane.Y;
            result.m11 = (float)mat.YPlane.Y;
            result.m12 = (float)mat.ZPlane.Y;
            result.m13 = (float)mat.WPlane.Y;
            result.m20 = (float)mat.XPlane.Z;
            result.m21 = (float)mat.YPlane.Z;
            result.m22 = (float)mat.ZPlane.Z;
            result.m23 = (float)mat.WPlane.Z;
            result.m30 = (float)mat.XPlane.W;
            result.m31 = (float)mat.YPlane.W;
            result.m32 = (float)mat.ZPlane.W;
            result.m33 = (float)mat.WPlane.W;
            return result;
        }
    }

    // Set position and rotation in world space
    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    public extern void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation);

    public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
    {
        position = this.position;
        rotation = this.rotation;
    }
    
    public extern void GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);

    // Moves the transform in the direction and distance of /translation/.
    public void Translate(Vector3 translation, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
    {
        if (relativeTo == Space.World)
            position += translation;
        else
            position += TransformDirection(translation);
    }

    public void Translate(Vector3 translation)
    {
        Translate(translation, Space.Self);
    }

    // Moves the transform by /x/ along the x axis, /y/ along the y axis, and /z/ along the z axis.
    public void Translate(float x, float y, float z,
        [UnityEngine.Internal.DefaultValue("Space.Self")]
        Space relativeTo)
    {
        Translate(new Vector3(x, y, z), relativeTo);
    }

    public void Translate(float x, float y, float z)
    {
        Translate(new Vector3(x, y, z), Space.Self);
    }

    // Moves the transform in the direction and distance of /translation/.
    public void Translate(Vector3 translation, Transform relativeTo)
    {
        if (relativeTo)
            position += relativeTo.TransformDirection(translation);
        else
            position += translation;
    }

    // Moves the transform by /x/ along the x axis, /y/ along the y axis, and /z/ along the z axis.
    public void Translate(float x, float y, float z, Transform relativeTo)
    {
        Translate(new Vector3(x, y, z), relativeTo);
    }

    // Applies a rotation of /eulerAngles.z/ degrees around the z axis, /eulerAngles.x/ degrees around the x axis, and /eulerAngles.y/ degrees around the y axis (in that order).
    public void Rotate(Vector3 eulers, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
    {
        if (relativeTo == Space.Self)
        {
            var h = new FHitResult();
            owner.K2_AddActorLocalRotation(U3QuaternionUtil.GetU1RotatorFromU3(Quaternion.Euler(eulers)), false, ref h, true);
        }
        else
        {
            eulerAngles += eulers;
        }
    }

    public void Rotate(Vector3 eulers)
    {
        Rotate(eulers, Space.Self);
    }

    // Applies a rotation of /zAngle/ degrees around the z axis, /xAngle/ degrees around the x axis, and /yAngle/ degrees around the y axis (in that order).
    public void Rotate(float xAngle, float yAngle, float zAngle,
        [UnityEngine.Internal.DefaultValue("Space.Self")]
        Space relativeTo)
    {
        Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
    }

    public void Rotate(float xAngle, float yAngle, float zAngle)
    {
        Rotate(new Vector3(xAngle, yAngle, zAngle), Space.Self);
    }

    [NativeMethod("RotateAround")]
    internal extern void RotateAroundInternal(Vector3 axis, float angle);

    // Rotates the transform around /axis/ by /angle/ degrees.
    public void Rotate(Vector3 axis, float angle,
        [UnityEngine.Internal.DefaultValue("Space.Self")]
        Space relativeTo)
    {
        if (relativeTo == Space.Self)
            RotateAroundInternal(transform.TransformDirection(axis), angle * Mathf.Deg2Rad);
        else
            RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
    }

    public void Rotate(Vector3 axis, float angle)
    {
        Rotate(axis, angle, Space.Self);
    }

    // Rotates the transform about /axis/ passing through /point/ in world coordinates by /angle/ degrees.
    public void RotateAround(Vector3 point, Vector3 axis, float angle)
    {
        Vector3 worldPos = position;
        Quaternion q = Quaternion.AngleAxis(angle, axis);
        Vector3 dif = worldPos - point;
        dif = q * dif;
        worldPos = point + dif;
        position = worldPos;
        RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
    }

    // Rotates the transform so the forward vector points at /target/'s current position.
    public void LookAt(Transform target, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp)
    {
        if (target) LookAt(target.position, worldUp);
    }

    public void LookAt(Transform target)
    {
        if (target) LookAt(target.position, Vector3.up);
    }

    // Rotates the transform so the forward vector points at /worldPosition/.
    public void LookAt(Vector3 worldPosition, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp)
    {
        Internal_LookAt(worldPosition, worldUp);
    }

    public void LookAt(Vector3 worldPosition)
    {
        Internal_LookAt(worldPosition, Vector3.up);
    }

    private void Internal_LookAt(Vector3 worldPosition, Vector3 worldUp)
    {
        rotation = Quaternion.LookRotation(worldPosition - position, worldUp);
    }

    // Transforms /direction/ from local space to world space.
    public Vector3 TransformDirection(Vector3 direction)
    {
        return direction.x * right + direction.y * up + direction.z * forward;
    }

    // Transforms direction /x/, /y/, /z/ from local space to world space.
    public Vector3 TransformDirection(float x, float y, float z)
    {
        return TransformDirection(new Vector3(x, y, z));
    }

    // Transforms multiple directions from local space to world space.
    internal unsafe extern void TransformDirections([Span("count", isReadOnly: true)] Vector3* directions,
        int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedDirections,
        int transformedCount);

    public unsafe void TransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections)
    {
        if (directions.Length != transformedDirections.Length)
            throw new InvalidOperationException(
                $"Both spans passed to Transform.TransformDirections() must be the same length");

        fixed (Vector3* srcPtr = directions)
        {
            fixed (Vector3* destPtr = transformedDirections)
            {
                TransformDirections(srcPtr, directions.Length, destPtr, transformedDirections.Length);
            }
        }
    }

    public unsafe void TransformDirections(Span<Vector3> directions)
    {
        TransformDirections(directions, directions);
    }


    // Transforms a /direction/ from world space to local space. The opposite of Transform.TransformDirection.
    public extern Vector3 InverseTransformDirection(Vector3 direction);

    // Transforms the direction /x/, /y/, /z/ from world space to local space. The opposite of Transform.TransformDirection.
    public Vector3 InverseTransformDirection(float x, float y, float z)
    {
        return InverseTransformDirection(new Vector3(x, y, z));
    }

    // Transforms multiple directions from world space to local space. The opposite of Transform.TransformDirections.
    internal unsafe extern void InverseTransformDirections([Span("count", isReadOnly: true)] Vector3* directions,
        int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedDirections,
        int transformedCount);

    public unsafe void InverseTransformDirections(ReadOnlySpan<Vector3> directions,
        Span<Vector3> transformedDirections)
    {
        if (directions.Length != transformedDirections.Length)
            throw new InvalidOperationException(
                $"Both spans passed to Transform.InverseTransformDirections() must be the same length");

        fixed (Vector3* srcPtr = directions)
        {
            fixed (Vector3* destPtr = transformedDirections)
            {
                InverseTransformDirections(srcPtr, directions.Length, destPtr, transformedDirections.Length);
            }
        }
    }

    public unsafe void InverseTransformDirections(Span<Vector3> directions)
    {
        InverseTransformDirections(directions, directions);
    }


    // Transforms /vector/ from local space to world space.
    public extern Vector3 TransformVector(Vector3 vector);

    // Transforms vector /x/, /y/, /z/ from local space to world space.
    public Vector3 TransformVector(float x, float y, float z)
    {
        return TransformVector(new Vector3(x, y, z));
    }

    // Transforms multiple vectors from local space to world space.
    internal unsafe extern void TransformVectors([Span("count", isReadOnly: true)] Vector3* vectors, int count,
        [Span("transformedCount", isReadOnly: false)]
        Vector3* transformedVectors, int transformedCount);

    public unsafe void TransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
    {
        if (vectors.Length != transformedVectors.Length)
            throw new InvalidOperationException(
                $"Both spans passed to Transform.TransformVectors() must be the same length");

        fixed (Vector3* srcPtr = vectors)
        {
            fixed (Vector3* destPtr = transformedVectors)
            {
                TransformVectors(srcPtr, vectors.Length, destPtr, transformedVectors.Length);
            }
        }
    }

    public unsafe void TransformVectors(Span<Vector3> vectors)
    {
        TransformVectors(vectors, vectors);
    }


    // Transforms a /vector/ from world space to local space. The opposite of Transform.TransformVector.
    public extern Vector3 InverseTransformVector(Vector3 vector);

    // Transforms the vector /x/, /y/, /z/ from world space to local space. The opposite of Transform.TransformVector.
    public Vector3 InverseTransformVector(float x, float y, float z)
    {
        return InverseTransformVector(new Vector3(x, y, z));
    }

    // Transforms multiple vectors from world space to local space. The opposite of Transform.TransformVectors.
    internal unsafe extern void InverseTransformVectors([Span("count", isReadOnly: true)] Vector3* vectors,
        int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedVectors, int transformedCount);

    public unsafe void InverseTransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
    {
        if (vectors.Length != transformedVectors.Length)
            throw new InvalidOperationException(
                $"Both spans passed to Transform.InverseTransformVectors() must be the same length");

        fixed (Vector3* srcPtr = vectors)
        {
            fixed (Vector3* destPtr = transformedVectors)
            {
                InverseTransformVectors(srcPtr, vectors.Length, destPtr, transformedVectors.Length);
            }
        }
    }

    public unsafe void InverseTransformVectors(Span<Vector3> vectors)
    {
        InverseTransformVectors(vectors, vectors);
    }


    // Transforms /position/ from local space to world space.
    public Vector3 TransformPoint(Vector3 position)
    {
        FVector location = U3VectorUtil.GetU1LocationFromU3(position);
        FVector worldLocation = owner.K2_GetRootComponent().GetRelativeTransform().TransformPosition(location);
        return U3VectorUtil.GetU3PositionFromU1(worldLocation);
    }

    // Transforms the position /x/, /y/, /z/ from local space to world space.
    public Vector3 TransformPoint(float x, float y, float z)
    {
        return TransformPoint(new Vector3(x, y, z));
    }

    // Transforms multiple positions from local space to world space.
    internal unsafe extern void TransformPoints([Span("count", isReadOnly: true)] Vector3* positions, int count,
        [Span("transformedCount", isReadOnly: false)]
        Vector3* transformedPositions, int transformedCount);

    public unsafe void TransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
    {
        if (positions.Length != transformedPositions.Length)
            throw new InvalidOperationException(
                $"Both spans passed to Transform.TransformPoints() must be the same length");

        fixed (Vector3* srcPtr = positions)
        {
            fixed (Vector3* destPtr = transformedPositions)
            {
                TransformPoints(srcPtr, positions.Length, destPtr, transformedPositions.Length);
            }
        }
    }

    public unsafe void TransformPoints(Span<Vector3> positions)
    {
        TransformPoints(positions, positions);
    }


    // Transforms /position/ from world space to local space. The opposite of Transform.TransformPoint.
    public Vector3 InverseTransformPoint(Vector3 position)
    {
        FVector location = U3VectorUtil.GetU1LocationFromU3(position);
        FVector relativeLocation = owner.K2_GetRootComponent().GetRelativeTransform().Inverse().TransformPosition(location);
        return U3VectorUtil.GetU3PositionFromU1(relativeLocation);
    }

    // Transforms the position /x/, /y/, /z/ from world space to local space. The opposite of Transform.TransformPoint.
    public Vector3 InverseTransformPoint(float x, float y, float z)
    {
        return InverseTransformPoint(new Vector3(x, y, z));
    }

    // Transforms multiple positions from world space to local space. The opposite of Transform.TransformPoints.
    internal unsafe extern void InverseTransformPoints([Span("count", isReadOnly: true)] Vector3* positions,
        int count, [Span("transformedCount", isReadOnly: false)] Vector3* transformedPositions,
        int transformedCount);

    public unsafe void InverseTransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
    {
        if (positions.Length != transformedPositions.Length)
            throw new InvalidOperationException(
                $"Both spans passed to Transform.InverseTransformPoints() must be the same length");

        fixed (Vector3* srcPtr = positions)
        {
            fixed (Vector3* destPtr = transformedPositions)
            {
                InverseTransformPoints(srcPtr, positions.Length, destPtr, transformedPositions.Length);
            }
        }
    }

    public unsafe void InverseTransformPoints(Span<Vector3> positions)
    {
        InverseTransformPoints(positions, positions);
    }


    // Returns the topmost transform in the hierarchy.
    public Transform root
    {
        get { return GetRoot(); }
    }

    private extern Transform GetRoot();

    // The number of children the Transform has.
    public int childCount
    {
        get
        {
            var AttachedActors = new TArray<AActor>();
            owner.GetAttachedActors(ref AttachedActors);
            return AttachedActors.Num();
        }
    }

    // Unparents all children.
    [FreeFunction("DetachChildren", HasExplicitThis = true)]
    public extern void DetachChildren();

    // Move itself to the end of the parent's array of children
    public extern void SetAsFirstSibling();

    // Move itself to the beginning of the parent's array of children
    public extern void SetAsLastSibling();

    public virtual void SetSiblingIndex(int index) {}

    [NativeMethod("MoveAfterSiblingInternal")]
    internal extern void MoveAfterSibling(Transform transform, bool notifyEditorAndMarkDirty);

    public virtual int GetSiblingIndex() { return 0;}

    [FreeFunction]
    private static extern Transform FindRelativeTransformWithPath(
        [NotNull("NullExceptionObject")] Transform transform, string path,
        [UnityEngine.Internal.DefaultValue("false")]
        bool isActiveOnly);

    // Finds a child by /name/ and returns it.
    public Transform Find(string n)
    {
        if (n == null)
            throw new ArgumentNullException("Name cannot be null");
        if (!n.Contains("/"))
        {
            var r = FindInternal(this, n);
            if (r == null) Debug.Log("Couldn't find " + name + "/" + n);

            return r;
        }

        var goNames = n.Split("/");
        var currName = name;
        var currTsf = this;
        foreach (var s in goNames)
        {
            currTsf = FindInternal(currTsf, s);
            currName += "/" + s;
            if (currTsf == null)
            {
                Debug.Log("Couldn't find " + currName);
                return null;
            }
        }

        return currTsf;
    }

    /**
    * Handles a single name, i.e. does not contain “/”
    */
    public Transform FindInternal(Transform transform, string goName)
    {
        UChildActorComponent childActorComponent = transform.owner.GetParentComponent();
        if (childActorComponent == null) {
            // case 1: transform belong to rootComponent and go is a prefab
            USceneComponent rootComponent = transform.owner.K2_GetRootComponent();
            if (rootComponent != null)
            {
                var result = FindTransformBelongComponent(rootComponent, goName);
                if (result != null)
                    return result;   
            }
            var AttachedActors = new TArray<AActor>();
            transform.owner.GetAttachedActors(ref AttachedActors);
            // case 2: not prefab
            foreach (var attachedActor in AttachedActors) {
                GameObject candidate = GameObject.GetFromActorOrCreate(attachedActor);
                string candidateName = candidate.name;
                if (candidateName.Equals(goName)) {
                    return candidate.GetComponent<Transform>();
                }
            }
        } else {
            // case 3: transform belong to childActorComponent
            var uClass = childActorComponent.ChildActor.GetClass();
            if (uClass.IsA<UBlueprintGeneratedClass>())
            {
                return FindTransformBelongComponent(childActorComponent.ChildActor.RootComponent, goName);  
            }
            return FindTransformBelongComponent(childActorComponent, goName);
        }

        return null;
    }
    private Transform FindTransformBelongComponent(USceneComponent component, string goName)
    {
        var childrenComponents = new TArray<USceneComponent>();
        component.GetChildrenComponents(false, ref childrenComponents);
        var ueComponentTag = "U3Name_" + goName;
        foreach (var childrenComponent in childrenComponents)
        {
            if (childrenComponent.IsA<UChildActorComponent>() && childrenComponent.ComponentTags.Contains(ueComponentTag))
            {
                UChildActorComponent childActorComponent = (UChildActorComponent)childrenComponent;
                var uClass = childActorComponent.ChildActor.GetClass();
                // child form bp
                if (uClass.IsA<UBlueprintGeneratedClass>())
                {
                    return GameObject.GetChildComponent<Transform>(childActorComponent.ChildActor.RootComponent);  
                }
                // child form Actor
                return GameObject.GetChildComponent<Transform>(childActorComponent);
            }
        }

        return null;
    }

    //*undocumented
    [NativeConditional("UNITY_EDITOR")]
    internal extern void SendTransformChangedScale();

    // The global scale of the object (RO).
    public extern Vector3 lossyScale { [NativeMethod("GetWorldScaleLossy")] get; }

    // Is this transform a child of /parent/?
    [FreeFunction("Internal_IsChildOrSameTransform", HasExplicitThis = true)]
    public extern bool IsChildOf([NotNull] Transform parent);

    // Has the transform changed since the last time the flag was set to 'false'?
    [NativeProperty("HasChangedDeprecated")]
    public extern bool hasChanged { get; set; }

    //*undocumented*
    [Obsolete("FindChild has been deprecated. Use Find instead (UnityUpgradable) -> Find([mscorlib] System.String)",
        false)]
    public Transform FindChild(string n)
    {
        return Find(n);
    }

    //*undocumented* Documented separately
    public IEnumerator GetEnumerator()
    {
        return new Transform.Enumerator(this);
    }

    private class Enumerator : IEnumerator
    {
        Transform outer;
        int currentIndex = -1;

        internal Enumerator(Transform outer)
        {
            this.outer = outer;
        }

        //*undocumented*
        public object Current
        {
            get { return outer.GetChild(currentIndex); }
        }

        //*undocumented*
        public bool MoveNext()
        {
            int childCount = outer.childCount;
            return ++currentIndex < childCount;
        }

        //*undocumented*
        public void Reset()
        {
            currentIndex = -1;
        }
    }

    // *undocumented* DEPRECATED
    [Obsolete("warning use Transform.Rotate instead.")]
    public extern void RotateAround(Vector3 axis, float angle);

    // *undocumented* DEPRECATED
    [Obsolete("warning use Transform.Rotate instead.")]
    public extern void RotateAroundLocal(Vector3 axis, float angle);

    // Get a transform child by index
    [NativeThrows]
    [FreeFunction("GetChild", HasExplicitThis = true)]
    public Transform GetChild(int index)
    {
        TArray<AActor> childActors = new TArray<AActor>();
        owner.GetAttachedActors(ref childActors);
        if (childActors.Num() <= index) return null;
        return GameObject.MakeFromActor(childActors[index]).transform;
    }

    //*undocumented* DEPRECATED
    [Obsolete("warning use Transform.childCount instead (UnityUpgradable) -> Transform.childCount", false)]
    [NativeMethod("GetChildrenCount")]
    public extern int GetChildCount();

    public int hierarchyCapacity
    {
        get { return internal_getHierarchyCapacity(); }
        set { internal_setHierarchyCapacity(value); }
    }

    [FreeFunction("GetHierarchyCapacity", HasExplicitThis = true)]
    private extern int internal_getHierarchyCapacity();

    [FreeFunction("SetHierarchyCapacity", HasExplicitThis = true)]
    private extern void internal_setHierarchyCapacity(int value);

    public int hierarchyCount
    {
        get { return internal_getHierarchyCount(); }
    }

    [FreeFunction("GetHierarchyCount", HasExplicitThis = true)]
    private extern int internal_getHierarchyCount();

    [NativeConditional("UNITY_EDITOR")]
    [FreeFunction("IsNonUniformScaleTransform", HasExplicitThis = true)]
    internal extern bool IsNonUniformScaleTransform();

    [NativeConditional("UNITY_EDITOR")]
    internal bool constrainProportionsScale
    {
        get => IsConstrainProportionsScale();
        set => SetConstrainProportionsScale(value);
    }

    [NativeConditional("UNITY_EDITOR")]
    private extern void SetConstrainProportionsScale(bool isLinked);

    [NativeConditional("UNITY_EDITOR")]
    private extern bool IsConstrainProportionsScale();
}