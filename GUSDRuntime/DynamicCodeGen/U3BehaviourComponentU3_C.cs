using System;
using System.Collections.Generic;
using GUSD.Utils;
using GUSD.Coroutine;
using Script.CoreUObject;
using Script.Dynamic;
using UnityEngine;
using Script.Engine;
using Object = System.Object;

namespace Script.DynamicCodeGen;

/**
 * Mimics MonoBehaviour behavior for generated IL classes.
 * To add new methods or modify existing functionality,
 * make changes in this class.
 */
[UClass]
[BlueprintSpawnableComponent]
public partial class U3BehaviourComponentU3_C : U3ComponentU3_C
{
    
    public U3BehaviourComponentU3_C(Component proxy) : base(proxy)
    {
    }
    
    public CoroutineManager coroutineManager = null;
    private UPrimitiveComponent u1CollisionComp;
    private Collider u3Collider;
    private Dictionary<UPrimitiveComponent, ContactInfo> contactPairMap = new ();
    
    private bool hasUpdateCall = true;
    private bool hasOnGUICall = true;

    protected override void OnProxyStart()
    {
        SetFixTickEnable(true);
        SetLateTickEnable(true);
        SetCoroutineTickEnable(true);
        SetDuringFrameTickEnable(true);
        Utils.InvokeMethod(proxy, "Start");
        if (coroutineManager != null)
            coroutineManager.UpdateCoroutine();
    }

    private double timpStamp;

    protected override void OnProxyBeforeUpdate(float DeltaSeconds)
    {
    }

    protected override void OnProxyAfterUpdate(float DeltaSeconds)
    {
        var proxBehaviour = proxy as MonoBehaviour;
        if (proxBehaviour == null)
        {
            return;
        }
        // update Invoke timer
        proxBehaviour._timer += DeltaSeconds;
        var _delayedActions = proxBehaviour._delayedActions;
        for (int i = _delayedActions.Count - 1; i >= 0; i--)
        {
            var (time, action) = _delayedActions[i];  
            if (proxBehaviour._timer >= time)
            {
                action?.Invoke();
                _delayedActions.RemoveAt(i);
            }
        }
    }

    protected override void OnProxyUpdate(float DeltaSeconds)
    {
        if (hasUpdateCall)
        {
            hasUpdateCall = InvokeMethod("Update");
        }
        // TODO 需要优化，暂时找不到其他地方执行，先放在Update中
        if (hasOnGUICall)
        {
            hasOnGUICall = InvokeMethod("OnGUI");
        }
        if (!hasOnGUICall && !hasUpdateCall)
        {
            SetReceiveTickEnable(false); // 临时优化， 最终应该通过fody il 在编译前来解决
        }
    }
    
    [Override]
    public override void DuringFrameTick(float DeltaTime)
    {
        base.DuringFrameTick(DeltaTime);
        if (proxy is MonoBehaviour p)
        {
            p.DuringUpdate(DeltaTime);
        }
    }
    
    [Override]
    public override void CoroutineTick()
    {
        if (coroutineManager != null)
            coroutineManager.UpdateCoroutine();
    }

    [Override]
    public override void LateTick(float DeltaTime)
    {
        bool iscallable = InvokeMethod("LateUpdate");
        if (!iscallable)
        {
            SetLateTickEnable(false);
        }
        if (coroutineManager != null)
            coroutineManager.isCurrentFrameEnd = true;
    }

    [Override]
    public override void FixTick(float DeltaTime)
    {
        base.FixTick(DeltaTime);
        Time.fixedDeltaTime = DeltaTime;
        bool iscallable  = InvokeMethod("FixedUpdate");
        if (!iscallable)
        {
            SetFixTickEnable(false);
        } 
        OnCheckHitEnd(DeltaTime);
        if (coroutineManager != null)
            coroutineManager.isCurrentFrameEnd = false;
    }

    [Override]
    public override void ReceiveEndPlay(EEndPlayReason EndPlayReason)
    {
        RemoveCollisionListen();
        Utils.InvokeMethod(proxy, "OnDestroy");
    }

    [Override]
    public override void ReceiveAwake()
    {
        base.ReceiveAwake();
        if (proxy is MonoBehaviour behaviour)
        {
            coroutineManager = behaviour.coroutineManager;
            coroutineManager.m_MonoBehaviour = behaviour;
        }
        Utils.InvokeMethod(proxy, "Awake");
        Utils.InvokeMethod(proxy, "OnEnable");
        AddCollisionListen();
    }
    
    [Override]
    public override void SetActive(bool bNewActive, bool bReset = false)
    {
        base.SetActive(bNewActive, bReset);
        if (bNewActive)
        {
            AddCollisionListen();
        }
        else
        {
            RemoveCollisionListen();
        }
    }
    
    private void AddCollisionListen()
    {
        // Both U1 and U3 must have collision components
        var list = GameObject.GetAllU1ChildComponent<UPrimitiveComponent>(this);
        if (list != null)
        {
            foreach (var item in list)
            {
                var collisionEnabled = item.GetCollisionEnabled();
                if (collisionEnabled == ECollisionEnabled.QueryAndPhysics ||
                    collisionEnabled == ECollisionEnabled.QueryOnly)
                {
                    u1CollisionComp = item;
                    break;
                }
            }
        }
        u3Collider = GameObject.GetChildComponent<Collider>(this);
        if (u1CollisionComp == null || u3Collider == null)
        {
            return;
        }
        AddCollisionListen(u1CollisionComp);
    }

    private void RemoveCollisionListen()
    {
        if (u1CollisionComp == null || u3Collider == null)
        {
            return;
        }
        RemoveCollisionListen(u1CollisionComp);
        contactPairMap.Clear();
    }

    [Override]
    private void OnOverlapBegin(UPrimitiveComponent OverlappedComp, AActor OtherActor, UPrimitiveComponent OtherComp, int OtherBodyIndex, bool BFromSweep, FHitResult SweepResult)
    {
        if (u1CollisionComp == null || OtherComp == null)
        {
            return;
        }
            
        Collider other = GameObject.GetChildComponent<Collider>(OtherComp);
        if (other == null) return;
        InvokeMethod("OnTriggerEnter", [other]);
    }
    
    [Override]
    private void OnOverlapEnd(UPrimitiveComponent OverlappedComp, AActor OtherActor, UPrimitiveComponent OtherComp, int OtherBodyIndex)
    {
        if (u1CollisionComp == null || OtherComp == null)
        {
            return;
        }

        Collider other = GameObject.GetChildComponent<Collider>(OtherComp);
        if (other == null) return;
        InvokeMethod("OnTriggerExit", [other]);
    }

    [Override]
    private void OnHit(UPrimitiveComponent HitComponent, AActor OtherActor, UPrimitiveComponent OtherComp, FVector NormalImpulse, FHitResult Hit)
    {
        if (u1CollisionComp == null || OtherComp == null)
        {
            return;
        }
        Collider other = GameObject.GetChildComponent<Collider>(OtherComp);
        if (other == null) return;
        // 0.4 is Exp Num
        double duration = 0.4f / Math.Max(Hit.ImpactNormal.Length(), 0.1f);
        if (contactPairMap.ContainsKey(OtherComp))
        {
            ContactInfo contactInfo =  new ContactInfo(duration, Hit, u3Collider, other);
            contactPairMap[OtherComp] = contactInfo;
            InvokeMethod("OnCollisionStay", [contactInfo.collision]);
        }
        else
        {
            ContactInfo contactInfo =  new ContactInfo(duration, Hit, u3Collider, other);
            contactPairMap.Add(OtherComp, contactInfo);
            InvokeMethod("OnCollisionEnter", [contactInfo.collision]);
        }
    }

    private void OnCheckHitEnd(float DeltaTime)
    {
        if (u1CollisionComp == null || u3Collider == null) return;
        if (contactPairMap.Count <= 0) return;
        TArray<UPrimitiveComponent> toRemove = new TArray<UPrimitiveComponent>();
        foreach (KeyValuePair<UPrimitiveComponent, ContactInfo> entry in contactPairMap)
        {
            UPrimitiveComponent component = entry.Key;
            ContactInfo contactInfo = entry.Value;
            contactInfo.duration -= DeltaTime;
            if (contactInfo.duration <= 0)
            {
                InvokeMethod("OnCollisionExit", [contactInfo.collision]);
                toRemove.Add(component);
            }
        }
        foreach (var component in toRemove)
        {
            contactPairMap.Remove(component);
        }
    }
    
    public void OnParticleCollision(GameObject other)
    {
        InvokeMethod("OnParticleCollision", [other]);
    }
    
    private class ContactInfo
    {
        public Collision collision;
        public double duration;

        public ContactInfo(double Duration, FHitResult Hit, Collider ThisCollider, Collider OtherCollider)
        {
            ContactPoint point = ParseContactPoint(Hit, ThisCollider, OtherCollider);
            collision = new Collision(point, ThisCollider, OtherCollider);
            this.duration = Duration;
        }
        
        private ContactPoint ParseContactPoint(FHitResult Hit, Collider ThisCollider, Collider OtherCollider)
        {
            if (Hit == null) return new ContactPoint();
            var point = U3VectorUtil.GetU3PositionFromU1(Hit.ImpactPoint);
            var normal = U3VectorUtil.GetU3PositionFromU1(Hit.ImpactNormal);
            // TO Fix no impulse in u1
            return new ContactPoint(point, normal, Vector3.zero, Hit.PenetrationDepth, ThisCollider, OtherCollider);
        }
    }
}