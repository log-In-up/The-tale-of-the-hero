using UnityEngine;
using static UnityEngine.Input;
using static UnityEngine.Mathf;
using static UnityEngine.Gizmos;
using static UnityEngine.Physics2D;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
sealed class PlayerController : MonoBehaviour
{
    #region Parameters
    [SerializeField]
    private float attackDistance = 0.5f, checkGroundDistance = 0.4f, movementSpeed = 2.5f, jumpForce = 2.5f,
        ledgeCheckDistance = 0.56f, wallCheckDistance = 0.26f, damage = 50.0f, maxHealthPoints = 200.0f;
    [SerializeField] private Vector2 climbPoint = new Vector2(0.3f, 0.73f);
    [SerializeField] private LayerMask whatIsGround, whatIsEnemy;
    [SerializeField] private Transform ledge;
    [SerializeField] private PlayerAnimatorParameters animatorParameters;
    [SerializeField] private PlayerInput input;

    private bool isClimbed = false, canAttack = true, isGrounded, isTouchingLedge, isTouchingWall;
    private float currentHealthPoints;

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
        canAttack = true;
        rigidBody2D.freezeRotation = true;
        spriteRenderer.flipX = false;

        currentHealthPoints = maxHealthPoints;
    }

    private void Update()
    {       
        //CheckEnvironment method should be called first
        CheckEnvironment();

        if (input.GetButtonDownAttack && canAttack)
        {
            canAttack = false;
            Attack();
        }

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
    internal void ApplyDamage(float damage)
    {
        currentHealthPoints -= damage;
        Debug.Log(currentHealthPoints);
    }

    private void Attack()
    {
        float attackPattern = Round(Random.Range(1.0f, 4.4f));
        animator.SetFloat(animatorParameters.attackPattern, attackPattern);
    }

    private void Climb()
    {
        if (!isTouchingLedge && isTouchingWall && !isClimbed && input.GetButtonDownJump)
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
        isGrounded = Raycast(transform.position, -transform.up, checkGroundDistance, whatIsGround);

        isTouchingLedge = Raycast(ledge.position, spriteRenderer.flipX ? ledge.position + (-ledge.right) : ledge.position + ledge.right,
            ledgeCheckDistance, whatIsGround);
        isTouchingWall = Raycast(transform.position, spriteRenderer.flipX ? -transform.right : transform.right,
            wallCheckDistance, whatIsGround);
    }

    //Executable method in animation Attack(A-D) as an event
    private void DealDamage()
    {
        bool isHit = Raycast(transform.position, spriteRenderer.flipX ? -transform.right : transform.right, attackDistance, whatIsEnemy);
        RaycastHit2D hit = Raycast(transform.position, spriteRenderer.flipX ? -transform.right : transform.right, attackDistance, whatIsEnemy);

        if (isHit && hit.collider.gameObject.GetComponent<AIController>() != null)
        {
            hit.collider.gameObject.GetComponent<AIController>().ApplyDamage(damage);
        }
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

    //Executable method in animation Attack(A-D) as an event
    private void NextPattern()
    {
        canAttack = true;
        animator.SetFloat(animatorParameters.attackPattern, 0.0f);
    }

    private void MovePlayer()
    {
        Vector2 playerInput = new Vector2(input.GetAxisHorizontal * movementSpeed, 0.0f);

        if (isGrounded)
        {
            rigidBody2D.AddForce(playerInput, ForceMode2D.Impulse);
            if (input.GetButtonDownJump && !isClimbed)
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

        animator.SetFloat(animatorParameters.movementSpeed, Abs(input.GetAxisHorizontal));
    }
    #endregion

    #region Inner classes
    [System.Serializable]
    sealed class PlayerInput
    {
        [SerializeField] internal string horizontal = "Horizontal", jump = "Jump", attack = "Fire1";

        internal bool GetButtonDownJump { get { return GetButtonDown(jump); } }
        internal bool GetButtonDownAttack { get { return GetButtonDown(attack); } }
        internal float GetAxisHorizontal { get { return GetAxis(horizontal); } }
    }

    [System.Serializable]
    sealed class PlayerAnimatorParameters
    {
        [SerializeField] internal string attackPattern = "Attack pattern", canClimb = "Can climb", movementSpeed = "Movement speed";
    }
    #endregion
}
