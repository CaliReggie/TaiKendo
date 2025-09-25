using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(GraphicRaycaster))]
public class GameUIManager : BaseStateManagerApplicationListener<GameUIManager, GameUIManager.EGameUIState>
{
    public enum EGameUIState
    {
        Default
    }
    
    [SerializeField] private GameUIManagerContext context;
    
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
    public class GameUIManagerContext : BaseStateMachineContext
    {
        #region ContextDeclarations

        [Header("Dynamic References - Don't Modify In Inspector")]
        public GameUIManager gameUIManager;
        
        
        #endregion
        
        #region BaseMethods

        protected override Dictionary<EGameUIState, EGameUIState[]> StatesDict()
        {
            return new Dictionary<EGameUIState, EGameUIState[]>()
            {
                { EGameUIState.Default, new EGameUIState[]{ } } //No invalid transitions for Default state
            };
        }

        protected override Dictionary<EGameUIState, BaseState<EGameUIState>> InitializedStates()
        {  
            Dictionary<EGameUIState, BaseState<EGameUIState>> states
                = new Dictionary<EGameUIState, BaseState<EGameUIState>>();

            foreach (var state in StatesDict())
            {
                switch (state.Key)
                {
                    case EGameUIState.Default:
                        states.Add(state.Key, new GameUIManagerDefault(this, state.Key, state.Value));
                        break;
                }
            }

            return states;
        }

        public override Dictionary<EGameUIState, BaseState<EGameUIState>> ContextInitialize(BaseStateMachine<EGameUIState> targetStateMachine)
        {
            gameUIManager = targetStateMachine as GameUIManager;

            return InitializedStates();
        }

        public override void ContextCallChangeState(EGameUIState newState)
        {
            gameUIManager.ChangeState(newState);
        }

        #endregion
    }
    
    public abstract class GameUIManagerState : BaseState<EGameUIState>
    {
        protected GameUIManagerContext Context { get; }
        
        protected GameUIManagerState(GameUIManagerContext context,
            EGameUIState key,
            EGameUIState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
    }
}
