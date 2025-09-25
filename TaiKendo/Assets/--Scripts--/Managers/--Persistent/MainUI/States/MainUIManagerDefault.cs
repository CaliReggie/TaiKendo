public class MainUIManagerDefault : MainUIManager.MainUIManagerState
{
    public MainUIManagerDefault(MainUIManager.MainUIManagerContext context,
        MainUIManager.EMainUIState key,
        MainUIManager.EMainUIState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }

    public override void EnterState()
    {
        // Logic depends on existence of AppManager
        if (Context.mainUIManager.IsApplicationManager) // If yes
        {
            //If application initialized and Running or AlternateRunning
            if (ApplicationManager.Instance.CurrentState != null && (
                ApplicationManager.Instance.CurrentState.State == ApplicationManager.EApplicationState.Running ||
                ApplicationManager.Instance.CurrentState.State == ApplicationManager.EApplicationState.AlternateRunning))
            {
                Context.transitionImage.transform.position =  // ensure uncovered screen
                    Context.transitionOffScreenLoc.position;
            }
            else // (Loading, Exiting, or Quitting)
            {
                Context.transitionImage.transform.position = // ensure covered screen
                Context.transitionOnScreenLoc.position;
            }
        }
        else // If no AppManager
        {
            Context.transitionImage.transform.position =
                Context.transitionOffScreenLoc.position; // ensure uncovered screen
        }
    }
    
    public override void UpdateState()
    {
    }
    
    public override void ExitState()
    {
    }

}