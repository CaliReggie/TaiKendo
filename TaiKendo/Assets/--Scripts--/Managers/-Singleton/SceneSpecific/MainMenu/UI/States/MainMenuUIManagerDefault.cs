using UnityEngine;

public class MainMenuUIManagerDefault : MainMenuUIManager.MainMenuUIManagerState
{
    public MainMenuUIManagerDefault(MainMenuUIManager.MainMenuUIManagerContext context,
        MainMenuUIManager.EMainMenuUIState key,
        MainMenuUIManager.EMainMenuUIState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }

    public override void EnterState()
    {
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
    }
}
