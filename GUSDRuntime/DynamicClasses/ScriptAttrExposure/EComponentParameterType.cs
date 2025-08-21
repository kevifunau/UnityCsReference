using Script.Dynamic;

namespace Script.CoreUObject;

[UEnum, BlueprintType]
public enum EComponentParameterType : byte
{
    InGO = 0,
    InPrefab = 1
}