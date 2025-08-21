using Script.Dynamic;

namespace Script.CoreUObject;

[UEnum, BlueprintType]
public enum EGameObjectParameterType : byte
{
    Actor = 0,
    Blueprint = 1,
    ChildActorComponent = 2
}