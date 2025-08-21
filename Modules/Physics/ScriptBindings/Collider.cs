using GUSD.Utils;
using UnityEngine;
using UnityEngine.Bindings;
using Script.Dynamic;
using Script.Engine;
using Script.CoreUObject;
using Script.UnrealCSharp;

[U3Exported(false)]
[RequireComponent(typeof(Transform))]
[NativeHeader("Modules/Physics/Collider.h")]
public partial class Collider : Component
{
    public bool enabled
    {
        get
        {
            var shapeComponent = GameObject.GetU1ChildComponent<UPrimitiveComponent>(u1Component);
            return shapeComponent.GetCollisionEnabled() == ECollisionEnabled.NoCollision;
        }
        
        set
        {
            var shapeComponent = GameObject.GetU1ChildComponent<UPrimitiveComponent>(u1Component);
            if (shapeComponent != null)
            {
                // TOFIX 当这里的原始碰撞相应不是QueryAndPhysics，这里可能也会有问题
                shapeComponent.SetCollisionEnabled(value ? 
                    ECollisionEnabled.QueryAndPhysics : 
                    ECollisionEnabled.NoCollision);
            }
        }
    }

    extern public Rigidbody attachedRigidbody {[NativeMethod("GetRigidbody")] get; }
    extern public ArticulationBody attachedArticulationBody {[NativeMethod("GetArticulationBody")] get; }
    extern public bool isTrigger { get; set; }
    extern public float contactOffset { get; set; }

    public Vector3 ClosestPoint(Vector3 position)
    {
        var shapeComponent = GameObject.GetU1ChildComponent<UPrimitiveComponent>(u1Component);
        if (shapeComponent == null) return Vector3.zero;
        
        FVector u1Pos = U3VectorUtil.GetU1LocationFromU3(position);
        FVector closestPos = FVector.Zero();
        shapeComponent.GetClosestPointOnCollision(u1Pos, ref closestPos, FName.NAME_None);
        return U3VectorUtil.GetU3PositionFromU1(closestPos);
    }
    public Bounds bounds 
    {
        get
        {
            var primitiveComponent = GameObject.GetU1ChildComponent<UPrimitiveComponent>(u1Component);
            if (primitiveComponent == null) return new Bounds();
            var u1Center = new FVector();
            var u1Extents = new FVector();
            UGUSDWorldUtil.GetWorldBounds(primitiveComponent, ref u1Center, ref u1Extents);
            return Bounds.GetBoundsFormU1(u1Center, u1Extents);
        } 
    }
    extern public bool hasModifiableContacts {get; set;}
    extern public bool providesContacts { get; set; }

    // Get/Set the Layer Override Priority.
    extern public int layerOverridePriority { get; set; }

    // Get/Set the Exclude Layers,
    extern public LayerMask excludeLayers { get; set; }

    // Get/Set the Include Layers,
    extern public LayerMask includeLayers { get; set; }

    [NativeMethod("Material")]
    extern public PhysicMaterial sharedMaterial { get; set; }

    extern public PhysicMaterial material
    {
        [NativeMethod("GetClonedMaterial")] get;
        [NativeMethod("SetMaterial")] set;
    }

    extern private RaycastHit Raycast(Ray ray, float maxDistance, ref bool hasHit);

    public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
    {
        bool hasHit = false;
        hitInfo = Raycast(ray, maxDistance, ref hasHit);
        return hasHit;
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
}