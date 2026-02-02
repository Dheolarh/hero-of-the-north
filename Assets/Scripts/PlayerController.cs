using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Transform playerTransform;
    Rigidbody2D playerRigidbody;
    [SerializeField] float speed;
    [SerializeField] float jumpForce;
    SpriteRenderer playerSprite;
    Animator playerAnimation;
    bool isGrounded = true;
    bool isJumping = false;
    bool isMultiJump;
    bool resetRotation = true;

    // For UI button controls
    private bool uiMoveLeft = false;
    private bool uiMoveRight = false;

    // Camera reference
    private CameraFollow cameraFollow;


    void Start()
    {
        playerTransform = GetComponent<Transform>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerSprite = GetComponent<SpriteRenderer>();
        playerAnimation = GetComponent<Animator>();

        // Find the camera with CameraFollow script
        cameraFollow = Camera.main.GetComponent<CameraFollow>();

        if (LevelManager.Instance != null)
        {
            isMultiJump = LevelManager.Instance.allowMultiJumps;
        }
    }

    void Update()
    {
        if (GameManager.Instance.isGameOver) return;
        HandleMovement();
        OnJump();
        ResetPlayerRotation();
    }

    public void MoveLeft()
    {
        uiMoveLeft = true;
    }

    public void StopMoveLeft()
    {
        uiMoveLeft = false;
    }

    public void MoveRight()
    {
        uiMoveRight = true;
    }

    public void StopMoveRight()
    {
        uiMoveRight = false;
    }

    public void Jump()
    {
        PerformJump();
    }

    void HandleMovement()
    {
        float moveDirection = 0f;

        if (Input.GetKey(KeyCode.LeftArrow) || uiMoveLeft)
        {
            moveDirection -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow) || uiMoveRight)
        {
            moveDirection += 1f;
        }

        if (isGrounded)
        {
            if (moveDirection != 0f)
            {
                playerTransform.Translate(Vector3.right * moveDirection * speed * Time.deltaTime);
                playerSprite.flipX = (moveDirection < 0);
                PlayAnimation("isWalking");
            }
            else
            {
                PlayAnimation("isIdle");
            }
        }
        else
        {
            if (moveDirection != 0f)
            {
                playerTransform.Translate(Vector3.right * moveDirection * speed * Time.deltaTime);
                playerSprite.flipX = (moveDirection < 0);
            }
        }
    }

    void OnJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformJump();
        }
    }

    void PerformJump()
    {
        if (isGrounded)
        {
            playerRigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            PlayAnimation("isJumping");
            isGrounded = false;
        }
        else if (isJumping && isMultiJump)
        {
            playerRigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            PlayAnimation("isJumping");
        }
    }

    void PlayAnimation(string animationName)
    {
        playerAnimation.SetBool("isWalking", false);
        playerAnimation.SetBool("isJumping", false);
        playerAnimation.SetBool("isIdle", false);
        playerAnimation.SetBool("isDead", false);
        playerAnimation.SetBool(animationName, true);
    }

    void ResetPlayerRotation()
    {
        if (!resetRotation) return;
        
        if (playerTransform.rotation != Quaternion.identity)
        {
            playerTransform.rotation = Quaternion.identity;
        }
    }

    IEnumerator GameOver()
    {
        yield return new WaitForSeconds(2f);
        GameManager.Instance.GameOver();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            isGrounded = true;
            PlayAnimation("isIdle");
        }
        if (other.CompareTag("Death"))
        {
            Debug.Log("Player is Dead");
            GameManager.Instance.isGameOver = true;
            PlayAnimation("isDead");
            StartCoroutine(StopCameraAfterDelay(1f));
            StartCoroutine(GameOver());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            isGrounded = false;
            isJumping = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("PlatformGround"))
        {
            isGrounded = true;
            PlayAnimation("isIdle");
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("PlatformGround"))
        {
            isGrounded = false;
            isJumping = true;
        }
    }

    IEnumerator StopCameraAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (cameraFollow != null)
        {
            cameraFollow.StopFollowing();
        }
    }
}
