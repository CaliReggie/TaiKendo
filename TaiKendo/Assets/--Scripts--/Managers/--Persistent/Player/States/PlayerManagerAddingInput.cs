using UnityEngine;

public class PlayerManagerAddingInput : PlayerManager.InputManagerState
{
    public PlayerManagerAddingInput(PlayerManager.PlayerManagerContext context,
        PlayerManager.EPlayerManagementState key,
        PlayerManager.EPlayerManagementState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }

    public override void EnterState()
    {
        Context.inputManagerComponent.EnableJoining(); // Enable player joining input
    }

    public override void UpdateState()
    {
        if (!Context.NeedMorePlayers) // If no longer need to add players
        {
            Context.inputManagerComponent.DisableJoining(); // Disable player joining input
            
            Context.ContextCallChangeState(PlayerManager.EPlayerManagementState.Default); // back to Default state
            
            return;
        }
    }

    public override void ExitState()
    {
    }
}
