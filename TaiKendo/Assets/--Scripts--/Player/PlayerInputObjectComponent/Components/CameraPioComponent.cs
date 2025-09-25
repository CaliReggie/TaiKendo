using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CameraPioComponent : PlayerInputObjectComponent
{
    /// <summary>
    /// The different types of cameras available to the player. Static turns off all virtual cameras, leaving MainPlayerCam
    /// on but static.
    /// </summary>
    public enum EPlayerCamType
    {
        Inactive,
        Fixed,
        FirstPerson,
        ThirdOrbit,
        ThirdFixed,
    }
    
    [Header("Inscribed References")]
    
    [Tooltip("Transform used for camera orientation reference")]
    [SerializeField] private Transform camOrientation;
    
    [Tooltip("The camera component of the main player camera")]
    [field: SerializeField] public Camera MainPlayerCam { get; private set; }
    
    [Tooltip("Virtual Camera to use for First Person view")]
    [SerializeField] private CinemachineCamera firstPersonVirtual;
    
    [Tooltip("Virtual Camera to use for Third Person Orbit view")]
    [SerializeField] private CinemachineCamera thirdPersonOrbitVirtual;
    
    [Tooltip("Virtual Camera to use for Third Person Fixed view")]
    [SerializeField] private CinemachineCamera thirdPersonFixedVirtual;
    
    [Header("Inscribed Settings")]

    [Tooltip("The default target camera type to set in initialization.\n\n" +
             "Inactive: All cameras off. " +
             "(Note: For player UI to be visible, " +
             "cannot be inactive as the player UI is an overlay canvas on the MainPlayerCam)\n\n" +
             "Fixed: MainPlayerCam on, static, no dynamic follow but position set on configure.\n\n" +
             "FirstPerson: First person view, follows orientation target.\n\n" +
             "ThirdOrbit: Third person orbit view, follows orientation target.\n\n" +
             "ThirdFixed: Third person fixed view, follows orientation target.")]
    [SerializeField] private EPlayerCamType defaultPlayerCamType = EPlayerCamType.FirstPerson;
    
    [Tooltip("The default position target for camera type. Can be null for no follow.")]
    [SerializeField] private Transform defaultTargetPosition;
    
    [Tooltip("Vertical rotation range for the camera orientation")]
    [SerializeField] private Vector2 verticalRotRange = new (-89, 89);

    [Header("Dynamic Settings - Don't modify in Inspector")]

    [Tooltip("Exists for serialization purposes. No effect. Click away!")]
    [SerializeField] private bool emptyBool;
    
    [field: SerializeField] public EPlayerCamType PlayerCamType  { get; private set; }
    
    [Header("Dynamic References - Don't modify in Inspector")]
    
    [SerializeField] private PlayerInput pioPlayerInput; // cached PlayerInput component on the same GameObject
    
    [SerializeField] private CinemachineBrain brain; // cached CinemachineBrain component on MainPlayerCam
    
    [SerializeField] private CinemachineOrbitalFollow thirdOrbitalComponent; // orbital comp for driving input axis
    
    [Tooltip("Current look input from player, set by OnLook method.")]
    [SerializeField] private Vector2 lookInput;
    
    [Tooltip("Current rotation target for the camera type.")]
    [SerializeField] private Vector3 targetEulerRotation;
    
    [Tooltip("Current position target for the camera type. Can be null for no follow.")]
    [SerializeField] private Transform targetPosition;
    
    [Tooltip("Current Virtual Camera if the current target type supports it, null otherwise.")]
    [SerializeField] private CinemachineCamera virtualCam;
    
    /// <summary>
    /// Called by message from PlayerInput component when the camera type input action is performed.
    /// </summary>
    public void OnLook(InputValue value)
    {
        Vector2 inputVector = value.Get<Vector2>();
        
        lookInput = inputVector;
    }
    
    /// <summary>
    /// Configure player camera with specified type and position target. Must be initialized.
    /// </summary>
    public void ConfigureCamera(EPlayerCamType targetType, Transform targetPos)
    {
        if (!Initialized)
        {
            Debug.LogError($"{ GetType().Name}: Cannot configure camera before initialization.");
            
            return;
        }
        
        // deactivate the current virtual camera if it exists
        if (virtualCam != null)
        {
            virtualCam.enabled = false;
            virtualCam.gameObject.SetActive(false);
            UnpairCameraChannels(virtualCam);
        }
        
        //set the position target. null (no follow) by default is allowed.
        targetPosition = targetPos;
        
        // Set the player cam type to the target type.
        PlayerCamType = targetType;
        
        switch (PlayerCamType)
        {
            case EPlayerCamType.Inactive:
                
                virtualCam = null;
                
                //turn off the MainPlayerCam
                MainPlayerCam.enabled = false;
                MainPlayerCam.gameObject.SetActive(false);
                
                break;
            
            case EPlayerCamType.Fixed:
                
                virtualCam = null;
                
                //set cam position if target assigned
                if (targetPosition != null)
                {
                    MainPlayerCam.transform.
                        SetPositionAndRotation(targetPosition.position, targetPosition.rotation);
                }
                
                //turn on the MainPlayerCam if it was off
                MainPlayerCam.enabled = true;
                MainPlayerCam.gameObject.SetActive(true);
                
                break;
            case EPlayerCamType.FirstPerson:
                
                virtualCam = firstPersonVirtual;
                
                break;
            case EPlayerCamType.ThirdOrbit:
                
                virtualCam = thirdPersonOrbitVirtual;
                
                break;
            case EPlayerCamType.ThirdFixed:
                
                virtualCam = thirdPersonFixedVirtual;
                
                break;
        }

        // If we have a valid virtualCam, configure it
        if (virtualCam != null)
        {
            
            PairCameraChannels(virtualCam);
            
            //turn on virtual cam
            virtualCam.enabled = true;
            virtualCam.gameObject.SetActive(true);
            
            //turn on the MainPlayerCam if it was off
            MainPlayerCam.enabled = true;
            MainPlayerCam.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Ensures certain settings are correct
    /// </summary>
    private void OnValidate()
    {
        if (verticalRotRange.x < -89)
        {
            verticalRotRange.x = -89;
        }
        
        if (verticalRotRange.y > 89)
        {
            verticalRotRange.y = 89;
        }
    }
    
    protected override void Initialize()
    {
        if (!CheckInscribedReferences())
        {
            return;
        }
        
        if (!SetDynamicReferences())
        {
            return;
        }
        
        //make virtual cams orient with orientation target
        InitVirtualCamFollows();
        
        //set default target position
        InitTargetPosition(defaultTargetPosition);
        
        // ensure all cams starting off
        InitAllCameras();
        
        Initialized = true;
        
        return;
        
        bool CheckInscribedReferences()
        {
            if (camOrientation == null ||
                MainPlayerCam == null ||
                firstPersonVirtual == null ||
                thirdPersonOrbitVirtual == null ||
                thirdPersonFixedVirtual == null)
            {
                Debug.LogError($"{ GetType().Name}:" +
                               $" One or more inscribed references are not set in the Inspector on {gameObject.name}.");
                return false;
            }
            
            return true;
        }
        
        bool SetDynamicReferences()
        {
            try
            {
                thirdOrbitalComponent = thirdPersonOrbitVirtual.GetComponent<CinemachineOrbitalFollow>();
                
                pioPlayerInput = Pio.GetComponent<PlayerInput>();
                
                brain = MainPlayerCam.GetComponent<CinemachineBrain>();
                
                if (thirdOrbitalComponent == null ||
                    pioPlayerInput == null ||
                    brain == null)
                {
                    Debug.LogError($"{ GetType().Name}: Error setting dynamic references.");
                    
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{ GetType().Name}: Error setting dynamic references: {e.Message}");
                
                return false;
            }
            
            return true;
        }
        
        // makes virtual cams follow the orientation target
        void InitVirtualCamFollows()
        {
            firstPersonVirtual.LookAt = camOrientation;
            
            thirdPersonOrbitVirtual.LookAt = camOrientation;
                
            thirdPersonFixedVirtual.LookAt = camOrientation;
        }
        
        void InitTargetPosition(Transform targetPos)
        {
            targetPosition = defaultTargetPosition;
        }
        
        // turns all off and ensures unpairs
        void InitAllCameras()
        {
                MainPlayerCam.enabled = false;
                MainPlayerCam.gameObject.SetActive(false);
                
                firstPersonVirtual.enabled = false;
                firstPersonVirtual.gameObject.SetActive(false);
                PairCameraChannels(firstPersonVirtual);
            
                thirdPersonOrbitVirtual.enabled = false;
                thirdPersonOrbitVirtual.gameObject.SetActive(false);
                PairCameraChannels(thirdPersonOrbitVirtual);
                
                thirdPersonFixedVirtual.enabled = false;
                thirdPersonFixedVirtual.gameObject.SetActive(false);
                PairCameraChannels(thirdPersonFixedVirtual);
        }
    }
    
    /// <summary>
    /// Takes a CineMachine Virtual Camera and pairs its output channel with the attached CineMachineBrain
    /// </summary>
    private void PairCameraChannels(CinemachineCamera virtualCam)
    {
        if (!Initialized) return;
        
        if (virtualCam == null) return;
        
        // works for 1-4 players. splitscreen is used because while when leaving,
        // current playerIndex is updated to fill the gap, splitScreenIndex remains constant for the player.
        // this could change if I added manual split screen adjustment and removed auto assignment. (Logic exists
        // in PlayerInputManager) 
        // Essentially, splitScreenIndex corresponds to visual location of player
        int index = pioPlayerInput.splitScreenIndex;
        
        brain.ChannelMask = (OutputChannels)(1 << index); // 1,2,4,8 for channels 0-3
        
        virtualCam.OutputChannel = brain.ChannelMask;
    }
    
    private void UnpairCameraChannels(CinemachineCamera virtualCam)
    {
        if (!Initialized) return;
        
        if (virtualCam == null) return;

        virtualCam.OutputChannel = OutputChannels.Default;
    }
    
    protected override void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.ObjectInitialize:
            case PlayerInputObject.EPlayerInputObjectState.Inactive:
            case PlayerInputObject.EPlayerInputObjectState.ApplicationUI:
                
                ConfigureCamera(EPlayerCamType.Inactive, targetPosition);
                
                enabled = false;
                break;
        }
    }
    
    protected override void OnAfterPioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.Player:
                
                ConfigureCamera(defaultPlayerCamType, defaultTargetPosition);
                
                enabled = true;
                
                break;
            
            case PlayerInputObject.EPlayerInputObjectState.PlayerUI:
                
                ConfigureCamera(EPlayerCamType.Fixed, null);
                
                enabled = true;
                
                break;
        }
    }
    
    private void Update()
    {
        bool isVirtualCamType = PlayerCamType != EPlayerCamType.Inactive &&
                                PlayerCamType != EPlayerCamType.Fixed;
        
        if (!Initialized || !isVirtualCamType)
        {
            return;
        }
        
        UpdateOrientationPosition();
        
        UpdateOrientationRotation();
        
        return; 
        
        // following position target if assigned
        void UpdateOrientationPosition()
        {
            if (targetPosition != null)
            {
                camOrientation.position = targetPosition.transform.position;
            }
        }
        
        // updating orientation rotation based on look input
        void UpdateOrientationRotation()
        {
            targetEulerRotation.y += lookInput.x * Time.deltaTime;
            
            targetEulerRotation.x -= lookInput.y * Time.deltaTime;
            
            targetEulerRotation.x = Mathf.Clamp(targetEulerRotation.x, verticalRotRange.x, verticalRotRange.y);
            
            targetEulerRotation.y = Mathf.Repeat(targetEulerRotation.y, 360);
            
            camOrientation.rotation = Quaternion.Euler(targetEulerRotation.x, targetEulerRotation.y, 0);
            
            // drive third person orbital component based on orientation if active
            bool thirdOrbitActive = PlayerCamType == EPlayerCamType.ThirdOrbit && thirdOrbitalComponent != null;
        
            if (thirdOrbitActive)
            {
                thirdOrbitalComponent.HorizontalAxis.Value = targetEulerRotation.y;
                
                thirdOrbitalComponent.VerticalAxis.Value = targetEulerRotation.x;
            }
        }
    }
}
