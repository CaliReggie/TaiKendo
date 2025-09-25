using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(GraphicRaycaster))]
public class MainMenuUIManager : BaseStateManagerApplicationListener<MainMenuUIManager, MainMenuUIManager.EMainMenuUIState>
{
    public enum EMainMenuUIState
    {
        Default
    }
    
    [SerializeField] private MainMenuUIManagerContext context;

    #region BaseMethods
    
    protected override void SetInstanceType()
    {
        InstanceType = EInstanceType.Singleton;
    }
    
    protected override void Initialize()
    {
        States = context.ContextInitialize(this);
    }
    
    protected override void OnBeforeApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode)
        {
            Debug.Log($"[{GetType().Name}] OnBeforeApplicationStateChange: {toState}");
        }
    }
    
    protected override void OnAfterApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode)
        {
            Debug.Log($"[{GetType().Name}] OnAfterApplicationStateChange: {toState}");
        }
    }
    
    #endregion
    
    [Serializable]
    public class MainMenuUIManagerContext : BaseStateMachineContext
    {
        #region ContextDeclarations

        [Header("Dynamic References - Don't Modify In Inspector")]
        
        public MainMenuUIManager mainMenuUIManager;

        #endregion
        
        #region BaseMethods
        
        protected override Dictionary<EMainMenuUIState, EMainMenuUIState[]> StatesDict()
        {
            return new Dictionary<EMainMenuUIState, EMainMenuUIState[]>()
            {
                { EMainMenuUIState.Default, new EMainMenuUIState[]{ } } //No invalid transitions for Default state
            };
        }

        protected override Dictionary<EMainMenuUIState, BaseState<EMainMenuUIState>> InitializedStates()
        {
            Dictionary<EMainMenuUIState, BaseState<EMainMenuUIState>> states =
                new Dictionary<EMainMenuUIState, BaseState<EMainMenuUIState>>();
            
            foreach (var state in StatesDict())
            {
                switch (state.Key)
                {
                    case EMainMenuUIState.Default:
                        states.Add(state.Key, new MainMenuUIManagerDefault(this, state.Key, state.Value));
                        break;
                }
            }

            return states;
        }

        public override Dictionary<EMainMenuUIState, BaseState<EMainMenuUIState>> ContextInitialize(BaseStateMachine<EMainMenuUIState> targetStateMachine)
        {
            mainMenuUIManager = (MainMenuUIManager)targetStateMachine;
            
            return InitializedStates();
        }
        
        public override void ContextCallChangeState(EMainMenuUIState newState)
        {
            mainMenuUIManager.ChangeState(newState);
        }
        
        #endregion
    }
    
    public abstract class MainMenuUIManagerState : BaseState<EMainMenuUIState>
    {
        protected MainMenuUIManagerContext Context { get; }
        
        protected MainMenuUIManagerState(MainMenuUIManagerContext context,
            EMainMenuUIState key,
            EMainMenuUIState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
    }
}
