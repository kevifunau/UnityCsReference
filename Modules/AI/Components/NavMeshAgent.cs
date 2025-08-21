// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Cinemachine.Utility;
using GUSD.Utils;
using Script.AIModule;
using Script.Dynamic;
using Script.Engine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using Script.CoreUObject;
using Script.NavigationSystem;

namespace UnityEngine.AI;
    
// Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
[MovedFrom("UnityEngine")]
public enum ObstacleAvoidanceType
{
    // Disable avoidance.
    NoObstacleAvoidance = 0,

    // Enable simple avoidance. Low performance impact.
    LowQualityObstacleAvoidance = 1,

    // Medium avoidance. Medium performance impact
    MedQualityObstacleAvoidance = 2,

    // Good avoidance. High performance impact
    GoodQualityObstacleAvoidance = 3,

    // Enable highest precision. Highest performance impact.
    HighQualityObstacleAvoidance = 4
}
// Navigation mesh agent.
[U3Exported(false)]
[MovedFrom("UnityEngine")]
[NativeHeader("Modules/AI/Components/NavMeshAgent.bindings.h")]
[NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
public sealed partial class NavMeshAgent : Behaviour
{
    // Sets or updates the destination. This triggers calculation for a new path.
    private Vector3 _target;
    void Start()
    {
        if (!isOnNavMesh)
        {
            PlaceActorToNearestNavMesh(gameObject.actor, 200.0f);
        }
    }
    
    public bool SetDestination(Vector3 target)
    {
        if (owner is APawn pawn)
        {
            _target = target;
            var controller = pawn.GetController();
            var location = U3VectorUtil.GetU1LocationFromU3(target);
            UAIBlueprintHelperLibrary.SimpleMoveToLocation(controller, location);
            return true;
        }
        return false;
    }

    // Destination to navigate towards.
    public extern Vector3 destination { get; set; }

    // Stop within this distance from the target position.
    public float stoppingDistance { get; set; }

    // The current velocity of the [[NavMeshAgent]] component.
    public extern Vector3 velocity { get; set; }

    // The next position on the path.
    [NativeProperty("Position")]
    public extern Vector3 nextPosition { get; set; }

    // The current steering target - usually the next corner or end point of the current path. (RO)
    public extern Vector3 steeringTarget { get; }

    // The desired velocity of the agent including any potential contribution from avoidance. (RO)
    public extern Vector3 desiredVelocity { get; }

    // Remaining distance along the current path - or infinity when not known. (RO)
    public extern float remainingDistance { get; }

    // The relative vertical displacement of the owning [[GameObject]].
    public float baseOffset { get; set; }

    // Is agent currently positioned on an OffMeshLink. (RO)
    public extern bool isOnOffMeshLink
    {
        [NativeName("IsOnOffMeshLink")]
        get;
    }

    // Enables or disables the current link.
    public extern void ActivateCurrentOffMeshLink(bool activated);

    // The current [[OffMeshLinkData]].
    public OffMeshLinkData currentOffMeshLinkData => GetCurrentOffMeshLinkDataInternal();

    [FreeFunction("NavMeshAgentScriptBindings::GetCurrentOffMeshLinkDataInternal", HasExplicitThis = true)]
    internal extern OffMeshLinkData GetCurrentOffMeshLinkDataInternal();

    // The next [[OffMeshLinkData]] on the current path.
    public OffMeshLinkData nextOffMeshLinkData => GetNextOffMeshLinkDataInternal();

    [FreeFunction("NavMeshAgentScriptBindings::GetNextOffMeshLinkDataInternal", HasExplicitThis = true)]
    internal extern OffMeshLinkData GetNextOffMeshLinkDataInternal();

    // Terminate OffMeshLink occupation and transfer the agent to the closest point on other side.
    public extern void CompleteOffMeshLink();

    // Automate movement onto and off of OffMeshLinks.
    public bool autoTraverseOffMeshLink { get; set; }

    // Automate braking of NavMeshAgent to avoid overshooting the destination.
    public bool autoBraking { get; set; }

    // Attempt to acquire a new path if the existing path becomes invalid
    public bool autoRepath { get; set; }

    // Does this agent currently have a path. (RO)
    public extern bool hasPath
    {
        [NativeName("HasPath")]
        get;
    }

    // A path is being computed, but not yet ready. (RO)
    public extern bool pathPending
    {
        [NativeName("PathPending")]
        get;
    }

    // Is the current path stale. (RO)
    public extern bool isPathStale
    {
        [NativeName("IsPathStale")]
        get;
    }

    // Query the state of the current path.
    public extern NavMeshPathStatus pathStatus { get; }

    //*undocumented*
    [NativeProperty("EndPositionOfCurrentPath")]
    public extern Vector3 pathEndPosition { get; }

    //*undocumented*
    public extern bool Warp(Vector3 newPosition);

    // Apply relative movement to current position.
    public extern void Move(Vector3 offset);

    [Obsolete("Set isStopped to true instead.")]
    public void Stop()
    {
        var pawn = (APawn)owner;
        if (pawn == null)
        {
            return;
        }
        pawn.GetController().StopMovement();
    }

    // Stop movement of this agent along its current path.
    [Obsolete("Set isStopped to true instead.")]
    public void Stop(bool stopUpdates) { Stop(); }

    // Resumes the movement along the current path.
    [Obsolete("Set isStopped to false instead.")]
    public void Resume()
    {
        var pawn = (APawn)owner;
        if (pawn == null)
        {
            return;
        }
        if (_target.IsNaN())
        {
            return;
        }
        var controller = pawn.GetController();
        var location = U3VectorUtil.GetU1LocationFromU3(_target);
        UAIBlueprintHelperLibrary.SimpleMoveToLocation(controller, location);
    }
    
    public extern bool isStopped
    {
        [FreeFunction("NavMeshAgentScriptBindings::GetIsStopped", HasExplicitThis = true)]
        get;
        [FreeFunction("NavMeshAgentScriptBindings::SetIsStopped", HasExplicitThis = true)]
        set;
    }

    // Clears the current path. Note that this agent will not start looking for a new path until SetDestination is called.
    public extern void ResetPath();

    // Assign path to this agent.
    public extern bool SetPath([NotNull] NavMeshPath path);

    // Set or get a copy of the current path.
    public NavMeshPath path
    {
        get
        {
            NavMeshPath result = new NavMeshPath();
            CopyPathTo(result);
            return result;
        }
        set
        {
            if (value == null)
                throw new NullReferenceException();
            SetPath(value);
        }
    }

    [NativeMethod("CopyPath")]
    internal extern void CopyPathTo([NotNull] NavMeshPath path);

    // Locate the closest NavMesh edge.
    [NativeName("DistanceToEdge")]
    public extern bool FindClosestEdge(out NavMeshHit hit);

    // Trace movement towards a target position in the NavMesh. Without moving the agent.
    public extern bool Raycast(Vector3 targetPosition, out NavMeshHit hit);

    // Calculate a path to a specified point and store the resulting path.
    public bool CalculatePath(Vector3 targetPosition, NavMeshPath path)
    {
        path.ClearCorners();
        return CalculatePathInternal(targetPosition, path);
    }

    [FreeFunction("NavMeshAgentScriptBindings::CalculatePathInternal", HasExplicitThis = true)]
    extern bool CalculatePathInternal(Vector3 targetPosition, [NotNull] NavMeshPath path);

    // Sample a position along the current path.
    public extern bool SamplePathPosition(int areaMask, float maxDistance, out NavMeshHit hit);

    [Obsolete("Use SetAreaCost instead.")]
    [NativeMethod("SetAreaCost")]
    public extern void SetLayerCost(int layer, float cost);

    [Obsolete("Use GetAreaCost instead.")]
    [NativeMethod("GetAreaCost")]
    public extern float GetLayerCost(int layer);

    public extern void SetAreaCost(int areaIndex, float areaCost);

    public extern float GetAreaCost(int areaIndex);

    public Object navMeshOwner => GetOwnerInternal();

    public int agentTypeID { get; set; }

    [NativeName("GetCurrentPolygonOwner")]
    extern Object GetOwnerInternal();

    [Obsolete("Use areaMask instead.")]
    public int walkableMask { get { return areaMask; } set { areaMask = value; } }

    public int areaMask { get; set; }

    private const float U3toU1distance = 100;
    
    // Maximum movement speed.
    public float speed
    {
        get
        {
            if (owner is ACharacter character)
            {
                return character.CharacterMovement.MaxWalkSpeed / U3toU1distance;
            }
            return 0;
        }
        set
        {
            if (owner is ACharacter character)
            {
                character.CharacterMovement.MaxWalkSpeed = value * U3toU1distance;
            }
        }
    }

    // Maximum rotation speed in (deg/s).
    public float angularSpeed
    {
        get
        {
            if (owner is ACharacter character)
            {
                return (float)character.CharacterMovement.RotationRate.Yaw;
            }
            return 0f;
        }
        
        set
        {
            if (owner is ACharacter character)
            {
                character.CharacterMovement.RotationRate = new FRotator(0, value, 0);
            }
        }
    }

    // Maximum acceleration.
    public float acceleration
    {
        get
        {
            if (owner is ACharacter character)
            {
                return character.CharacterMovement.MaxAcceleration / U3toU1distance;
            }
            return 0f;
        }
        set
        {
            if (owner is ACharacter character)
            {
                character.CharacterMovement.MaxAcceleration = value * U3toU1distance;
            }
        }
    }

    // Should the agent update the transform position.
    public extern bool updatePosition { get; set; }

    // Should the agent update the transform orientation.
    public extern bool updateRotation { get; set; }

    public extern bool updateUpAxis { get; set; }

    // Agent avoidance radius.
    public float radius
    {
        get
        {
            if (owner is ACharacter character)
            {
                return character.CapsuleComponent.GetUnscaledCapsuleRadius() / U3toU1distance;
            }
            return 0f;
        }
        set
        {
            if (owner is ACharacter character)
            {
                character.CapsuleComponent.SetCapsuleRadius(value * U3toU1distance);
            }
        }
    }

    // Agent height.
    public float height { 
        get
        {
            if (owner is ACharacter character)
            {
                return character.CapsuleComponent.GetUnscaledCapsuleHalfHeight() * 2 / U3toU1distance;
            }
            return 0f;
        }
        set
        {
            if (owner is ACharacter character)
            {
                character.CapsuleComponent.SetCapsuleHalfHeight(value / 2 * U3toU1distance);
            }
        }
    }

    // The level of quality of avoidance.
    public ObstacleAvoidanceType obstacleAvoidanceType { get; set; }

    // The avoidance priority level.
    public int avoidancePriority { get; set; }

    // Is agent mapped to navmesh
    public bool isOnNavMesh
    {
        [NativeName("InCrowdSystem")]
        get
        {
            FVector agentLocation = gameObject.actor.K2_GetActorLocation();
            UNavigationSystemV1 navSys = UNavigationSystemV1.GetNavigationSystem(Unreal.GWorld);
        
            if (navSys == null)
            {
                Debug.LogWarning("Navigation system not found");
                return false;
            }
        
            float verticalTolerance = 50.0f;
            float horizontalTolerance = 10.0f;
        
            FVector projectedLocation = new FVector(0, 0, 0);
            bool bProjected = UNavigationSystemV1.K2_ProjectPointToNavigation(
                Unreal.GWorld,
                agentLocation,
                ref projectedLocation,
                null, 
                null,
                new FVector(horizontalTolerance, horizontalTolerance, verticalTolerance)
            );
            
            if (bProjected)
            {
                return true;
            }
            return false;
        }
    }
    
    public static bool PlaceActorToNearestNavMesh(AActor actor, float searchRadius)
    {
        FVector actorLocation = actor.K2_GetActorLocation();
        UNavigationSystemV1 navSys = UNavigationSystemV1.GetNavigationSystem(Unreal.GWorld);
        if (navSys == null)
        {
            Debug.LogWarning("Navigation system not found");
            return false;
        }
        
        FVector projectedLocation = new FVector(0, 0, 0);
        bool success = UNavigationSystemV1.K2_ProjectPointToNavigation(
            Unreal.GWorld,
            actorLocation,
            ref projectedLocation,
            null,
            null,
            new FVector(searchRadius, searchRadius, searchRadius)
        );
        
        if (success)
        {
            FHitResult hitResult = new FHitResult();
            actor.K2_SetActorLocation(projectedLocation, false, ref hitResult, true);
            return true;
        }
        return false;
    }
}