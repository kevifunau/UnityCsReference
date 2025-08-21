using System.Reflection;
using Script.Dynamic;
using Script.Engine;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Script.CoreUObject;

[U3Exported]
public partial class InitUIWidget : MonoBehaviour
{
    void Awake()
    {
        System.Type[] uiWidgetTypesToInitialize =
        {
            typeof(Image), typeof(RawImage), typeof(Slider), typeof(Canvas), typeof(UnityEngine.UI.Button),
            typeof(RectTransform), typeof(Text), typeof(InputField), typeof(Dropdown), typeof(Toggle),
            typeof(Scrollbar), typeof(ScrollRect), typeof(Mask), typeof(VerticalLayoutGroup)
        };

        var curWorld = u1Component.GetWorld();
        TArray<AActor> allActors = new TArray<AActor>();
        UGameplayStatics.GetAllActorsOfClass(curWorld, AActor.StaticClass(), ref allActors);
        foreach (var actor in allActors)
        {
            if (actor == null)
                continue;
            GameObject go = GameObject.GetFromActorOrCreate(actor);
            if (go == null)
                continue;
            foreach (var uiWidgetClass in uiWidgetTypesToInitialize)
            {
                var comp = go.GetComponent(uiWidgetClass);
                if (comp == null)
                    continue;
                var method = comp.GetType().GetMethod("Awake",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(comp, null);
                }
            }
        }
    }

    public void Start()
    {
    }

    public void Update()
    {
        // UKismetSystemLibrary.PrintString(this, "MonoBehaviour Update Run..");
    }
}