using UnityEngine;
public class MainUIManagerTransitionOutScene : MainUIManager.MainUIManagerState
{
    public MainUIManagerTransitionOutScene(MainUIManager.MainUIManagerContext context,
        MainUIManager.EMainUIState key,
        MainUIManager.EMainUIState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }
    
    private float _transitionOutTimeLeft;

    public override void EnterState()
    {
        Context.transitionImage.transform.position = Context.transitionOffScreenLoc.position; // uncover screen
        
        _transitionOutTimeLeft = Context.transitionOutDuration; // Set transition time left
    }
    
    public override void UpdateState()
    {
        // transition does not depend on time scale
        _transitionOutTimeLeft -= Time.unscaledDeltaTime;
        
        float transitionPercentageLeft = _transitionOutTimeLeft / Context.transitionOutDuration;
        
        if (transitionPercentageLeft <= 0) // Done transition if no time left
        {
            Context.ContextCallChangeState(MainUIManager.EMainUIState.Default);
            return;
        }
        
        // Use transition percentage to sample transition out anim curve
        float i = 1 - Context.transitionOutCurve.Evaluate(transitionPercentageLeft);
        
        // Set transition image position based on anim curve
        Context.transitionImage.transform.position = 
            Vector3.Lerp(Context.transitionOffScreenLoc.position, 
                         Context.transitionOnScreenLoc.position, i);
    }

    public override void ExitState()
    {
    }
}
