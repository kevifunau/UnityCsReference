using Script.AudioExtensions;
using Script.Dynamic;
using Script.Engine;
using Script.CoreUObject;

namespace Script.CoreUObject;

[UStruct, BlueprintType]
public partial class FComponentParameter
{
    [UProperty, EditAnywhere]
    public EComponentParameterType ParamType { get; set; }

    // Used to reference component in GO
    [UProperty, EditAnywhere, EditConditionHides]
    [DisplayAfter("ParamType"), EditCondition("ParamType == EComponentParameterType::InGO")]
    public AActor InGOParam { get; set; }
    
    // Used to reference component in prefab
    [UProperty, EditAnywhere, EditConditionHides]
    [DisplayAfter("ParamType"), EditCondition("ParamType == EComponentParameterType::InPrefab")]
    public FString InPrefabParam { get; set; }
}