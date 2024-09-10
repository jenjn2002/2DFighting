using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour, IPlayerController
{
    [SerializeField] public Stats stats;
    public Rigidbody2D rb2d;
    public CapsuleCollider2D col;
    public FrameInput frameInput;
    public Vector2 frameVelocity;
    public bool catchQuerryStartInCollider;
    public GameObject effectPos;
    public GameObject effectPrefabs;

    #region Interface
    public Vector2 FrameInput => frameInput.move;
    public event Action<bool, float> groundChanged;
    public event Action jumped;
    public event Action attacked;
    public event Action blocked;
    public event Action casted;
    public event Action dashed;
    #endregion

    public float time;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        catchQuerryStartInCollider = Physics2D.queriesStartInColliders;
    }

    private void Update()
    {
        time += Time.deltaTime;
        GatherInput();
    }

    private void FixedUpdate()
    {
        CheckCollision();

        HandleJump();  
        HandleDirection();
        HandleGravity();
        HandleDash();
        HandleAttack();
        HandleBlock();
        HandleCast();
        

        ApplyMovement();

    }

    private void GatherInput()
    {
        frameInput = new FrameInput
        {
            jumpDown = UnityEngine.Input.GetKeyDown(KeyCode.C) || UnityEngine.Input.GetButtonDown("Jump"),
            jumpHeld = UnityEngine.Input.GetKey(KeyCode.C) || UnityEngine.Input.GetButton("Jump"),
            move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical")),
            attack = UnityEngine.Input.GetKeyDown(KeyCode.Mouse0),
            block = UnityEngine.Input.GetKey(KeyCode.Mouse1),
            cast = UnityEngine.Input.GetKeyDown(KeyCode.G),
            dash = UnityEngine.Input.GetKeyDown(KeyCode.F)
        };

        if (stats.snapInput)
        {
            frameInput.move.x = Mathf.Abs(frameInput.move.x) < stats.horizontalDeadzone ? 0 : Mathf.Sign(frameInput.move.x);
            frameInput.move.y = Mathf.Abs(frameInput.move.y) < stats.verticalDeadzone ? 0 : Mathf.Sign(frameInput.move.y);
        }

        if (frameInput.jumpDown)
        {
            jumpToConsume = true;
            timeJumpWasPressed = time;
        }

        if (frameInput.dash)
        {
            dashToConsume = true;
        }
    }

    #region Collisions
    public float frameLeftGrounded = float.MinValue;
    public bool grounded;

    public void CheckCollision()
    {
        Physics2D.queriesStartInColliders = false;

        bool groundHit = Physics2D.CapsuleCast(col.bounds.center, col.size, col.direction, 0, Vector2.down, stats.grounderDistance, ~stats.playerLayer);
        bool ceilingHit = Physics2D.CapsuleCast(col.bounds.center, col.size, col.direction, 0, Vector2.up, stats.grounderDistance, ~stats.playerLayer);

        if (ceilingHit)
        {
            frameVelocity.y = MathF.Min(0, frameVelocity.y);
        }

        if(!grounded && groundHit)
        {
            grounded = true;
            coyateUsable = true;
            bufferJumpUsable = true;
            endJumpEarly = true;
            groundChanged?.Invoke(true, MathF.Abs(frameVelocity.y));
        }
        else if (grounded && !groundHit)
        {
            grounded = false;
            frameLeftGrounded = time;
            groundChanged?.Invoke(false, 0);
        }

        Physics2D.queriesStartInColliders = catchQuerryStartInCollider;

    }

    #endregion

    #region Cast

    public void HandleCast()
    {
        if(frameInput.cast) ExecuteCast();
    }

    public void ExecuteCast()
    {
        casted?.Invoke();
    }

    #endregion


    #region Block

    public void HandleBlock()
    {
        if (frameInput.block) ExecuteBlock();
    }

    public void ExecuteBlock()
    {
        blocked?.Invoke();
    }

    #endregion


    #region Attack

    public void HandleAttack()
    {
        if(frameInput.attack) ExecuteAttack();
    }

    public void ExecuteAttack()
    {
        attacked?.Invoke();
    }

    #endregion

    #region Dash

    public bool dashToConsume;
    
    public void HandleDash()
    {
        if (!dashToConsume) return;
        ExecuteDash();
        dashToConsume = false;
    }

    public void ExecuteDash()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite.flipX == false)
        {
            frameVelocity.x = stats.dashPower;
        }
        else frameVelocity.x = -stats.dashPower;
        
        dashed?.Invoke();
    }
    #endregion

    #region Jump
    public bool jumpToConsume;
    public bool bufferJumpUsable;
    public bool endJumpEarly;
    public bool coyateUsable;
    public float timeJumpWasPressed;

    public bool hasBufferJump => bufferJumpUsable && time < timeJumpWasPressed + stats.jumpBuffer;
    public bool canUseCoyate => coyateUsable && time < frameLeftGrounded + stats.coyoteTime;

    public void HandleJump()
    {
        if (!endJumpEarly && !grounded && !frameInput.jumpHeld && rb2d.velocity.y > 0) endJumpEarly = true;

        if (!jumpToConsume && !hasBufferJump) return;

        if (grounded || canUseCoyate) ExecuteJump();
        jumpToConsume = false;
    }

    private void ExecuteJump()
    {
        endJumpEarly = false;
        timeJumpWasPressed = 0;
        bufferJumpUsable = false;
        coyateUsable = false;
        frameVelocity.y = stats.jumpPower;
        jumped?.Invoke();
    }
    #endregion

    #region Horizontal

    public void HandleDirection()
    {
        if(frameInput.move.x == 0)
        {
            var deceleration = grounded ? stats.groundDeceleration : stats.airDeceleration;
            frameVelocity.x = Mathf.MoveTowards(frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            frameVelocity.x = Mathf.MoveTowards(frameVelocity.x, frameInput.move.x * stats.speed, stats.accelration * Time.fixedDeltaTime);
        }
    }
    #endregion

    #region Velocity
    /*public void HandleGravity()
    {
        if (grounded && frameVelocity.y <= 0f)
        {   
            frameVelocity.y = stats.groundingForce;
        }
        else
        {
            var inAirVelocity = stats.fallDeceleration;
            if (endJumpEarly && frameVelocity.y > 0f)
            {
                inAirVelocity *= stats.jumpEndEarlyGravityModifier;
            }
            frameVelocity.y = Mathf.MoveTowards(frameVelocity.y, stats.maxFallSpeed, inAirVelocity*Time.fixedDeltaTime);
        }
    }*/
    public void HandleGravity()
    {
        if (grounded && frameVelocity.y <= 0)
        {
            // Nếu player đang đứng trên mặt đất và không di chuyển lên, thiết lập vận tốc dọc
            frameVelocity.y = stats.groundingForce;
        }
        else
        {
            // Nếu player đang trong không trung, áp dụng trọng lực
            var inAirVelocity = stats.fallDeceleration;

            // Nếu cú nhảy kết thúc sớm, áp dụng trọng lực mạnh hơn
            if (endJumpEarly && frameVelocity.y > 0)
            {
                inAirVelocity *= stats.jumpEndEarlyGravityModifier;
            }

            // Cập nhật vận tốc dọc để mô phỏng rơi tự do
            frameVelocity.y = Mathf.MoveTowards(frameVelocity.y, -stats.maxFallSpeed, inAirVelocity * Time.fixedDeltaTime);
        }
    }

    #endregion

    public void ApplyMovement() => rb2d.velocity = frameVelocity;
    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
    #endif
}

public struct FrameInput
{
    public bool jumpDown;
    public bool jumpHeld;
    public Vector2 move;
    public bool attack;
    public bool block;
    public bool cast;
    public bool dash;
}

public interface IPlayerController
{
    public event Action<bool, float> groundChanged;
    public event Action jumped;
    public event Action attacked;
    public event Action blocked;
    public event Action casted;
    public event Action dashed;
    public Vector2 FrameInput { get; }
}
