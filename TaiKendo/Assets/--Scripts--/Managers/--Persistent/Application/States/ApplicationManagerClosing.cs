using UnityEngine;

public class ApplicationManagerClosing : ApplicationManager.ApplicationManagerState
{
    public ApplicationManagerClosing(ApplicationManager.ApplicationManagerContext context,
        ApplicationManager.EApplicationState key,
        ApplicationManager.EApplicationState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }
    
    public override void EnterState()
    {
    }
    
    public override void UpdateState()
    {
        // after enter logic called, quit the application (this can be made to wait for other circumstance in future)
        if (Application.isPlaying)
        {
            
            // quit will exit this update loop
            Application.Quit();
        }
    }
    
    public override void ExitState()
    {
    }
}
