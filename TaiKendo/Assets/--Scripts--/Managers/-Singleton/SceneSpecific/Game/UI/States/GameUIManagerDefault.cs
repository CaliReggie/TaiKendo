using UnityEngine;

public class GameUIManagerDefault : GameUIManager.GameUIManagerState
{
    public GameUIManagerDefault(GameUIManager.GameUIManagerContext context,
        GameUIManager.EGameUIState key,
        GameUIManager.EGameUIState[] invalidTransitions)
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
