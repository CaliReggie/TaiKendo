using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerObjectPioComponent : PlayerInputObjectComponent
{
    public enum EPlayerObjectState
    {
        Inactive,
        Idle,
        Walking,
        Jumping,
        Falling
    }
    
    [Header("Inscribed References")]
    
    [SerializeField] private Transform playerObject;
    
    [Header("Inscribed Settings")]
    
    [Tooltip("The speed and which player object will move when move controls are used.")]
    [SerializeField] private float walkSpeed = 5f;
    
    [Tooltip("The real height the player will jump dependant on Physics gravity settings.")]
    [SerializeField] private float jumpHeight = 2f;
    
    [Tooltip("The time after pressing jump that playerObject will still attempt to jump (for jump forgiveness in air)")]
    [SerializeField] private float jumpCoyoteTimeBuffer = 0.2f;
    
    [Tooltip("The speed at which the player object will rotate towards target move when in a non-fixed player camera.")]
    [SerializeField] [Range(0,1)] private float playerRotationEasing = 0.1f;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [SerializeField] private CharacterController characterController; // attached to playerObject
    
    [SerializeField] private CameraPioComponent cameraPioComponent; // attached to Pio
    
    [SerializeField] private Transform lookOrientation; // the look orientation of player (usually camera)
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [Tooltip("The raw move input received from players input regardless of view orientation.")]
    [SerializeField] Vector3 rawMoveInput;
    
    [Tooltip("The move input oriented to the current lookOrientation.")]
    [SerializeField] Vector3 orientedMoveInput;
    
    [Tooltip("The target world space move vector the player object will move towards.")]
    [SerializeField] Vector3 targetMove;
    
    [Tooltip("The target euler rotation the player object will rotate towards.")]
    [SerializeField] Vector3 targetEulerRotation;
    
    [SerializeField] private bool isGrounded;
    
    [Tooltip("The time left that the coyote jump buffer is active.")]
    [SerializeField] private float jumpCoyoteTimer;
    
    /// <summary>
    /// Automatically true if coyote jump buffer is active (jump was recently requested).
    /// </summary>
    private bool JumpRequested => jumpCoyoteTimer > 0f;
    
    [Tooltip("The current state of the player object based on player conditions.")]
    [SerializeField] private EPlayerObjectState currentState;
    
    /// <summary>
    /// Message to be received from players clone of InputActions when move input is performed.
    /// </summary>
    public void OnMove(InputValue value)
    {
        Vector2 inputValue = value.Get<Vector2>();
        
        rawMoveInput = new Vector3(inputValue.x, 0f, inputValue.y);
    }

    /// <summary>
    /// Message to be received from players clone of InputActions when jump button is pressed.
    /// </summary>
    public void OnJump(InputValue buttonValue)
    {
        if (buttonValue.isPressed)
        {
            jumpCoyoteTimer = jumpCoyoteTimeBuffer;
        }
    }
    
    protected override void Initialize()
    {
        if (!CheckInscribedReferences()) return;

        if (!SetDynamicReferences()) return;
        
        // ensure starts off
        TogglePlayerObject(false);
        
        Initialized = true;
        
        return;
        
        bool CheckInscribedReferences()
        {
            if (playerObject == null)
            {
                Debug.LogError($"{GetType().Name}: Error checking inscribed references.");
                
                return false;
            }
            
            return true;
        }
        
        bool SetDynamicReferences()
        {
            try
            {
                characterController = playerObject.GetComponent<CharacterController>();
                
                cameraPioComponent = Pio.GetComponent<CameraPioComponent>();

                lookOrientation = cameraPioComponent.MainPlayerCam.transform;
                
                if (characterController == null ||
                    cameraPioComponent == null ||
                    lookOrientation == null)
                {
                    Debug.LogError($"{GetType().Name}: Error setting dynamic references.");
                    
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{GetType().Name}: Exception setting dynamic references: {e.Message}");
                
                return false;
            }
            
            return true;
        }
    }
    
    protected override void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.ObjectInitialize:
            case PlayerInputObject.EPlayerInputObjectState.Inactive:
            case PlayerInputObject.EPlayerInputObjectState.ApplicationUI:

                TogglePlayerObject(false);
                
                enabled = false;
                
                break;
            
        }
    }
    
    protected override void OnAfterPioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.Player:
            case PlayerInputObject.EPlayerInputObjectState.PlayerUI:
                
                TogglePlayerObject(true);
                
                enabled = true;
                
                break;
        }
    }
    
    private void TogglePlayerObject(bool active)
    {
        if (playerObject != null)
        {
            playerObject.gameObject.SetActive(active);
        }
        
        // if off set inactive state
        if (!active) currentState = EPlayerObjectState.Inactive;
    }

    private void FixedUpdate()
    {
        if (!Initialized) { return; }
        
        //move the character controller based on targetMove
        characterController.Move(Time.fixedDeltaTime * targetMove);
    }

    private void Update()
    {
        if (!Initialized) { return; }
        
        ManageCooldownsAndTimers();

        ManageGrounded();

        OrientMoveInput();

        ManageTargetMove();

        RotatePlayerObject();
        
        ManageCurrentState();
        
        return;
        
        void ManageCooldownsAndTimers()
        {
            // counting down jump coyote timer (and JumpRequested by extension)
            if (jumpCoyoteTimer > 0f)
            {
                jumpCoyoteTimer -= Time.deltaTime;
                
                if (jumpCoyoteTimer < 0f)
                {
                    jumpCoyoteTimer = 0f;
                }
            }
        }

        void ManageGrounded()
        {
            // sample from player controller
            isGrounded = characterController.isGrounded;
        }

        void OrientMoveInput()
        {
            // oriented input is raw input relative to look orientation
            orientedMoveInput = lookOrientation.forward * rawMoveInput.z + lookOrientation.right * rawMoveInput.x;
        
            // don't allow vertical movement from move input
            orientedMoveInput.y = 0f;
        
            // normalize to prevent faster diagonal movement
            orientedMoveInput.Normalize();
        }

        void ManageTargetMove()
        {
            // lateral management
            targetMove.x = orientedMoveInput.x * walkSpeed;
            
            targetMove.z = orientedMoveInput.z * walkSpeed;
            
            if (isGrounded)
            {
                // if jump requested, jump
                if (JumpRequested)
                {
                    jumpCoyoteTimer = 0f; //reset coyote time (also resets JumpRequested)
                    
                    targetMove.y = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y); // use grav for calc jump height
                }
                // if targetMove isn't upward (just jumped), set to small downward force to keep grounded
                else if (targetMove.y <= 0f)
                {
                    targetMove.y = -0.1f; //small downward force to keep grounded
                }
            }
            // if not grounded
            else
            {
                targetMove.y += Physics.gravity.y * Time.deltaTime; // add gravity when not grounded
            }
        }

        void RotatePlayerObject()
        {
            // depends on players view type
            switch (cameraPioComponent.PlayerCamType)
            {
                // nothing if player can't see or is fixed (likely in UI)
                case CameraPioComponent.EPlayerCamType.Inactive:
                case CameraPioComponent.EPlayerCamType.Fixed:
                    break;
                
                // if a strict view follow type of virtual camera, face look orientation dir
                case CameraPioComponent.EPlayerCamType.FirstPerson:
                case CameraPioComponent.EPlayerCamType.ThirdFixed:
                    
                    targetEulerRotation = Quaternion.LookRotation(lookOrientation.forward).eulerAngles;
                
                    playerObject.rotation = Quaternion.Euler(0f, targetEulerRotation.y, 0f);
                    
                    break;
                
                // if third orbit virtual camera, rotate to oriented move input dir
                case CameraPioComponent.EPlayerCamType.ThirdOrbit:
                    
                    // don't rotate if no move input
                    if (orientedMoveInput != Vector3.zero)
                    {
                        //target rot is lerped from playerObject rot to oriented move input dir
                        targetEulerRotation = 
                            Quaternion.Lerp(playerObject.rotation, Quaternion.LookRotation(orientedMoveInput), 
                            playerRotationEasing).eulerAngles;
                        
                        
                        playerObject.rotation = Quaternion.Euler(0f, targetEulerRotation.y, 0f);
                    }
                    
                    break;
            }
        }
        
        void ManageCurrentState()
        {
            EPlayerObjectState previousState = currentState;
            
            if (isGrounded)
            {
                // if grounded and trying to move walking
                if (orientedMoveInput.magnitude > 0f)
                {
                    currentState = EPlayerObjectState.Walking;
                }
                // otherwise idle
                else
                {
                    currentState = EPlayerObjectState.Idle;
                }
            }
            else
            {
                // if not grounded and target move is up, jumping
                if (targetMove.y > 0f)
                {
                    currentState = EPlayerObjectState.Jumping;
                }
                // otherwise must be falling
                else
                {
                    currentState = EPlayerObjectState.Falling;
                }
            }

            // change state if needed
            if (previousState != currentState)
            {
                ChangeState(currentState);
            }
            
            return;
            
            // State is set dependent on player object conditions. This should not be used to set state directly.
            // (e.g), setting to idle to stop player movement is incorrect.
            // Instead, it reflects current player state and logic for handling things like animations
            // and other logic or effects. 
            void ChangeState(EPlayerObjectState newState)
            {
                if (currentState == newState) { return; }
                
                if (debugMode)
                {
                    Debug.Log($"{GetType().Name}: {Pio.gameObject.name} changing to state: {newState}");
                }
                
                currentState = newState;
            }
        }
    }
}
