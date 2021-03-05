using UnityEngine;
using static UnityEngine.Mathf;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
sealed class AIController : MonoBehaviour
{
    #region Parameters
    [SerializeField] private AIAnimatorParameters animatorParameters;
    [SerializeField] private float wallCheckDistance = 0.25f, groundCheckDistance = 0.5f, movementSpeed = 1.0f;
    [SerializeField] private Transform groundCheck = null;
    [SerializeField] private LayerMask whatIsGround;

    private enum AIState { Chaising, Dead, Hit, Idle, Walking }
    private AIState currentState;
    private Animator animator = null;
    private bool wallDetected, isGrounded;
    private CapsuleCollider2D capsuleCollider = null;
    private Rigidbody2D rigidBody2D = null;
    private SpriteRenderer spriteRenderer = null;
    private sbyte facingDirection;
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
        currentState = AIState.Walking;
        rigidBody2D.freezeRotation = true;
        spriteRenderer.flipX = false;

        facingDirection = 1;
    }

    private void Update()
    {
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

    #region Attaking state
    private void EnterChaisingState()
    {

    }

    private void UpdateChaisingState()
    {

    }

    private void ExitChaisingState()
    {

    }
    #endregion

    #region Dead state
    private void EnterDeadState()
    {

    }

    private void UpdateDeadState()
    {

    }

    private void ExitDeadState()
    {

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

    }

    private void UpdateIdleState()
    {

    }

    private void ExitIdleState()
    {

    }
    #endregion

    #region Walking state
    private void EnterWalkingState()
    {

    }

    private void UpdateWalkingState()
    {
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        wallDetected = Physics2D.Raycast(transform.position, transform.right, wallCheckDistance, whatIsGround);

        if (!isGrounded || wallDetected)
        {
            facingDirection *= -1; 
            transform.Rotate(0.0f, 180.0f, 0.0f);
        }
        else
        {
            movement.Set(movementSpeed * facingDirection, rigidBody2D.velocity.y);
            rigidBody2D.velocity = movement;

            animator.SetFloat(animatorParameters.movementSpeed, Abs(rigidBody2D.velocity.x));
        }
    }

    private void ExitWalkingState()
    {

    }
    #endregion

    #region Other methods
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
    #endregion

    #region Inner classes
    [System.Serializable]
    class AIAnimatorParameters
    {
        [SerializeField] internal string movementSpeed = "Movement speed";
    }
    #endregion
}
