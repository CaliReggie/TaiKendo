using UnityEngine;

public class PlayerManagerDefault : PlayerManager.InputManagerState
{
    public PlayerManagerDefault(PlayerManager.PlayerManagerContext context,
        PlayerManager.EPlayerManagementState key,
        PlayerManager.EPlayerManagementState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }

    public override void EnterState()
    {
        Context.inputManagerComponent.DisableJoining(); // Disable player joining input
    }

    public override void UpdateState()
    {
        if (Context.NeedMorePlayers) // If target players is greater than current players
        { 
            Context.ContextCallChangeState(PlayerManager.EPlayerManagementState.InsufficientPlayers);
            
            return;
        }
        
        if (Context.NeedLessPlayers) // If target players is less than current players
        {
            Context.ContextCallChangeState(PlayerManager.EPlayerManagementState.ExcessPlayers);
            
            return;
        }
    }

    public override void ExitState()
    {
    }
}
