using System.Collections;
using UnityEngine;
using static UnityEngine.Mathf;
using static UnityEngine.Physics2D;

[RequireComponent(typeof(Animator), typeof(CapsuleCollider2D), typeof(Rigidbody2D))]
sealed class PlayerController : MonoBehaviour
{
    #region Parameters
    [SerializeField]
    private float attackDistance = 0.5f, checkAboveDistance = 0.5f, checkGroundDistance = 0.4f, damage = 50.0f,
        hideWeaponTime = 5.0f, jumpTime = 1.1f, ledgeCheckDistance = 0.56f, maxHealthPoints = 200.0f,
        movementDeceleration = 0.5f, movementSpeed = 2.5f, wallCheckDistance = 0.26f;
    [SerializeField] private int attackPatterns = 4, hitPatterns = 2;
    [SerializeField] private LayerMask whatIsEnemy, whatIsGround;
    [SerializeField] private PlayerAnimatorParameters animatorParameters;
    [SerializeField] private Transform floorCheck = null, ledgeCheck = null, jumpPointCheck = null;
    [SerializeField] private RectTransform attackRT = null, jumpRT = null;
    [SerializeField] private Vector2 climbPoint = new Vector2(0.3f, 0.73f);

    private bool canAttack, canJump, cannotJump, isClimbed, isGrounded, isJumping, isTouchingFloor, isTouchingLedge,
        lookToRight, overhead, preJump;
    private float currentAttackPatterns, currentHealthPoints, currentHitPatterns, percentage, timeStartLerp,
        currentGravityScale;

    private const float accelerationThreshold = 0.01f, expandRange = 0.4f, lookToLeftAngle = 180.0f,
        lookToRightAngle = 0.0f, maximumPercentage = 1.0f, minOfRange = 1.0f, minimumPercentage = 0.0f, zero = 0.0f,
        zeroGravityScale = 0.0f;

    private Animator animator = null;
    private ButtonHandler attackButton = null, jumpButton = null;
    private CapsuleCollider2D capsuleCollider2D = null;
    private Coroutine currentCoroutine = null;
    private Rigidbody2D rigidBody2D = null;
    private Vector2 climbEnd, jumpStart, jumpEnd;

    internal float MaxHealthPoints { get { return maxHealthPoints; } }
    public static PlayerController Instance { get; private set; }
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        Instance = Singleton.GetSingleton(gameObject, Instance);

        animator = GetComponent<Animator>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        rigidBody2D = GetComponent<Rigidbody2D>();

        attackButton = attackRT.GetComponent<ButtonHandler>();
        jumpButton = jumpRT.GetComponent<ButtonHandler>();
    }

    private void Start()
    {
        canAttack = lookToRight = true;
        overhead = isClimbed = preJump = isJumping = false;

        rigidBody2D.freezeRotation = true;

        animator.SetBool(animatorParameters.isAlive, true);
        animator.SetBool(animatorParameters.holdWeapon, false);

        currentGravityScale = rigidBody2D.gravityScale;
        percentage = minimumPercentage;
        currentHealthPoints = maxHealthPoints;
        currentAttackPatterns = attackPatterns + expandRange;
        currentHitPatterns = hitPatterns + expandRange;

        UIHealthBar.Instance.UpdateHealthBar(currentHealthPoints);
    }


    private void FixedUpdate()
    {
        CheckEnvironment();
    }

    private void Update()
    {
        if (currentHealthPoints <= zero)
        {
            animator.SetBool(animatorParameters.isAlive, false);
            rigidBody2D.velocity = Vector2.zero;

            return;
        }

        if (isJumping && percentage <= maximumPercentage)
        {
            Jump();
        }

        if (attackButton.isPressed && canAttack)
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
#pragma warning disable IDE0051
    internal void ApplyDamage(float damage)
    {
        currentHealthPoints -= damage;
        UIHealthBar.Instance.UpdateHealthBar(currentHealthPoints);

        animator.SetFloat(animatorParameters.hitPattern, Round(Random.Range(minOfRange, currentHitPatterns)));
    }

    private IEnumerator HideWeapon(float time)
    {
        yield return new WaitForSeconds(time);

        animator.SetBool(animatorParameters.holdWeapon, false);
    }

    private void Attack()
    {
        float attackPattern = Round(Random.Range(minOfRange, currentAttackPatterns));
        animator.SetFloat(animatorParameters.attackPattern, attackPattern);
    }

    private void CheckEnvironment()
    {
        isGrounded = Raycast(transform.position, -transform.up, checkGroundDistance, whatIsGround);
        isTouchingLedge = Raycast(ledgeCheck.position, ledgeCheck.right, ledgeCheckDistance, whatIsGround);
        isTouchingFloor = Raycast(floorCheck.position, floorCheck.right, wallCheckDistance, whatIsGround);
        canJump = Raycast(jumpPointCheck.position, -jumpPointCheck.up, checkGroundDistance, whatIsGround);
        overhead = Raycast(transform.position, transform.up, checkAboveDistance, whatIsGround);
    }

    /// <summary>
    /// Executable method in Hit animations as an event
    /// </summary>
    private void ChangeHitPattern()
    {
        animator.SetFloat(animatorParameters.hitPattern, zero);
    }

    private void Climb()
    {
        if (!isTouchingLedge && !overhead && isTouchingFloor && !isClimbed && jumpButton.isPressed)
        {
            DisableComponents();

            if (lookToRight)
            {
                climbEnd = transform.position + (Vector3)climbPoint;
            }
            else
            {
                climbEnd.x = transform.position.x - climbPoint.x;
                climbEnd.y = transform.position.y + climbPoint.y;
            }

            isClimbed = true;
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
        bool isHit = Raycast(transform.position, transform.right, attackDistance, whatIsEnemy);
        RaycastHit2D hit = Raycast(transform.position, transform.right, attackDistance, whatIsEnemy);

        if (isHit && hit.collider.gameObject.GetComponent<AIController>() != null)
        {
            hit.collider.gameObject.GetComponent<AIController>().ApplyDamage(damage);
        }
    }

    private void DisableComponents()
    {
        capsuleCollider2D.enabled = false;
        rigidBody2D.velocity = Vector2.zero;
        rigidBody2D.gravityScale = zeroGravityScale;
    }

    private void EnableComponents()
    {
        capsuleCollider2D.enabled = true;
        rigidBody2D.gravityScale = currentGravityScale;
    }

    /// <summary>
    /// Executable method in animation Climb as an event
    /// </summary>
    private void FinishClimb()
    {
        isClimbed = false;

        transform.position = climbEnd;

        EnableComponents();

        animator.SetBool(animatorParameters.canClimb, isClimbed);
    }

    private void Jump()
    {
        if (!preJump)
        {
            animator.SetBool(animatorParameters.isJumping, isJumping);

            DisableComponents();

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

            preJump = true;
        }

        percentage = (Time.time - timeStartLerp) / jumpTime;

        transform.position = Vector3.Lerp(jumpStart, jumpEnd, percentage);
    }

    /// <summary>
    /// Executable method in animation Jump as an event
    /// </summary>
    private void JumpEnd()
    {
        isJumping = preJump = false;
        percentage = minimumPercentage;

        animator.SetBool(animatorParameters.isJumping, isJumping);

        EnableComponents();
    }

    private void MovePlayer()
    {
        Vector2 playerInput = new Vector2(JoystickController.Instance.Horizontal() * movementSpeed, zero);
        cannotJump = Linecast(transform.position, jumpPointCheck.position, whatIsGround);

        if (!isClimbed && isGrounded)
        {
            rigidBody2D.AddForce(playerInput, ForceMode2D.Impulse);
            if (jumpButton.isPressed && canJump && !cannotJump)
            {
                timeStartLerp = Time.time;
                isJumping = true;
            }
        }

        if (rigidBody2D.velocity.x < -accelerationThreshold)
        {
            transform.rotation = Quaternion.Euler(zero, lookToLeftAngle, zero);
            lookToRight = false;
        }
        else if (rigidBody2D.velocity.x > accelerationThreshold)
        {
            transform.rotation = Quaternion.Euler(zero, lookToRightAngle, zero);
            lookToRight = true;
        }

        float currentVelocity = !animator.GetBool(animatorParameters.holdWeapon) ? rigidBody2D.velocity.x : rigidBody2D.velocity.x * movementDeceleration;
        animator.SetFloat(animatorParameters.movementSpeed, Abs(currentVelocity));
    }

    /// <summary>
    /// Executable method in animation Attack(A-D) as an event
    /// </summary>
    private void NextPattern()
    {
        canAttack = true;
        animator.SetFloat(animatorParameters.attackPattern, zero);
    }
#pragma warning restore IDE0051
    #endregion

    #region Inner classes
    [System.Serializable]
    sealed class PlayerAnimatorParameters
    {
        [SerializeField]
        internal string attackPattern = "AttackPattern", canClimb = "CanClimb", hitPattern = "HitPattern",
            holdWeapon = "HoldWeapon", isAlive = "IsAlive", isJumping = "IsJumping", movementSpeed = "MovementSpeed";
    }
    #endregion
}
