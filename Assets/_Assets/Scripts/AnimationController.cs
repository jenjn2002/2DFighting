using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer sprite;
    public PlayerController playerController;
    public IPlayerController player;
    private Action onJumped;
    private Action<bool, float> onGroundChanged;
    private Action onAttacked;
    private Action onDashed;
    private Action onBlocked;
    private Action onCasted;

    public float attackAnimateTime = 0.2f;
    public float castAnitmateTime = 0.8f;
    public float dashAnimateTime = 0.8f;
    public float blockAnimateTime = 0.8f;
    public float lockedTill;
    public bool jumped;
    public bool attacked;
    public bool grounded;
    public float impactGround;
    public bool dashed;
    public bool blocked;
    public bool casted;

    

    private void Awake()
    {
        animator= GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        sprite = GetComponent<SpriteRenderer>();
        player = GetComponentInChildren<IPlayerController>();
    }

    private void Start()
    {
        /*playerController.attacked += () 
            => { attacked = true; };
        playerController.jumped += ()
            => { jumpTrigger = true; };*/
    }

    private void Update()
    {   
        if (playerController.frameVelocity.x != 0) sprite.flipX = playerController.frameVelocity.x < 0;

        var state = GetState();
        attacked = false;
        jumped = false;
        dashed = false;
        casted = false;
        blocked = false;
        if (state == currentState) return;
        animator.CrossFade(state, 0.5f, 0);
        currentState = state;
        
    }

    private void OnEnable()
    {
        onJumped = () => { jumped = true; };
        onGroundChanged = (ground, impact) => { grounded = ground; impactGround = impact; };
        onAttacked = () => { attacked = true; };
        onDashed = () => { dashed = true; };
        onBlocked = () => { blocked = true; };
        onCasted = () => { casted = true; };

        player.jumped += onJumped;
        player.groundChanged += onGroundChanged;
        player.attacked += onAttacked;
        player.dashed += onDashed;
        player.blocked += onBlocked;
        player.casted += onCasted;

    }

    private void OnDisable()
    {
        player.jumped -= onJumped;
        player.groundChanged -= onGroundChanged;
        player.attacked -= onAttacked;
        player.dashed -= onDashed;
        player.blocked -= onBlocked;
        player.casted -= onCasted;

    }

    public int GetState()
    {
        if (Time.time < lockedTill) return currentState;


        if (dashed)
        {         
            GameObject dashPrefabs = Instantiate(playerController.effectPrefabs, playerController.effectPos.transform);
            Animator dashAnimator = dashPrefabs.GetComponent<Animator>();
            dashAnimator.Play("dash");
            Destroy(dashPrefabs, 0.3f);
            return LockState(dash, dashAnimateTime);
        }

        if (grounded)
        {
            if (attacked) return LockState(attack, attackAnimateTime);
            if (blocked) return LockState(block, blockAnimateTime);
            if (casted) return LockState(cast, castAnitmateTime);
        }
        else
        {
            return jump;
        }
        return playerController.frameVelocity.x == 0 ? idle : walk; 

        int LockState(int s, float t)
        {
            lockedTill = Time.time + t;
            return s;
        }

    }


    #region Animation 
    public int currentState;

    private static int idle = Animator.StringToHash("idle");
    private static int walk = Animator.StringToHash("walk");
    private static int jump = Animator.StringToHash("jump");
    private static int attack = Animator.StringToHash("attack");
    private static int block = Animator.StringToHash("block");
    private static int cast = Animator.StringToHash("cast");
    private static int dash = Animator.StringToHash("dash");
    #endregion
}
