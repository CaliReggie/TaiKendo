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
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions input;
    CharacterController controller;
    Animator animator;
    AudioSource audioSource;

    [Header("Controller")]
    public float moveSpeed = 5;
    public float gravity = -9.8f;
    public float jumpHeight = 1.2f;

    Vector3 _PlayerVelocity;

    bool isGrounded;

    [Header("Camera")]
    public Camera cam;
    public float sensitivity;

    float xRotation = 0f;

    void Awake()
    {
        s = this;
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        playerInput = new InputSystem_Actions();
        input = playerInput.Player;
        AssignInputs();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        SetAnimations();
    }

    void FixedUpdate() 
    { MoveInput(input.Move.ReadValue<Vector2>()); }

    void LateUpdate() 
    { LookInput(input.Look.ReadValue<Vector2>()); }

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
        float mouseX = input.x;
        float mouseY = input.y;
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
        if (locked)
        {
            Vector3 dirToTarget = lockTarget.transform.position - cam.transform.position;
            Vector3 flatDir = new Vector3(dirToTarget.x, 0, dirToTarget.z);
            transform.forward = flatDir.normalized;
            float verticalAngle = Mathf.Atan2(dirToTarget.y, flatDir.magnitude) * Mathf.Rad2Deg;
            cam.transform.localRotation = Quaternion.Euler(-verticalAngle, 0, 0);
            return;
        }
        xRotation -= (mouseY * Time.deltaTime * sensitivity);
        xRotation = Mathf.Clamp(xRotation, -80, 80);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime * sensitivity));
    }

    void OnEnable() 
    { input.Enable(); }

    void OnDisable()
    { input.Disable(); }

    void Jump()
    {
        // Adds force to the player rigidbody to jump
        if (isGrounded)
            _PlayerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
    }

    void AssignInputs()
    {
        input.Jump.performed += ctx => Jump();
        input.Attack.started += ctx => InputAttack(ctx);
       /* input.LockOn.started += ctx => LockOn();
        input.Block.started += ctx => Block(ctx);
        input.Block.canceled += ctx => Block(ctx);
        input.SimulateHit.started += ctx => SimulateHit();*/
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
    
    void Block(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            blocking = true;
            shield.SetActive(true);
        }
        else if (ctx.canceled)
        {
            blocking = false;
            aimingInput = Vector2.zero;
            shield.SetActive(false);
        }
    }

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
    
    void InputAttack(InputAction.CallbackContext ctx)
    {
        if(!readyToAttack || attacking || aiming || blocking) return;
        if (ctx.started)
        {
            aimEndTime = Time.time + aimingDuration;
            aiming = true;
        }
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
            return;
        }
        if(Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, lockDistance, attackLayer))
        {
            if (hit.collider.gameObject.TryGetComponent<Actor>(out Actor actor))
            {
                locked = true;
                lockTarget = hit.collider.gameObject;
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
        if(Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, attackDistance, attackLayer))
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