using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerObjectPioComponent))]
[RequireComponent(typeof(CameraPioComponent))]
[RequireComponent(typeof(CursorPioComponent))]
[RequireComponent(typeof(UIPioComponent))]
public class PlayerInputObject : BaseStateMachine<PlayerInputObject.EPlayerInputObjectState>
{
    public enum EPlayerInputObjectState
    {
        ObjectInitialize, // Settings refs and waiting on initialized components
        Inactive, // No components or input maps active
        Player, // Playable object and camera active, no player ui or cursor. Player map active
        PlayerUI, // Playable object and camera active, player ui and cursor active. UI map active
        ApplicationUI // Cursor active, no player or camera. UI map active
    }
     
    
    [SerializeField] private PlayerInputObjectContext context;
    
    /// <summary>
    /// Flag to know if this Pio and it's components are initialized and ready to use.
    /// </summary>
    public bool Initialized => context.objectInitialized;

    #region BaseMethods

    protected override void Initialize()
    {
        States = context.ContextInitialize(this);
    }

    private void OnDestroy()
    {
        // unsubscribe from player manager event if exists
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnAfterTargetPlayerStateChange -= context.ContextCallChangeState;
        }
    }

    /// <summary>
    /// The PlayerInputObject modifies the behaviour of the base state machine. Instead of internal state calls as the
    /// primary method of changing state, the Pio has a target state and actively changes based on an attempt to match
    /// the current state to the target state. This allows for external management of the target state along with still
    /// having internal state logic and transitions for the structured behaviour of the Pio.
    /// </summary>
    protected override void Update()
    {
        base.Update();
        
        if (CurrentState.State != context.targetState)
        {
            ChangeState(context.targetState);
        }
    }

    #endregion

    #region InputMessageMethods

    /// <summary>
    /// Public message to be received by the player's clone of InputActions in the PlayerInput component. Should be named
    /// whatever the action is called in the InputActions asset. This action is intended to toggle between modes of play.
    /// Logic is variable.
    /// If a PlayerManager exists, the request to toggle is sent to the manager and this will respond to that change.
    /// If not, default toggle logic is used based on inspector settings.
    ///
    /// While abstracted to default and alternate states. Think about it simply like play / pause. It is done like
    /// this to allow for use across variable desired types of scenes. (e.g. gameplay or UI focused scenes).
    /// </summary>
    public void OnAlternatePlayerState(InputValue value)
    {
        if (!Started)
        {
            return;
        }
        
        EPlayerInputObjectState currentState = CurrentState.State;
        
        // logic for toggling depends on current state and existence of managers or not
        if (value.isPressed)
        {
            // if player manager, set request alternate state and rest is handled by management and this listening
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.PlayerRequestToggleTargetPlayerState();
            }
            //if solo logic
            else
            {
                // swapping default to alternate
                if (currentState == context.defaultState)
                {
                    context.SetTargetState(context.defaultAlternateState);
                }
                //swapping alternate to default
                else if (currentState == context.defaultAlternateState)
                {
                    context.SetTargetState(context.defaultState);
                }
                //swapping something else to default
                else
                {
                    
                    context.SetTargetState(context.defaultState);
                }
            }
        }
    }
    
    #endregion
    
    [Serializable]
    public class PlayerInputObjectContext : BaseStateMachineContext 
    {
        public enum EInputActionMap
        {  
            None,
            Player,
            UI 
        }
    
        private static readonly Dictionary<EInputActionMap, string> ActionMapTypeNames = new ()
        {
            { EInputActionMap.None, null }, 
            { EInputActionMap.Player, "Player" },
            { EInputActionMap.UI, "UI" }
        };
        
        #region ContextDeclarations
        
        [Header("Inscribed Settings")]
        
        [Tooltip("The default state for this Pio to start in if no PlayerManager exists to manage it.\n\n"+
                 "Object Initialize: DO NOT SET AS DEFAULT. This state is for initialization only.\n\n" +
                 "Inactive: No components or input maps active. DO NOT SET IF NOT MANAGED BY PM.\n\n" +
                 "Player: Player Object and Camera are active with Player input map.\n\n" +
                 "PlayerUI: Player UI and Cursor are active, Object and Camera enabled in background, with UI input map.\n\n" +
                 "ApplicationUI: Only Cursor active, with UI input map. DO NOT SET IF NOT MANAGED BY PM.")]
        public EPlayerInputObjectState defaultState = EPlayerInputObjectState.Player;
        
        [Tooltip("The default alternate state for this Pio to toggle to if no PlayerManager exists to manage it.\n\n" +
                 "Object Initialize: DO NOT SET AS DEFAULT. This state is for initialization only.\n\n" +
                 "Inactive: No components or input maps active. DO NOT SET IF NOT MANAGED BY PM.\n\n" +
                 "Player: Player Object and Camera are active with Player input map.\n\n" +
                 "PlayerUI: Player UI and Cursor are active, Object and Camera enabled in background, with UI input map.\n\n" +
                 "ApplicationUI: Only Cursor active, with UI input map. DO NOT SET IF NOT MANAGED BY PM.")]
        public EPlayerInputObjectState defaultAlternateState = EPlayerInputObjectState.PlayerUI;

        [Header("Dynamic References - Don't Modify In Inspector")]
        
        public PlayerInputObject playerInputObject;
        
        public PlayerInput playerInput;

        public List<PlayerInputObjectComponent> playerInputObjectComponents;

        [Header("Dynamic Settings - Don't Modify In Inspector")]

        public bool objectInitialized;
        
        public EPlayerInputObjectState targetState;

        #endregion
        
        #region BaseMethods
        
        protected override Dictionary<EPlayerInputObjectState, EPlayerInputObjectState[]> StatesDict()
        {
            return new Dictionary<EPlayerInputObjectState, EPlayerInputObjectState[]>
            {
                { EPlayerInputObjectState.ObjectInitialize, Array.Empty<EPlayerInputObjectState>() }, // Go anywhere
                { EPlayerInputObjectState.Inactive, new [] { EPlayerInputObjectState.ObjectInitialize }}, // No re init
                { EPlayerInputObjectState.Player, new [] { EPlayerInputObjectState.ObjectInitialize } }, // No re init
                { EPlayerInputObjectState.PlayerUI, new [] { EPlayerInputObjectState.ObjectInitialize } }, // No re init
                { EPlayerInputObjectState.ApplicationUI, new [] { EPlayerInputObjectState.ObjectInitialize } } // No re init
            };
        }

        protected override Dictionary<EPlayerInputObjectState, BaseState<EPlayerInputObjectState>> InitializedStates()
        {
            Dictionary<EPlayerInputObjectState, BaseState<EPlayerInputObjectState>> states = new();

            foreach (var state in StatesDict())
            {
                switch (state.Key)
                {
                    case EPlayerInputObjectState.ObjectInitialize:
                        states.Add(state.Key, new pioObjectInitialize(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.Inactive:
                        states.Add(state.Key, new pioInactive(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.Player:
                        states.Add(state.Key, new pioPlayer(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.PlayerUI:
                        states.Add(state.Key, new pioPlayerUI(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.ApplicationUI:
                        states.Add(state.Key, new pioGameUI(this, state.Key, state.Value));
                        break;
                }
            }
            
            return states;
        }

        public override Dictionary<EPlayerInputObjectState, BaseState<EPlayerInputObjectState>> 
            ContextInitialize(BaseStateMachine<EPlayerInputObjectState> targetStateMachine)
        {
            playerInputObject = (PlayerInputObject) targetStateMachine;
            
            playerInput = playerInputObject.GetComponent<PlayerInput>();

            // split screen index is used as it corresponds to the visual location of the player
            int playerNameIndex = playerInput.splitScreenIndex;

            // renaming GO
            playerInputObject.name = ($"Player{playerNameIndex}InputObject");
            
            //init Pio component list
            playerInputObjectComponents = new List<PlayerInputObjectComponent>(
                playerInputObject.GetComponents<PlayerInputObjectComponent>());
            
            // try to get child components as well
            try 
            { 
                playerInputObjectComponents = new List<PlayerInputObjectComponent>(
                    playerInputObject.GetComponents<PlayerInputObjectComponent>());
            }
            // designed to use them so warn if none found
            catch (Exception e)
            {
                Debug.LogWarning($"No child components found for {playerInputObject.name}\n{e}");
            }
            
            // if managed by Player Manager, set to its target state and subscribe to state changes,
            // else start in default state
            if (PlayerManager.Instance != null)
            {
                targetState = PlayerManager.Instance.TargetPlayerState;

                PlayerManager.Instance.OnAfterTargetPlayerStateChange += SetTargetState;
            }
            else
            {
                targetState = defaultState;
            }
             
            return InitializedStates();
        }

        public override void ContextCallChangeState(EPlayerInputObjectState newState)
        {
            playerInputObject.ChangeState(newState);
        }

        #endregion
        
        #region StateMethods
        
        /// <summary>
        /// Method to change target state. Should be the typical method of changing state for the Pio, but can
        /// override with ContextCallChangeState if needed. Issues could arise if lots of invalid transitions
        /// start to be defined.
        /// </summary>
        public void SetTargetState(EPlayerInputObjectState newState)
        {
            targetState = newState;
        }
        
        /// <summary>
        /// Function to set the current input action map. Disables all other action maps first.
        /// If target map is None, all maps are disabled. Should be used when transitioning to/from states based
        /// on the desired map for relevant listener components to be able to function by receiving messages from
        /// the correct map.
        /// </summary>
        public void SetCurrentInputActionMap(EInputActionMap targetActionMapType)
        {
            //disable all action maps
            foreach (var actionMap in playerInput.actions.actionMaps)
            {
                actionMap.Disable();
            }
            
            // nothing to do if no map
            if (targetActionMapType == EInputActionMap.None)
            {
                return;
            }

            try
            {
                // set current map
                playerInput.currentActionMap = playerInput.actions.FindActionMap(ActionMapTypeNames[targetActionMapType]);
                
                // enable current map
                playerInput.currentActionMap.Enable();
                
            }
            catch (Exception e)
            {
                Debug.LogError($"{playerInputObject.name}: Error in setting current action map: \n{e}");
            }
            
            if (playerInputObject.DebugMode)
            {
                Debug.Log($"{playerInputObject.name}: Set current action map to {targetActionMapType}");
            }
        }
        
        #endregion
    }
    
    public abstract class NewPlayerInputObjectState : BaseState<EPlayerInputObjectState>
    {
        protected NewPlayerInputObjectState(PlayerInputObjectContext context, 
            EPlayerInputObjectState key, 
            EPlayerInputObjectState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
        
        protected PlayerInputObjectContext Context { get; }
    }
}
