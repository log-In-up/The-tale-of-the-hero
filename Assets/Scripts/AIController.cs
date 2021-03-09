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
    private float wallCheckDistance = 0.25f, groundCheckDistance = 0.5f, attackDistance = 0.6f, playerCheckDistance = 4.0f, movementSpeed = 1.0f,
        idleTime = 2.5f, maxHealthPoints = 100, damage = 5;
    [SerializeField] private Transform groundCheck = null;
    [SerializeField] private LayerMask whatIsGround, whatIsPlayer;

    private enum AIState { Chaising, Dead, Hit, Idle, Walking }
    private AIState currentState = AIState.Idle;

    private bool wallDetected, isGrounded, isIdle, playerDetected, canAttack;
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
        animator.SetBool(animatorParameters.isAllive, true);

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
        animator.SetBool(animatorParameters.isAllive, false);
    }

    private void UpdateDeadState()
    {

    }

    private void ExitDeadState()
    {
        currentHealthPoints = maxHealthPoints;

        capsuleCollider.enabled = true;

        rigidBody2D.gravityScale = 1.0f;

        animator.SetBool(animatorParameters.isAllive, true);
    }
    #endregion

    #region Hit state
    private void EnterHitState()
    {

    }

    private void UpdateHitState()
    {

    }

    private void ExitHitState()
    {

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
            StartCoroutine(WhileWaiting(idleTime, AIState.Walking));
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

    #region Other methods
    internal void ApplyDamage(float damage)
    {
        currentHealthPoints -= damage;
    }

    private void Rotation()
    {
        facingDirection *= -1;
        transform.Rotate(0.0f, 180.0f, 0.0f);
    }

    private void Movement()
    {
        movement.Set(movementSpeed * facingDirection, rigidBody2D.velocity.y);
        rigidBody2D.velocity = movement;

        animator.SetFloat(animatorParameters.movementSpeed, Abs(rigidBody2D.velocity.x));
    }

    //Executable method in animation Attack(A, B) as an event 
    private void NextAttack()
    {
        randomAttackValue = Round(Random.Range(1.0f, 2.4f));
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

    private IEnumerator WhileWaiting(float waitingTime, AIState state)
    {
        yield return new WaitForSeconds(waitingTime);

        SwitchState(state);
    }

    //Executable method in animation Attack(A, B) as an event 
    private void DealDamage()
    {
        bool isHit = Raycast(transform.position, transform.right, attackDistance, whatIsPlayer);
        RaycastHit2D hit = Raycast(transform.position, transform.right, attackDistance, whatIsPlayer);

        if (isHit && hit.collider.gameObject.GetComponent<PlayerController>() != null)
        {
            hit.collider.gameObject.GetComponent<PlayerController>().ApplyDamage(damage);
        }
    }
    #endregion

    #region Inner classes
    [System.Serializable]
    class AIAnimatorParameters
    {
        [SerializeField] internal string attackValue = "Attack value", isAllive = "Is Allive", isAttaking = "Is attaking", movementSpeed = "Movement speed";
    }
    #endregion
}
