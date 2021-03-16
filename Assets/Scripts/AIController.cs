using System.Collections;
using UnityEngine;
using static UnityEngine.Mathf;
using static UnityEngine.Physics2D;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
sealed class AIController : MonoBehaviour
{
    #region Parameters
    [SerializeField] private AIAnimatorParameters animatorParameters;
    [SerializeField] private bool jump = true;
    [SerializeField]
    private float attackDistance = 0.6f, damage = 5.0f, groundCheckDistance = 0.5f, idleTime = 2.5f,
        maxHealthPoints = 100.0f, movementSpeed = 1.0f, playerCheckDistance = 4.0f, wallCheckDistance = 0.25f,
        jumpTime = 0.6f;
    [SerializeField] private int attackPatterns = 2;
    [SerializeField] private Transform groundCheck = null, jumpPointCheck = null;
    [SerializeField] private LayerMask whatIsGround, whatIsPlayer;

    private enum AIState { Chaising, Dead, Hit, Idle, Jump, Walking }
    private AIState currentState = AIState.Idle;
    private AIState previousState;

    private bool canAttack, canSee,canJump, isGrounded, isIdle, playerDetected, wallDetected, lookToRight;
    private float currentHealthPoints, randomAttackValue, percentage, timeStartLerp;
    private sbyte facingDirection;

    private Animator animator = null;
    private CapsuleCollider2D capsuleCollider2D = null;
    private Rigidbody2D rigidBody2D = null;
    private SpriteRenderer spriteRenderer = null;
    private Vector2 movement, jumpStart, jumpEnd;
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        animator = GetComponent<Animator>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        animator.SetBool(animatorParameters.isAlive, true);
        rigidBody2D.freezeRotation = true;

        currentState = AIState.Idle;
        lookToRight = isIdle = true;

        currentHealthPoints = maxHealthPoints;
        timeStartLerp = percentage = 0.0f;
        randomAttackValue = 1.0f;
        facingDirection = 1;
    }

    private void Update()
    {
        if (currentHealthPoints <= 0.0f)
        {
            SwitchState(AIState.Dead);
        }

        switch (currentState)
        {
            case AIState.Chaising:
                UpdateChaisingState();
                break;
            case AIState.Dead:
                UpdateDeadState();
                break;
            case AIState.Hit:
                UpdateHitState();
                break;
            case AIState.Idle:
                UpdateIdleState();
                break;
            case AIState.Jump:
                UpdateJumpState();
                break;
            case AIState.Walking:
                UpdateWalkingState();
                break;
        }
    }
    #endregion

    #region Finite State Machine
    #region Chaising state
    private void EnterChaisingState()
    {
        playerDetected = canAttack = false;
    }

    private void UpdateChaisingState()
    {
        playerDetected = Raycast(transform.position, transform.right, playerCheckDistance, whatIsPlayer);
        canAttack = Raycast(transform.position, transform.right, attackDistance, whatIsPlayer);
        isGrounded = Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        canJump = Raycast(jumpPointCheck.position, -jumpPointCheck.up, groundCheckDistance, whatIsGround);

        if (playerDetected)
        {
            if (canAttack)
            {
                //Attack
                rigidBody2D.velocity = Vector2.zero;
                animator.SetFloat(animatorParameters.movementSpeed, Abs(rigidBody2D.velocity.x));

                animator.SetFloat(animatorParameters.attackValue, randomAttackValue);
            }
            else
            {
                animator.SetFloat(animatorParameters.attackValue, 0.0f);
                if (jump && !isGrounded && canJump)
                {
                    previousState = currentState;
                    SwitchState(AIState.Jump);
                }
                else if(!jump && !isGrounded)
                {
                    Rotation();
                    SwitchState(AIState.Idle);
                }
                else
                {
                    Movement();
                }
            }
        }
        else
        {
            Rotation();
            SwitchState(AIState.Idle);
        }
    }

    private void ExitChaisingState()
    {
        playerDetected = canAttack = false;
    }
    #endregion

    #region Dead state
    private void EnterDeadState()
    {
        DisableComponents();

        spriteRenderer.sortingOrder -= 1;

        animator.SetBool(animatorParameters.isAlive, false);
    }

    private void UpdateDeadState()
    {

    }

    private void ExitDeadState()
    {
        currentHealthPoints = maxHealthPoints;

        EnableComponents();

        spriteRenderer.sortingOrder += 1;

        animator.SetBool(animatorParameters.isAlive, true);
    }
    #endregion

    #region Hit state
    private void EnterHitState()
    {
        rigidBody2D.velocity = Vector2.zero;
        animator.SetBool(animatorParameters.isHit, true);
    }

    private void UpdateHitState()
    {
        rigidBody2D.velocity = Vector2.zero;
    }

    private void ExitHitState()
    {
        animator.SetBool(animatorParameters.isHit, false);
    }
    #endregion

    #region Idle state
    private void EnterIdleState()
    {
        isIdle = true;
    }

    private void UpdateIdleState()
    {
        rigidBody2D.velocity = Vector2.zero;
        animator.SetFloat(animatorParameters.movementSpeed, Abs(rigidBody2D.velocity.x));

        if (isIdle)
        {
            StartCoroutine(ToggleStateByTime(idleTime, AIState.Walking));
            isIdle = false;
        }

        if (Raycast(transform.position, transform.right, playerCheckDistance, whatIsPlayer))
        {
            StopCoroutine(nameof(ToggleStateByTime));
            SwitchState(AIState.Chaising);
        }
    }

    private void ExitIdleState()
    {
        isIdle = false;
    }
    #endregion

    #region Jump state
    private void EnterJumpState()
    {
        animator.SetBool(animatorParameters.isJumping, true);
        DisableComponents();

        timeStartLerp = Time.time;
        jumpStart = transform.position;

        if (lookToRight)
        {
            jumpEnd = transform.position + jumpPointCheck.localPosition;
        }
        else
        {
            jumpEnd.x = transform.position.x - jumpPointCheck.localPosition.x;
            jumpEnd.y = transform.position.y + jumpPointCheck.localPosition.y;
        }
    }


    private void UpdateJumpState()
    {
        if (percentage <= 1.0f)
        {
            percentage = (Time.time - timeStartLerp) / jumpTime;

            transform.position = Vector3.Lerp(jumpStart, jumpEnd, percentage);
        }
        else 
        {
            SwitchState(previousState);
        }
    }

    private void ExitJumpState()
    {
        EnableComponents();

        animator.SetBool(animatorParameters.isJumping, false);
        percentage = 0.0f;
    }
    #endregion

    #region Walking state
    private void EnterWalkingState()
    {
        canSee = isGrounded = wallDetected = false;
        animator.SetFloat(animatorParameters.attackValue, 0.0f);
    }

    private void UpdateWalkingState()
    {
        isGrounded = Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        wallDetected = Raycast(transform.position, transform.right, wallCheckDistance, whatIsGround);

        if (!isGrounded || wallDetected)
        {
            Rotation();
        }
        else
        {
            Movement();
        }

        canSee = Linecast(transform.position, PlayerController.Instance.transform.position, whatIsGround);
        playerDetected = Raycast(transform.position, transform.right, playerCheckDistance, whatIsPlayer);

        if (!canSee && playerDetected && isGrounded)
        {
            SwitchState(AIState.Chaising);
        }
    }

    private void ExitWalkingState()
    {
        canSee = playerDetected = false;
        animator.SetFloat(animatorParameters.movementSpeed, 0.0f);
    }
    #endregion
    #endregion

    #region Other methods
    internal void ApplyDamage(float damage)
    {
        currentHealthPoints -= damage;

        previousState = currentState;
        SwitchState(AIState.Hit);
    }

    private IEnumerator ToggleStateByTime(float time, AIState state)
    {
        yield return new WaitForSeconds(time);

        SwitchState(state);
    }

    /// <summary>
    /// Executable method in animations of Attack as an event
    /// </summary>
    private void DealDamage()
    {
        bool isHit = Raycast(transform.position, transform.right, attackDistance, whatIsPlayer);
        RaycastHit2D hit = Raycast(transform.position, transform.right, attackDistance, whatIsPlayer);

        if (isHit && hit.collider.gameObject.GetComponent<PlayerController>() != null)
        {
            hit.collider.gameObject.GetComponent<PlayerController>().ApplyDamage(damage);
        }
    }

    private void Movement()
    {
        movement.Set(movementSpeed * facingDirection, rigidBody2D.velocity.y);
        rigidBody2D.velocity = movement;

        animator.SetFloat(animatorParameters.movementSpeed, Abs(rigidBody2D.velocity.x));
    }

    /// <summary>
    /// Executable method in animations of Attack as an event
    /// </summary>
    private void NextAttack()
    {
        float maxValue = attackPatterns + 0.4f;
        randomAttackValue = Round(Random.Range(1.0f, maxValue));
    }

    private void Rotation()
    {
        facingDirection *= -1;
        transform.Rotate(0.0f, 180.0f, 0.0f);

        lookToRight = !lookToRight;
    }

    private void DisableComponents()
    {
        capsuleCollider2D.enabled = false;
        rigidBody2D.velocity = Vector2.zero;
        rigidBody2D.gravityScale = 0.0f;
    }

    private void EnableComponents()
    {
        capsuleCollider2D.enabled = true;
        rigidBody2D.gravityScale = 1.0f;
    }

    private void SwitchState(AIState state)
    {
        switch (currentState)
        {
            case AIState.Chaising:
                ExitChaisingState();
                break;
            case AIState.Dead:
                ExitDeadState();
                break;
            case AIState.Hit:
                ExitHitState();
                break;
            case AIState.Idle:
                ExitIdleState();
                break;
            case AIState.Jump:
                ExitJumpState();
                break;
            case AIState.Walking:
                ExitWalkingState();
                break;
        }

        switch (state)
        {
            case AIState.Chaising:
                EnterChaisingState();
                break;
            case AIState.Dead:
                EnterDeadState();
                break;
            case AIState.Hit:
                EnterHitState();
                break;
            case AIState.Idle:
                EnterIdleState();
                break;
            case AIState.Jump:
                EnterJumpState();
                break;
            case AIState.Walking:
                EnterWalkingState();
                break;
        }
        currentState = state;
    }

    /// <summary>
    /// Executable method in animation Hit and Jump as an event
    /// </summary>
    private void TerminationOfAction() => SwitchState(previousState);
    #endregion

    #region Inner classes
    [System.Serializable]
    class AIAnimatorParameters
    {
        [SerializeField]
        internal string attackValue = "AttackValue", isAlive = "IsAlive", isHit = "IsHit", isJumping = "IsJumping",
            movementSpeed = "MovementSpeed";
    }
    #endregion
}
