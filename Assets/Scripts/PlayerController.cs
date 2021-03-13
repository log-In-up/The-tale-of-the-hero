using System.Collections;
using UnityEngine;
using static UnityEngine.Input;
using static UnityEngine.Mathf;
using static UnityEngine.Physics2D;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
sealed class PlayerController : MonoBehaviour
{
    #region Parameters
    [SerializeField]
    private float attackDistance = 0.5f, checkGroundDistance = 0.4f, damage = 50.0f, hideWeaponTime = 5.0f,
        jumpForce = 2.5f, ledgeCheckDistance = 0.56f, maxHealthPoints = 200.0f, movementSpeed = 2.5f,
        wallCheckDistance = 0.26f;
    [SerializeField] private int attackPatterns = 4, hitPatterns = 2;
    [SerializeField] private Vector2 climbPoint = new Vector2(0.3f, 0.73f);
    [SerializeField] private LayerMask whatIsEnemy, whatIsGround;
    [SerializeField] private Transform floorCheck, ledgeCheck;
    [SerializeField] private PlayerAnimatorParameters animatorParameters;
    [SerializeField] private PlayerInput inputAxes;

    private bool canAttack, isClimbed, isGrounded, isTouchingFloor, isTouchingLedge;
    private float currentAttackPatterns, currentHealthPoints, currentHitPatterns;

    private Animator animator = null;
    private CapsuleCollider2D capsuleCollider2D = null;
    private Coroutine currentCoroutine = null;
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
        isClimbed = false;
        rigidBody2D.freezeRotation = true;
        spriteRenderer.flipX = false;

        animator.SetBool(animatorParameters.isAlive, true);
        animator.SetBool(animatorParameters.holdWeapon, false);

        currentHealthPoints = maxHealthPoints;
        currentAttackPatterns = attackPatterns + 0.4f;
        currentHitPatterns = hitPatterns + 0.4f;
    }

    private void FixedUpdate()
    {
        CheckEnvironment();
    }

    private void Update()
    {
        if (currentHealthPoints <= 0.0f)
        {
            animator.SetBool(animatorParameters.isAlive, false);
            rigidBody2D.velocity = Vector2.zero;

            return;
        }

        if (inputAxes.GetButtonDownAttack && canAttack)
        {
            canAttack = false;
            Attack();
            animator.SetBool(animatorParameters.holdWeapon, true);

            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(HideWeapon(hideWeaponTime));
        }

        //Climb method must be called before the MovePlayer method
        Climb();
        MovePlayer();
    }
    #endregion

    #region Custom methods
    internal void ApplyDamage(float damage)
    {
        currentHealthPoints -= damage;
        animator.SetFloat(animatorParameters.hitPattern, Round(Random.Range(1.0f, currentHitPatterns)));
        Debug.Log($"Player's health points {currentHealthPoints}");
    }

    private IEnumerator HideWeapon(float time)
    {
        yield return new WaitForSeconds(time);

        animator.SetBool(animatorParameters.holdWeapon, false);
    }

    private void Attack()
    {
        float attackPattern = Round(Random.Range(1.0f, currentAttackPatterns));
        animator.SetFloat(animatorParameters.attackPattern, attackPattern);
    }

    private void CheckEnvironment()
    {
        isGrounded = Raycast(transform.position, -transform.up, checkGroundDistance, whatIsGround);

        isTouchingLedge = Raycast(ledgeCheck.position, spriteRenderer.flipX ? ledgeCheck.position + (-ledgeCheck.right) : ledgeCheck.position + ledgeCheck.right,
            ledgeCheckDistance, whatIsGround);
        isTouchingFloor = Raycast(floorCheck.position, spriteRenderer.flipX ? -floorCheck.right : floorCheck.right,
            wallCheckDistance, whatIsGround);
    }

    /// <summary>
    /// Executable method in Hit animations as an event
    /// </summary>
    private void ChangeHitPattern()
    {
        animator.SetFloat(animatorParameters.hitPattern, 0.0f);
    }

    private void Climb()
    {
        if (!isTouchingLedge && isTouchingFloor && !isClimbed && inputAxes.GetButtonDownJump)
        {
            capsuleCollider2D.enabled = false;
            rigidBody2D.velocity = Vector2.zero;
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

    /// <summary>
    /// Executable method in animation Attack(A-D) as an event
    /// </summary>
    private void DealDamage()
    {
        bool isHit = Raycast(transform.position, spriteRenderer.flipX ? -transform.right : transform.right, attackDistance, whatIsEnemy);
        RaycastHit2D hit = Raycast(transform.position, spriteRenderer.flipX ? -transform.right : transform.right, attackDistance, whatIsEnemy);

        if (isHit && hit.collider.gameObject.GetComponent<AIController>() != null)
        {
            hit.collider.gameObject.GetComponent<AIController>().ApplyDamage(damage);
        }
    }

    /// <summary>
    /// Executable method in animation Climb as an event
    /// </summary>
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
        Vector2 playerInput = new Vector2(inputAxes.GetAxisHorizontal * movementSpeed, 0.0f);

        if (!isClimbed && isGrounded)
        {
            rigidBody2D.AddForce(playerInput, ForceMode2D.Impulse);
            if (inputAxes.GetButtonDownJump)
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

        float currentVelocity = !animator.GetBool(animatorParameters.holdWeapon) ? rigidBody2D.velocity.x : rigidBody2D.velocity.x * 0.5f;
        animator.SetFloat(animatorParameters.movementSpeed, Abs(currentVelocity));
    }

    /// <summary>
    /// Executable method in animation Attack(A-D) as an event
    /// </summary>
    private void NextPattern()
    {
        canAttack = true;
        animator.SetFloat(animatorParameters.attackPattern, 0.0f);
    }

    #endregion

    #region Inner classes

    [System.Serializable]
    sealed class PlayerAnimatorParameters
    {
        [SerializeField]
        internal string attackPattern = "AttackPattern", canClimb = "CanClimb", hitPattern = "HitPattern",
            holdWeapon = "HoldWeapon", isAlive = "IsAlive", movementSpeed = "MovementSpeed";
    }

    [System.Serializable]
    sealed class PlayerInput
    {
        [SerializeField] internal string horizontalAxis = "Horizontal", jump = "Jump", attack = "Fire1";

        internal bool GetButtonDownJump { get { return GetButtonDown(jump); } }
        internal bool GetButtonDownAttack { get { return GetButtonDown(attack); } }
        internal float GetAxisHorizontal { get { return GetAxis(horizontalAxis); } }
    }
    #endregion
}
