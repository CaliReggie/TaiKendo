using UnityEngine;

public class MainUIManagerTransitionInScene : MainUIManager.MainUIManagerState
{
    public MainUIManagerTransitionInScene(MainUIManager.MainUIManagerContext context,
        MainUIManager.EMainUIState key,
        MainUIManager.EMainUIState[] invalidTransitions)
        : base(context, key, invalidTransitions)
    {
    }

    private float _transitionInTimeLeft;

    public override void EnterState()
    {
        Context.transitionImage.transform.position = Context.transitionOnScreenLoc.position; // cover screen
        
        _transitionInTimeLeft = Context.transitionInDuration; // Set transition time left
    }
    
    public override void UpdateState()
    {
        // transition does not depend on time scale
        _transitionInTimeLeft -= Time.unscaledDeltaTime;
        
        float transitionPercentageLeft = _transitionInTimeLeft / Context.transitionInDuration;
        
        if (transitionPercentageLeft <= 0) // Done transition if no time left
        {
            Context.ContextCallChangeState(MainUIManager.EMainUIState.Default);
            return;
        }
        
        // Use transition percentage to sample context transition out anim curve
        float i = 1 - Context.transitionInCurve.Evaluate(transitionPercentageLeft);
        
        // Set transition image position based on anim curve
        Context.transitionImage.transform.position = 
            Vector3.Lerp(Context.transitionOnScreenLoc.position, 
                         Context.transitionOffScreenLoc.position, i);
    }

    public override void ExitState()
    {
    }
}
