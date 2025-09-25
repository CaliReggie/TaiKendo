using UnityEngine;

public class ApplicationManagerAlternateRunning : ApplicationManager.ApplicationManagerState
{
    public ApplicationManagerAlternateRunning(ApplicationManager.ApplicationManagerContext context,
        ApplicationManager.EApplicationState key,
        ApplicationManager.EApplicationState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }
    
    public override void EnterState()
    {
        if (Context.activeSceneSO != null)
        {
            Time.timeScale = Context.activeSceneSO.PauseTimeInAlternateRunning ? 0f : 1f;
        }
    }
    
    public override void UpdateState()
    {
        
    }
    
    public override void ExitState()
    {
        Time.timeScale = 1f; // Resume the game by setting time scale back to 1
    }
}
