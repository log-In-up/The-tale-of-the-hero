using UnityEngine;
using static UnityEngine.Input;
using static UnityEngine.Mathf;
using static UnityEngine.Gizmos;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
sealed class PlayerController : MonoBehaviour
{
    #region Parameters
    [SerializeField]
    private float movementSpeed = 2.5f, jumpForce = 2.5f, checkGroundDistance = 0.4f,
        wallCheckDistance = 0.26f, ledgeCheckDistance = 0.56f;
    [SerializeField] private string horizontal = "Horizontal", jump = "Jump";
    [SerializeField] private Vector2 climbPoint = new Vector2(0.3f, 0.73f);
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Transform ledge;
    [SerializeField] private PlayerAnimatorParameters animatorParameters;

    private bool isClimbed = false, isGrounded, isTouchingLedge, isTouchingWall;

    private Animator animator = null;
    private CapsuleCollider2D capsuleCollider2D = null;
    private Rigidbody2D rigidBody2D = null;
    private SpriteRenderer spriteRenderer = null;
    private Vector2 climbEnd;

    public static PlayerController Instance { get; private set; }
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        #region Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        #endregion

        animator = GetComponent<Animator>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();        
    }

    private void Start()
    {
        rigidBody2D.freezeRotation = true;
        spriteRenderer.flipX = false;
    }

    private void Update()
    {
        CheckEnvironment();

        //Climb method must be called before the MovePlayer method
        Climb(); 
        MovePlayer();
    }

    private void OnDrawGizmos()
    {
        color = Color.red;
        if (spriteRenderer != null)
        {
            DrawLine(transform.position, spriteRenderer.flipX ? transform.position + (-transform.right) : transform.position + transform.right);
            DrawLine(ledge.position, spriteRenderer.flipX ? ledge.position + (-ledge.right) : ledge.position + ledge.right);
        }
    }
    #endregion

    #region Custom methods
    private void Climb()
    {
        if (!isTouchingLedge && isTouchingWall && !isClimbed && GetButtonDown(jump))
        {
            capsuleCollider2D.enabled = false;
            rigidBody2D.gravityScale = 0;

            isClimbed = true;
            if (spriteRenderer.flipX)
            {
                climbEnd.x = transform.position.x - climbPoint.x;
                climbEnd.y = transform.position.y + climbPoint.y;
            }
            else
            {
                climbEnd = transform.position + (Vector3)climbPoint;
            }
        }

        if (isClimbed)
        {
            animator.SetBool(animatorParameters.canClimb, isClimbed);
        }
    }

    private void CheckEnvironment()
    {
        isGrounded = Physics2D.Raycast(transform.position, -transform.up, checkGroundDistance, whatIsGround);

        isTouchingLedge = Physics2D.Raycast(ledge.position, spriteRenderer.flipX ? ledge.position + (-ledge.right) : ledge.position + ledge.right,
            ledgeCheckDistance, whatIsGround);
        isTouchingWall = Physics2D.Raycast(transform.position, spriteRenderer.flipX ? -transform.right : transform.right,
            wallCheckDistance, whatIsGround);
    }

    //Executable method in animation Climb as an event 
    private void FinishClimb()
    {
        isClimbed = false;

        transform.position = climbEnd;

        capsuleCollider2D.enabled = true;
        rigidBody2D.gravityScale = 1;

        animator.SetBool(animatorParameters.canClimb, isClimbed);
    }

    private void MovePlayer()
    {
        Vector2 playerInput = new Vector2(GetAxis(horizontal) * movementSpeed, 0.0f);

        if (isGrounded)
        {
            rigidBody2D.AddForce(playerInput, ForceMode2D.Impulse);
            if (GetButtonDown(jump) && !isClimbed)
            {
                rigidBody2D.velocity = playerInput + (Vector2.up * jumpForce);
            }
        }

        if (rigidBody2D.velocity.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (rigidBody2D.velocity.x > 0.01f)
        {
            spriteRenderer.flipX = false;
        }

        animator.SetFloat(animatorParameters.movementSpeed, Abs(GetAxis(horizontal)));
    }
    #endregion

    #region Inner classes
    [System.Serializable]
    sealed class PlayerAnimatorParameters
    {
        [SerializeField] internal string movementSpeed = "Movement speed";
        [SerializeField] internal string canClimb = "Can climb";
    }
    #endregion
}
