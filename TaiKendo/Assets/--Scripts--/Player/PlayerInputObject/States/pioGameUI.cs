using UnityEngine;

public class pioGameUI : PlayerInputObject.NewPlayerInputObjectState
{
    public pioGameUI(PlayerInputObject.PlayerInputObjectContext context,
        PlayerInputObject.EPlayerInputObjectState key,
        PlayerInputObject.EPlayerInputObjectState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }
    
    public override void EnterState()
    {
        // Switch to the UI action map
        Context.SetCurrentInputActionMap(PlayerInputObject.PlayerInputObjectContext.EInputActionMap.UI);
    }
    
    public override void UpdateState()
    {
    }
    
    public override void ExitState()
    {
    }
}
