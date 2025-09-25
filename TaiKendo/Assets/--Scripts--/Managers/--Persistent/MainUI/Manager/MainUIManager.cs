using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(GraphicRaycaster))]
public class MainUIManager : BaseStateManagerApplicationListener<MainUIManager, MainUIManager.EMainUIState>
{
    /// <summary>
    /// States the MainUIManager can be in.
    /// </summary>
    public enum EMainUIState
    {
        Default,
        TransitionInScene,
        TransitionOutScene,
    }

    [SerializeField] private MainUIManagerContext context;

    #region BaseMethods

    protected override void SetInstanceType()
    {
        InstanceType = EInstanceType.PersistentSingleton;
    }
    
    protected override void Initialize()
    {
        States = context.ContextInitialize(this);
    }
    
    protected override void OnBeforeApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode)
        {
            Debug.Log($"{GetType().Name}: OnBeforeApplicationStateChange to: {toState}");
        }
    }
    
    protected override void OnAfterApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode)
        {
            Debug.Log($"{GetType().Name}: OnAfterApplicationStateChange to: {toState}");
        }
        
        switch (toState)
        {   
            case ApplicationManager.EApplicationState.LoadingScene:
                
                ChangeState(EMainUIState.TransitionInScene);
                
                break;
            
            case ApplicationManager.EApplicationState.ExitingScene:
                
                ChangeState(EMainUIState.TransitionOutScene);
                
                break;
        }
    }
    
    #endregion
    
    [Serializable]
    public class MainUIManagerContext : BaseStateMachineContext
    {
        #region ContextDeclarations
        
        [Header("Inscribed References")]
        
        public Transform transitionImage;
        
        [Header("Transition Locations")]
        
        public Transform transitionOnScreenLoc;
        
        public Transform transitionOffScreenLoc;
        
        [Header("Transition Curves and Durations")]

        public AnimationCurve transitionInCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        public float transitionInDuration = 1f;
        
        [Space]
        
        public AnimationCurve transitionOutCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        public float transitionOutDuration = 1f;
        
        [Header("Dynamic References - Don't Modify In Inspector")]
        
        public MainUIManager mainUIManager;
        
        #endregion
        
        #region BaseMethods
        
        protected override Dictionary<EMainUIState, EMainUIState[]> StatesDict()
        {
            return new Dictionary<EMainUIState, EMainUIState[]>
            {
                {EMainUIState.Default, new EMainUIState[] {}}, // No invalid transitions for Default state
                {EMainUIState.TransitionInScene, new EMainUIState[] { }}, // No invalid transitions for TransitionInScene state
                {EMainUIState.TransitionOutScene, new EMainUIState[] { }}, // No invalid transitions for TransitionOutScene state
            };
        }

        protected override Dictionary<EMainUIState, BaseState<EMainUIState>> InitializedStates()
        {
            Dictionary<EMainUIState, BaseState<EMainUIState>> states = 
                new Dictionary<EMainUIState, BaseState<EMainUIState>>();
            
            foreach (var state in StatesDict())
            {
                switch (state.Key)
                {
                    case EMainUIState.Default:
                        states.Add(state.Key, new MainUIManagerDefault(this, state.Key, state.Value));
                        break;
                    case EMainUIState.TransitionInScene:
                        states.Add(state.Key, 
                            new MainUIManagerTransitionInScene(this, state.Key, state.Value));
                        break;
                    case EMainUIState.TransitionOutScene:
                        states.Add(state.Key, 
                            new MainUIManagerTransitionOutScene(this, state.Key, state.Value));
                        break;
                }
            }
            
            return states;
        }

        public override Dictionary<EMainUIState, BaseState<EMainUIState>> 
            ContextInitialize(BaseStateMachine<EMainUIState> targetStateMachine)
        {
            mainUIManager = (MainUIManager) targetStateMachine;

            return InitializedStates();
        }
        
        public override void ContextCallChangeState(EMainUIState newState)
        {
            mainUIManager.ChangeState(newState);
        }

        #endregion
    }
    
    public abstract class MainUIManagerState : BaseState<EMainUIState>
    {
        
        protected MainUIManagerState(MainUIManagerContext context, 
            EMainUIState key, 
            EMainUIState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
        
        protected MainUIManagerContext Context { get; }
    }
}
