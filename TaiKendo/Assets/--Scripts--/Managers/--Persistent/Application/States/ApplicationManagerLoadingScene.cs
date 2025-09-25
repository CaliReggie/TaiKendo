using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationManagerLoadingScene : ApplicationManager.ApplicationManagerState
{
    public ApplicationManagerLoadingScene(ApplicationManager.ApplicationManagerContext context,
        ApplicationManager.EApplicationState key,
        ApplicationManager.EApplicationState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }
    
    public override void EnterState()
    {
        // if already in target scene, can set it as active scene
        if (SceneSO.IsActiveScene(Context.targetSceneSO))
        {
            Context.SetActiveSceneSO(Context.targetSceneSO);
        }
        // if not, load the target scene
        else
        {
            SceneManager.LoadScene(Context.targetSceneSO.TryGetScenePathAsName());
        }
    }
    
    public override void UpdateState()
    {
        if (SceneSO.IsActiveScene(Context.targetSceneSO)) // If target scene is indeed active
        {
            //Ensure the active scene SO is set correctly
            Context.SetActiveSceneSO(Context.targetSceneSO);
            
            // Manually setting target scene to null (usually should use public method, but this is a safe time to
            // do so and keep things clean)
            Context.targetSceneSO = null;
            
            // Change to running state will exit this update loop
            Context.ContextCallChangeState(ApplicationManager.EApplicationState.Running);
        }
    }
    
    public override void ExitState()
    {
    }

}
