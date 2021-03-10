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
    [SerializeField]
    private float attackDistance = 0.6f, damage = 5.0f, groundCheckDistance = 0.5f, idleTime = 2.5f,
        maxHealthPoints = 100.0f, movementSpeed = 1.0f,playerCheckDistance = 4.0f, wallCheckDistance = 0.25f;
    [SerializeField] private Transform groundCheck = null;
    [SerializeField] private LayerMask whatIsGround, whatIsPlayer;

    private enum AIState { Chaising, Dead, Hit, Idle, Walking }
    private AIState currentState = AIState.Idle;
    private AIState previousState;

    private bool canAttack, isGrounded, isIdle, playerDetected, wallDetected;
    private float currentHealthPoints, randomAttackValue;
    private sbyte facingDirection;

    private Animator animator = null;
    private CapsuleCollider2D capsuleCollider = null;
    private Rigidbody2D rigidBody2D = null;
    private SpriteRenderer spriteRenderer = null;
    private Vector2 movement;
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        animator.SetBool(animatorParameters.isAlive, true);

        currentState = AIState.Idle;
        isIdle = true;

        rigidBody2D.freezeRotation = true;
        spriteRenderer.flipX = false;

        currentHealthPoints = maxHealthPoints;
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
        playerDetected = false;
        canAttack = false;
    }

    private void UpdateChaisingState()
    {
        playerDetected = Raycast(transform.position, transform.right, playerCheckDistance, whatIsPlayer);
        canAttack = Raycast(transform.position, transform.right, attackDistance, whatIsPlayer);

        if (playerDetected)
        {
            if (canAttack)
            {
                //Attack
                rigidBody2D.velocity = Vector2.zero;
                animator.SetFloat(animatorParameters.movementSpeed, Abs(rigidBody2D.velocity.x));

                animator.SetBool(animatorParameters.isAttaking, canAttack);
                animator.SetFloat(animatorParameters.attackValue, randomAttackValue);
            }
            else
            {
                animator.SetFloat(animatorParameters.attackValue, 0.0f);
                animator.SetBool(animatorParameters.isAttaking, canAttack);
                Movement();
            }
        }
        else
        {
            SwitchState(AIState.Idle);
        }
    }

    private void ExitChaisingState()
    {
        playerDetected = false;
        canAttack = false;
    }
    #endregion

    #region Dead state
    private void EnterDeadState()
    {
        capsuleCollider.enabled = false;

        rigidBody2D.velocity = Vector2.zero;
        rigidBody2D.gravityScale = 0.0f;

        animator.SetBool(animatorParameters.isAttaking, false);
        animator.SetBool(animatorParameters.isAlive, false);
    }

    private void UpdateDeadState()
    {

    }

    private void ExitDeadState()
    {
        currentHealthPoints = maxHealthPoints;

        capsuleCollider.enabled = true;

        rigidBody2D.gravityScale = 1.0f;

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
    }

    private void ExitIdleState()
    {
        isIdle = false;
    }
    #endregion

    #region Walking state
    private void EnterWalkingState()
    {
        isGrounded = false;
        wallDetected = false;
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

        if (Raycast(transform.position, transform.right, playerCheckDistance, whatIsPlayer))
        {
            SwitchState(AIState.Chaising);
        }
    }

    private void ExitWalkingState()
    {
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
    /// Executable method in animation Attack(A, B) as an event
    /// </summary>
#pragma warning disable IDE0051 
    private void DealDamage()
#pragma warning restore IDE0051 
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
    /// Executable method in animation Attack(A, B) as an event
    /// </summary>
#pragma warning disable IDE0051 
    private void NextAttack()
#pragma warning restore IDE0051 
    {
        randomAttackValue = Round(Random.Range(1.0f, 2.4f));
    }

    private void Rotation()
    {
        facingDirection *= -1;
        transform.Rotate(0.0f, 180.0f, 0.0f);
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
            case AIState.Walking:
                EnterWalkingState();
                break;
        }
        currentState = state;
    }

    /// <summary>
    /// Executable method in animation Hit as an event
    /// </summary>
#pragma warning disable IDE0051 
    private void TerminationOfHit() => SwitchState(previousState);
#pragma warning restore IDE0051 
    #endregion

    #region Inner classes
    [System.Serializable]
    class AIAnimatorParameters
    {
        [SerializeField] internal string attackValue = "AttackValue", isAlive = "IsAlive", isAttaking = "IsAttaking",
            isHit = "IsHit", movementSpeed = "MovementSpeed";
    }
    #endregion
}
