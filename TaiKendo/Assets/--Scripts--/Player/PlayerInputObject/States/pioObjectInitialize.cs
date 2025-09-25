using UnityEngine;

public class pioObjectInitialize : PlayerInputObject.NewPlayerInputObjectState
{
    public pioObjectInitialize(PlayerInputObject.PlayerInputObjectContext context,
        PlayerInputObject.EPlayerInputObjectState key,
        PlayerInputObject.EPlayerInputObjectState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }
    
    public override void EnterState()
    {
        // Disable all input by setting to None action map
        Context.SetCurrentInputActionMap(PlayerInputObject.PlayerInputObjectContext.EInputActionMap.None);
    }
    
    public override void UpdateState()
    {
        // wait till components are initialized to switch to inactive state
        bool allComponentsInitialized = 
            Context.playerInputObjectComponents.TrueForAll(component => component.Initialized);
        
        if (allComponentsInitialized)
        {
            Context.objectInitialized = true;
            
            Context.ContextCallChangeState(PlayerInputObject.EPlayerInputObjectState.Inactive);
        }
    }
    
    public override void ExitState()
    {
    }
}