using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManagerRemovingInput : PlayerManager.InputManagerState
{
    public PlayerManagerRemovingInput(PlayerManager.PlayerManagerContext context,
        PlayerManager.EPlayerManagementState key,
        PlayerManager.EPlayerManagementState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }

    public override void EnterState()
    {
        Context.inputManagerComponent.DisableJoining(); // Disable player joining input
        
        // Remove players until the number of players matches the target
        while (Context.NumPlayers > Context.targetPlayers)  // CHEEKY WHILE LOOP ??!! :D (could it break?)
        {
            Context.RemovePlayer(Context.NumPlayers); 
        }
    }

    public override void UpdateState()
    {
        if (!Context.NeedLessPlayers) // If no longer need to remove players
        {
            Context.ContextCallChangeState(PlayerManager.EPlayerManagementState.Default); // back to Default state
            
            return;
        }
        
        //TODO: Maybe add logic handle if logic on enter didn't work?
    }

    public override void ExitState()
    {
    }
}
