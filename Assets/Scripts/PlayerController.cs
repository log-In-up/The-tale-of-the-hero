using UnityEngine;
using static UnityEngine.Input;
using static UnityEngine.Mathf;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
sealed class PlayerController : MonoBehaviour
{
    #region Parameters
    [SerializeField] private float movementSpeed = 2.5f, jumpForce = 2.5f, checkGroundDistance = 0.4f;
    [SerializeField] private string horizontal = "Horizontal", jump = "Jump";
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private PlayerAnimatorController animatorController;

    private float horizontalAxis = 0.0f;
    private Animator animator = null;
    private CapsuleCollider2D capsuleCollider2D = null;
    private Rigidbody2D rigidBody2D = null;
    private SpriteRenderer spriteRenderer = null;

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

        rigidBody2D.freezeRotation = true;
    }

    private void Update()
    {
        horizontalAxis = GetAxis(horizontal);

        MovePlayer();
        AnimatePlayer();
    }
    #endregion

    #region Custom methods
    private bool CheckGround()
    {
        if (Physics2D.Raycast(transform.position, Vector2.down, checkGroundDistance, whatIsGround))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void AnimatePlayer()
    {
        animator.SetFloat(animatorController.movementSpeed, Abs(horizontalAxis));
    }

    private void MovePlayer()
    {
        Vector2 playerInput = new Vector2(horizontalAxis * movementSpeed, 0.0f);

        if (CheckGround())
        {
            rigidBody2D.AddForce(playerInput, ForceMode2D.Impulse);
            if (GetButtonDown(jump))
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
    }
    #endregion

    #region Inner classes
    [System.Serializable]
    sealed class PlayerAnimatorController
    {
        [SerializeField] internal string movementSpeed = "Movement speed";
    }
    #endregion
}
