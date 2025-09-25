using UnityEngine;

public class ApplicationManagerRunning : ApplicationManager.ApplicationManagerState
{
    public ApplicationManagerRunning(ApplicationManager.ApplicationManagerContext context,
        ApplicationManager.EApplicationState key,
        ApplicationManager.EApplicationState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }
    
    public override void EnterState()
    {
    }
    
    public override void ExitState()
    {
    }
    
    public override void UpdateState()
    {
    }

}
