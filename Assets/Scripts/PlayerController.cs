using UnityEngine;
using static UnityEngine.Input;
using static UnityEngine.Mathf;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
class PlayerController : MonoBehaviour
{
    #region Parameters
    [SerializeField] private float movementSpeed = 2.5f;
    [SerializeField] private string horizontal = "Horizontal";
    [SerializeField] private PlayerAnimatorController animatorController;

    private Animator animator = null;
    private CapsuleCollider2D capsuleCollider2D = null;
    private Rigidbody2D rigidBody2D = null;
    private SpriteRenderer spriteRenderer = null;
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        animator = GetComponent<Animator>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rigidBody2D.freezeRotation = true;
    }


    private void Update()
    {
        MovePlayer();
        AnimatePlayer();
    }
    #endregion

    #region Custom methods
    private void AnimatePlayer()
    {
        animator.SetFloat(animatorController.movementSpeed, Abs(GetAxis(horizontal)));
    }

    private void MovePlayer()
    {
        float horizontalAxis = GetAxis(horizontal);

        if (horizontalAxis < 0.0f)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }

        Vector2 playerInput = new Vector2(horizontalAxis * movementSpeed, 0.0f);

        rigidBody2D.AddForce(playerInput, ForceMode2D.Impulse);
    }
    #endregion

    #region Inner classes
    [System.Serializable]
    class PlayerAnimatorController
    {
        [SerializeField] internal string movementSpeed = "Movement speed";
    }
    #endregion
}
