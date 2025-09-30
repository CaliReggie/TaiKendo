using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Android;
using Random = UnityEngine.Random;

public class PlayerController1 : MonoBehaviour
{
    public static PlayerController1 s;
    CharacterController controller;
    Animator animator;
    AudioSource audioSource;

    [Header("Controller")]
    public float moveSpeed = 5;
    public float gravity = -9.8f;
    public float jumpHeight = 1.2f;

    Vector3 _PlayerVelocity;

    bool isGrounded;
    
    Vector2 moveInput;
    
    Vector2 lookInput;

    private CameraPioComponent playerCamScript;

    float xRotation = 0f;
    
    public void OnMove(InputValue movementValue)
    {
        Vector2 input = movementValue.Get<Vector2>();
        
        moveInput = input;
    }
    
    public void OnLook(InputValue lookValue)
    {
        Vector2 input = lookValue.Get<Vector2>();
        
        lookInput = input;
    }
    
    public void OnJump(InputValue buttonValue)
    {
        if (buttonValue.isPressed)
        {
            if (isGrounded) _PlayerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }
    
    public void OnBlock(InputValue buttonValue)
    {
        if (buttonValue.isPressed)
        {
            blocking = true;
            shield.SetActive(true);
        }
        else
        {
            blocking = false;
            aimingInput = Vector2.zero;
            shield.SetActive(false);
        }
    }
    
    public void OnLockOnTarget(InputValue buttonValue)
    {
        if (buttonValue.isPressed)
        {
            LockOn();
        }
    }
    
    public void OnAttack(InputValue attackValue)
    {
        if (attackValue.isPressed)
        {
            if(!readyToAttack || attacking || aiming || blocking) return;
            
            aimEndTime = Time.time + aimingDuration;
            aiming = true;
        }
    }

    void Awake()
    {
        s = this;
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        // this script is child of playableObjectPIOcomponent. Therefore. Get that, (it has PIO) ref, get the playerCamScript
        // component, and get the main player playerCamScript from that.
        try
        {
            playerCamScript = GetComponentInParent<PlayerObjectPioComponent>().Pio.GetComponent<CameraPioComponent>();
        }
        catch (Exception e)
        {
            Debug.LogError("PlayerController1: Error assigning camera. " + e);
        }
    }
    
    void OnDisable()
    {
        RESET();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        SetAnimations();
    }

    void FixedUpdate() 
    { MoveInput(moveInput); }

    void LateUpdate() 
    { LookInput(lookInput); }

    void MoveInput(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        controller.Move(transform.TransformDirection(moveDirection) * moveSpeed * Time.deltaTime);
        _PlayerVelocity.y += gravity * Time.deltaTime;
        if(isGrounded && _PlayerVelocity.y < 0)
            _PlayerVelocity.y = -2f;
        controller.Move(_PlayerVelocity * Time.deltaTime);
    }

    void LookInput(Vector2 input)
    {
        if (aiming || blocking)
        {
            aimingInput += input;
            if (blocking)
            {
                DetermineAttackDir();
                if (shield == null)
                {
                    return;
                }
                switch (attackDirection)
                {
                    case Direction.Left:
                        shield.transform.position = shieldPosition.transform.GetChild(0).position;
                        break;
                    case Direction.Right:
                        shield.transform.position = shieldPosition.transform.GetChild(1).position;
                        break;
                    default:
                        shield.transform.position = shieldPosition.transform.GetChild(2).position;
                        break;
                }

                return;
            }
            if (Time.time > aimEndTime)
            {
                aiming = false;
                DetermineAttackDir();
                Attack();
                aimingInput = Vector2.zero;
            }
        }
    }

    // ---------- //
    // ANIMATIONS //
    // ---------- //

    public const string IDLE = "Idle";
    public const string WALK = "Walk";
    public const string ATTACK1 = "Attack 1";
    public const string ATTACK2 = "Attack 2";

    string currentAnimationState;

    public void ChangeAnimationState(string newState) 
    {
        // STOP THE SAME ANIMATION FROM INTERRUPTING WITH ITSELF //
        if (currentAnimationState == newState) return;

        // PLAY THE ANIMATION //
        currentAnimationState = newState;
        animator.CrossFadeInFixedTime(currentAnimationState, 0.2f);
    }

    void SetAnimations()
    {
        // If player is not attacking
        if(!attacking)
        {
            if(_PlayerVelocity.x == 0 &&_PlayerVelocity.z == 0)
            { ChangeAnimationState(IDLE); }
            else
            { ChangeAnimationState(WALK); }
        }
    }

    // ------------------- //
    // ATTACKING BEHAVIOUR //
    // ------------------- //

    [Header("Attacking")]
    public float attackDistance = 3f;
    public float attackDelay = 0.4f;
    public float attackSpeed = 1f;
    public int attackDamage = 1;
    public LayerMask attackLayer;

    public GameObject hitEffect;
    public AudioClip swordSwing;
    public AudioClip hitSound;

    bool attacking = false;
    bool readyToAttack = true;
    int attackCount;
    
    //additions
    public float lockDistance = 10;
    public float aimingDuration = 0.25f;
    public bool blocking = false;
    public GameObject shieldPosition;
    public GameObject shield;
    private bool aiming;
    private float aimEndTime = 0;
    public Vector2 aimingInput = Vector2.zero;
    public Direction hitDirection = Direction.Right;
    public Material waveMaterial;
    private Coroutine waveCoroutine;
    public float waveDuration = 2.0f;
    public Vector3 hitPosition;
    public float waveSpread = 0.25f;
    public float startSpread = 0.1f;
    public enum Direction
    {
        Left,
        Right,
        Top
    }
    private Direction attackDirection = Direction.Right;
    private bool locked;
    private GameObject lockTarget;

    void SimulateHit()
    {
        if (hitDirection == attackDirection && blocking)
        {
            HitShield();
            return;
        }
        HitPlayer();
    }

    void HitShield()
    {
        Debug.Log("Hit Shield");
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        waveCoroutine = StartCoroutine(WaveCoroutine());
    }

    IEnumerator WaveCoroutine()
    {
        Vector2 screenPosition = Camera.main.WorldToViewportPoint(shield.transform.position);
        waveMaterial.SetVector("_Origin", screenPosition);
        waveMaterial.SetFloat("_Active", 1);
        float t = startSpread;
        while (t <= waveSpread + startSpread)
        {
            t += Time.deltaTime * waveSpread/ waveDuration;
            waveMaterial.SetFloat("_DistFromCenter", t);
            yield return null;
        }
        waveMaterial.SetFloat("_DistFromCenter", 0);
        waveMaterial.SetFloat("_Active", 0);
    }

    void HitPlayer()
    {
        Debug.Log("Hit Player");
    }
    
    void DetermineAttackDir()
    {
        Vector2 inp = aimingInput;
        float upMag = Vector2.Dot(inp, Vector2.up);
        float rightMag = Mathf.Abs(Vector2.Dot(inp, Vector2.right));
        if (upMag > rightMag)
        {
            attackDirection = Direction.Top;
        }
        else
        {
            if (inp.x < 0)
            {
                attackDirection = Direction.Left;
            }
            else
            {
                attackDirection = Direction.Right;
            }
        }
    }

    void LockOn()
    {
        if (locked)
        {
            locked = false;

            playerCamScript.ConfigureTargetLock(false);
            
            return;
        }
        if(Physics.Raycast(playerCamScript.MainPlayerCam.transform.position,
               playerCamScript.MainPlayerCam.transform.forward,
               out RaycastHit hit, lockDistance, attackLayer))
        {
            if (hit.collider.gameObject.TryGetComponent(out Actor actor))
            {
                locked = true;
                
                lockTarget = hit.collider.gameObject;
                
                playerCamScript.ConfigureTargetLock(true, lockTarget.transform);
            }
            
        } 
    }
    void Attack()
    {
        readyToAttack = false;
        attacking = true;

        Invoke(nameof(ResetAttack), attackSpeed);
        Invoke(nameof(AttackRaycast), attackDelay);

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(swordSwing);

        if (attackDirection == Direction.Left)
        {
            ChangeAnimationState(ATTACK1);
        }
        else
        {
            ChangeAnimationState(ATTACK2);
        }
    }

    void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
    }

    void AttackRaycast()
    {
        if(Physics.Raycast(playerCamScript.MainPlayerCam.transform.position,
               playerCamScript.MainPlayerCam.transform.forward,
               out RaycastHit hit, attackDistance, attackLayer))
        { 
            HitTarget(hit.point);

            if(hit.transform.TryGetComponent<Actor>(out Actor T))
            { T.TakeDamage(attackDamage); }
        } 
    }

    void HitTarget(Vector3 pos)
    {
        audioSource.pitch = 1;
        audioSource.PlayOneShot(hitSound);

        GameObject GO = Instantiate(hitEffect, pos, Quaternion.identity);
        Destroy(GO, 20);
    }
    
    void RESET()
    {
        aiming = false;
        aimingInput = Vector2.zero;
        aimEndTime = 0;
        blocking = false;
        if (shield != null)
        {
            shield.SetActive(false);
        }
        if (locked)
        {
            locked = false;
            lockTarget = null;
            playerCamScript.ConfigureTargetLock(false);
        }
        
        //for now no spawn pos so tp player object to world 0,0,0
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    public void EnemyDeath(GameObject enemy)
    {
        if (enemy == lockTarget)
        {
            lockTarget = null;
            if (locked)
            {
                LockOn();
            }
        }
    }
}