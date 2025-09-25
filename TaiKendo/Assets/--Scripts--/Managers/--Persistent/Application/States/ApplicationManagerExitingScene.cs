using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationManagerExitingScene : ApplicationManager.ApplicationManagerState
{
    public ApplicationManagerExitingScene(ApplicationManager.ApplicationManagerContext context,
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
        // If mainUIManager exists
        if (MainUIManager.Instance != null)
        {
            // don't load the scene till the transition out is complete
            if (MainUIManager.Instance.CurrentState.State == MainUIManager.EMainUIState.TransitionOutScene) return;
        }
        
        // change to loading will exit this update loop
        Context.ContextCallChangeState(ApplicationManager.EApplicationState.LoadingScene);
    }
    public override void ExitState()
    {
    }
}
