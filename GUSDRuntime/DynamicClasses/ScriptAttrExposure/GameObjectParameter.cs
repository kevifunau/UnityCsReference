using Script.AudioExtensions;
using Script.Dynamic;
using Script.Engine;
using Script.CoreUObject;

namespace Script.CoreUObject;

[UStruct, BlueprintType]
public partial class FGameObjectParameter
{
    [UProperty, EditAnywhere]
    public EGameObjectParameterType ParamType { get; set; }

    [UProperty, EditAnywhere, EditConditionHides]
    [DisplayAfter("ParamType"), EditCondition("ParamType == EGameObjectParameterType::Actor")]
    public AActor ActorParam { get; set; }

    // Blueprint asset, needs to be referenced as a UClass
    [UProperty, EditAnywhere, EditConditionHides]
    [DisplayAfter("ParamType"), EditCondition("ParamType == EGameObjectParameterType::Blueprint")]
    public TSubclassOf<UObject> BlueprintParam { get; set; }
    
    // Used to reference GO in prefab
    [UProperty, EditAnywhere, EditConditionHides]
    [DisplayAfter("ParamType"), EditCondition("ParamType == EGameObjectParameterType::ChildActorComponent")]
    public FString ChildActorComponentParam { get; set; }
}