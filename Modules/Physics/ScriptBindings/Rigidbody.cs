using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using System;
using GUSD.Utils;
using Script.Dynamic;
using Script.Engine;
using Script.CoreUObject;

namespace UnityEngine;

[U3Exported(false)]
[RequireComponent(typeof(Transform))]
[NativeHeader("Modules/Physics/Rigidbody.h")]
public partial class Rigidbody : UnityEngine.Component
{
    public UCapsuleComponent capsule;
    public UPrimitiveComponent realRigidBody;
    public override void InitializeCorrespondU1Component()
    {
        capsule = (UCapsuleComponent)owner.AddComponentByClass(UCapsuleComponent.StaticClass(),false, FTransform.Identity, false);
    }

    public Vector3 velocity
    {
        get
        {
            if (realRigidBody == null) 
                GetRealRigidBody();
            FVector velocity = realRigidBody?.GetPhysicsLinearVelocity() ?? FVector.ZeroVector;
            return U3VectorUtil.GetU3PositionFromU1(velocity);
        } 
        set
        {
            if (realRigidBody == null) 
                GetRealRigidBody();
            FVector velocity = U3VectorUtil.GetU1LocationFromU3(value);
            realRigidBody?.SetAllPhysicsLinearVelocity(velocity, false);
        }
    }
    extern public Vector3 angularVelocity { get; set; }
    extern public float drag { get; set; }
    extern public float angularDrag { get; set; }
    public float mass
    {
        get
        {
            if (realRigidBody == null) 
                GetRealRigidBody();
            return realRigidBody?.GetMass() ?? 0.0f;
        }
        set
        {
            if (realRigidBody == null) 
                GetRealRigidBody();
            realRigidBody?.SetMassOverrideInKg(null, value, true);
        }
    }
    extern public void SetDensity(float density);
    extern public float maxDepenetrationVelocity { get; set; }
    public bool isKinematic
    {
        get
        {
            if (realRigidBody == null)
                GetRealRigidBody();
            return realRigidBody != null && !realRigidBody.IsSimulatingPhysics();
        }
        set
        {
            if (realRigidBody == null)
                GetRealRigidBody();
            realRigidBody?.SetSimulatePhysics(!value);
        }
    }    
    extern public bool freezeRotation { get; set; }
    extern public RigidbodyConstraints constraints { get; set; }
    extern public CollisionDetectionMode collisionDetectionMode { get; set; }
    extern public bool automaticCenterOfMass { get; set; }
    extern public Vector3 centerOfMass { get; set; }
    extern public Vector3 worldCenterOfMass { get; }
    extern public bool automaticInertiaTensor { get; set; }
    extern public Quaternion inertiaTensorRotation { get; set; }
    extern public Vector3 inertiaTensor { get; set; }
    extern public bool detectCollisions { get; set; }

    public Vector3 position
    {
        get
        {
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                return U3VectorUtil.GetU3PositionFromU1(owner.K2_GetActorLocation());
            }
            else
            {
                return U3VectorUtil.GetU3PositionFromU1(owner.GetParentComponent().K2_GetComponentLocation());
            }
        }
        set
        {
            var location = U3VectorUtil.GetU1LocationFromU3(value);
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
    
    public bool useGravity
    {
        get
        {
            if (realRigidBody == null)
                GetRealRigidBody();

            return realRigidBody?.IsGravityEnabled() ?? false;
        }
        set
        {
            if (realRigidBody == null)
                GetRealRigidBody();

            realRigidBody?.SetEnableGravity(value);
        }
    }

    public Quaternion rotation
    {
        get => U3QuaternionUtil.ConvertU1QuatToU3(owner.K2_GetActorRotation().Quaternion());
        set
        {
            if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
            {
                owner.K2_SetActorRotation(U3QuaternionUtil.GetU1RotatorFromU3(value), true); 
            }
            else
            {
                var result = new FHitResult();
                owner.GetParentComponent().K2_SetWorldRotation(U3QuaternionUtil.GetU1RotatorFromU3(value), true,ref result,false);
            }
        }
    }

    extern public RigidbodyInterpolation interpolation { get; set; }
    extern public int solverIterations { get; set; }
    extern public float sleepThreshold { get; set; }
    extern public float maxAngularVelocity { get; set; }
    extern public float maxLinearVelocity { get; set; }
    
    private void GetRealRigidBody()
    {
        var shapeComponent = GameObject.GetU1ChildComponent<UPrimitiveComponent>(u1Component);
        if (shapeComponent != null)
        {
            realRigidBody = shapeComponent;
            realRigidBody?.SetSimulatePhysics(true);
        }
    }

    public void MovePosition(Vector3 position)
    {
        FHitResult result = new FHitResult();
        if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
        {
            owner.K2_SetActorLocation(U3VectorUtil.GetU1LocationFromU3(position), false, ref result, false);
        }
        else
        {
            owner.GetParentComponent().K2_SetWorldLocation(U3VectorUtil.GetU1LocationFromU3(position), true, ref result, false);
        }
    }

    public void MoveRotation(Quaternion rot)
    {
        if (owner.GetParentComponent() == null || !owner.GetParentComponent().IsA<UChildActorComponent>())
        { 
            owner.K2_SetActorRotation(U3QuaternionUtil.GetU1RotatorFromU3(rot), false);
        }
        else
        {  
            var result = new FHitResult();
            owner.GetParentComponent().K2_SetWorldRotation(U3QuaternionUtil.GetU1RotatorFromU3(rot), true,ref result,false);
        }
    }
    
    extern public void Move(Vector3 position, Quaternion rotation);
    extern public void Sleep();
    extern public bool IsSleeping();
    extern public void WakeUp();
    extern public void ResetCenterOfMass();
    extern public void ResetInertiaTensor();
    extern public Vector3 GetRelativePointVelocity(Vector3 relativePoint);
    extern public Vector3 GetPointVelocity(Vector3 worldPoint);
    extern public int solverVelocityIterations { get; set; }

    // Get/Set the Exclude Layers,
    extern public LayerMask excludeLayers { get; set; }

    // Get/Set the Include Layers,
    extern public LayerMask includeLayers { get; set; }

    extern public Vector3 GetAccumulatedForce([DefaultValue("Time.fixedDeltaTime")] float step);

    [ExcludeFromDocs]
    public Vector3 GetAccumulatedForce()
    {
        return GetAccumulatedForce(Time.fixedDeltaTime);
    }

    extern public Vector3 GetAccumulatedTorque([DefaultValue("Time.fixedDeltaTime")] float step);

    [ExcludeFromDocs]
    public Vector3 GetAccumulatedTorque()
    {
        return GetAccumulatedTorque(Time.fixedDeltaTime);
    }

    public void AddForce(Vector3 u3Force, [DefaultValue("ForceMode.Force")] ForceMode mode)
    {
        if (realRigidBody == null)
            GetRealRigidBody();
        FVector u1Force = U3VectorUtil.GetU1LocationFromU3(u3Force);
        if (mode == ForceMode.Force) {
            realRigidBody?.AddForce(u1Force);
        } else if (mode == ForceMode.Acceleration) {
            realRigidBody?.AddForce(u1Force, null, true);
        } else if (mode == ForceMode.Impulse) {
            realRigidBody?.AddImpulse(u1Force);
        } else if (mode == ForceMode.VelocityChange) {
            realRigidBody?.AddImpulse(u1Force, null, true);
        }
    }

    [ExcludeFromDocs]
    public void AddForce(Vector3 force)
    {
        AddForce(force, ForceMode.Force);
    }

    public void AddForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode)
    {
        AddForce(new Vector3(x, y, z), mode);
    }

    [ExcludeFromDocs]
    public void AddForce(float x, float y, float z)
    {
        AddForce(new Vector3(x, y, z), ForceMode.Force);
    }

    extern public void AddRelativeForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

    [ExcludeFromDocs]
    public void AddRelativeForce(Vector3 force)
    {
        AddRelativeForce(force, ForceMode.Force);
    }

    public void AddRelativeForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode)
    {
        AddRelativeForce(new Vector3(x, y, z), mode);
    }

    [ExcludeFromDocs]
    public void AddRelativeForce(float x, float y, float z)
    {
        AddRelativeForce(new Vector3(x, y, z), ForceMode.Force);
    }

    extern public void AddTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

    [ExcludeFromDocs]
    public void AddTorque(Vector3 torque)
    {
        AddTorque(torque, ForceMode.Force);
    }

    public void AddTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode)
    {
        AddTorque(new Vector3(x, y, z), mode);
    }

    [ExcludeFromDocs]
    public void AddTorque(float x, float y, float z)
    {
        AddTorque(new Vector3(x, y, z), ForceMode.Force);
    }

    extern public void AddRelativeTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

    [ExcludeFromDocs]
    public void AddRelativeTorque(Vector3 torque)
    {
        AddRelativeTorque(torque, ForceMode.Force);
    }

    public void AddRelativeTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode)
    {
        AddRelativeTorque(new Vector3(x, y, z), mode);
    }

    [ExcludeFromDocs]
    public void AddRelativeTorque(float x, float y, float z)
    {
        AddRelativeTorque(x, y, z, ForceMode.Force);
    }

    extern public void AddForceAtPosition(Vector3 force, Vector3 position,
        [DefaultValue("ForceMode.Force")] ForceMode mode);

    [ExcludeFromDocs]
    public void AddForceAtPosition(Vector3 force, Vector3 position)
    {
        AddForceAtPosition(force, position, ForceMode.Force);
    }

    extern public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius,
        [DefaultValue("0.0f")] float upwardsModifier, [DefaultValue("ForceMode.Force)")] ForceMode mode);

    [ExcludeFromDocs]
    public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius,
        float upwardsModifier)
    {
        AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Force);
    }

    [ExcludeFromDocs]
    public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius)
    {
        AddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0.0f, ForceMode.Force);
    }

    [NativeName("ClosestPointOnBounds")]
    extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

    public Vector3 ClosestPointOnBounds(Vector3 position)
    {
        float dist = 0f;
        Vector3 outpos = Vector3.zero;
        Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
        return outpos;
    }

    extern private RaycastHit SweepTest(Vector3 direction, float maxDistance,
        QueryTriggerInteraction queryTriggerInteraction, ref bool hasHit);

    public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance,
        [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
    {
        float dirLength = direction.magnitude;

        if (dirLength > float.Epsilon)
        {
            Vector3 normalizedDirection = direction / dirLength;
            bool hasHit = false;
            hitInfo = SweepTest(normalizedDirection, maxDistance, queryTriggerInteraction, ref hasHit);
            return hasHit;
        }
        else
        {
            hitInfo = new RaycastHit();
            return false;
        }
    }

    [ExcludeFromDocs]
    public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, float maxDistance)
    {
        return SweepTest(direction, out hitInfo, maxDistance, QueryTriggerInteraction.UseGlobal);
    }

    [ExcludeFromDocs]
    public bool SweepTest(Vector3 direction, out RaycastHit hitInfo)
    {
        return SweepTest(direction, out hitInfo, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
    }

    [NativeName("SweepTestAll")]
    extern private RaycastHit[] Internal_SweepTestAll(Vector3 direction, float maxDistance,
        QueryTriggerInteraction queryTriggerInteraction);

    public RaycastHit[] SweepTestAll(Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance,
        [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
    {
        float dirLength = direction.magnitude;
        if (dirLength > float.Epsilon)
        {
            Vector3 normalizedDirection = direction / dirLength;
            return Internal_SweepTestAll(normalizedDirection, maxDistance, queryTriggerInteraction);
        }
        else
        {
            return new RaycastHit[0];
        }
    }

    [ExcludeFromDocs]
    public RaycastHit[] SweepTestAll(Vector3 direction, float maxDistance)
    {
        return SweepTestAll(direction, maxDistance, QueryTriggerInteraction.UseGlobal);
    }

    [ExcludeFromDocs]
    public RaycastHit[] SweepTestAll(Vector3 direction)
    {
        return SweepTestAll(direction, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
    }
    
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
    public float sleepVelocity { get { return 0; } set {} }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("The sleepAngularVelocity is no longer supported. Use sleepThreshold to specify energy.", true)]
    public float sleepAngularVelocity { get { return 0; } set {} }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use Rigidbody.maxAngularVelocity instead.")]
    public void SetMaxAngularVelocity(float a) { maxAngularVelocity = a; }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Cone friction is no longer supported.", true)]
    public bool useConeFriction { get { return false; } set {} }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Please use Rigidbody.solverIterations instead. (UnityUpgradable) -> solverIterations")]
    public int solverIterationCount { get { return solverIterations; } set { solverIterations = value; } }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Please use Rigidbody.solverVelocityIterations instead. (UnityUpgradable) -> solverVelocityIterations")]
    public int solverVelocityIterationCount { get { return solverVelocityIterations; } set { solverVelocityIterations = value; } }
}