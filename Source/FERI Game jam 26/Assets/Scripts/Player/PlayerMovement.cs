using System.Collections;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

//numbers match the animator "state" int: Idle 0, Running 1, Attack 2, AttackUp 3, AttackDown 4, Death 5
public enum PlayerState
{
    Idle,
    Running,
    Attack,
    AttackUp,
    AttackDown,
    Death
}

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer sprite;
    private Animator anim;
    private Vector2 moveInput;
    private float lookInputVertical;
    private float lookInputHorizontal;

    [Header("vars")]
    [SerializeField] PlayerCombat playerCombat;
    [SerializeField] private Transform[] flipChildren;
    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private float cameraOffsetHorizontal = 1f;
    [SerializeField] private float cameraOffsetVertical = 1f;
    [SerializeField] private float cameraOffsetVerticalDefault = 1f;
    [SerializeField] private float verticalHoldTime = 1f;
    [SerializeField] private float verticalHoldCounter = 0f;
    [SerializeField] private float fallMultiplier = 2f;


    [Header("stats")]
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] public int direction = 0;
    [SerializeField] private float jumpForce = 0f;
    [SerializeField] private float jumpTime = 0f;
    [SerializeField] private float jumpTimeCounter = 0f;
    [SerializeField] public int baseJumpCount = 1;
    [SerializeField] private int jumpCount = 0;
    [SerializeField] private bool isJumping = false;
    [SerializeField] private float groundLockoutTime = .1f;
    [SerializeField] private float groundLockoutCounter = 0f;

    [Header("knockback")]
    [SerializeField] private float knockbackPower = 10f;
    [SerializeField] private float knockbackUpPower = 6f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float knockbackCounter = 0f;
    [SerializeField] private float hitStopTime = 0.07f;

    [Header("attack bounce")]
    [SerializeField] private float pogoPower = 14f;
    [SerializeField] private float sideRecoilPower = 7f;
    [SerializeField] private float sideRecoilDuration = 0.1f;

    [Header("state")]
    public PlayerState state = PlayerState.Idle;
    private float attackStateTimer = 0f;

    public bool IsGrounded()
    {
        //casts lil box under player to check if touching ground
        return Physics2D.BoxCast(col.bounds.center, new Vector2(col.bounds.size.x / 1.5f, col.bounds.size.y), 0f, Vector2.down, .1f, jumpableGround);
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        //player move input logic
        moveInput = context.ReadValue<Vector2>();
        //Debug.Log("player move input: " + moveInput);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        //checks if holding and has jump tokens (can't start a jump while stunned or dead)
        if (context.started && jumpCount > 0 && knockbackCounter <= 0f && state != PlayerState.Death)
        {
            //consume a jump token
            jumpCount--;

            //start jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            isJumping = true;

            //reset counter to max
            jumpTimeCounter = jumpTime;

            //start ground lockout, this disables IsGrounded checks for a bit after jumping
            groundLockoutCounter = groundLockoutTime;
        }

        //player stops holding jump
        if (context.canceled)
        {
            isJumping = false;
        }
    }

    public void OnLookVertical(InputAction.CallbackContext context)
    {
        //player vertical look input
        lookInputVertical = context.ReadValue<float>();
        //Debug.Log("player look input vertical: " + lookInputVertical);
    }

    public void OnLookHorizontal(InputAction.CallbackContext context)
    {
        //player horizontal look input
        lookInputHorizontal = context.ReadValue<float>();
        //Debug.Log("player look input horizontal: " + lookInputHorizontal);
    }

    //dir: 1 pushes right, -1 pushes left
    public void Knockback(int dir)
    {
        if (dir == 0) return;
        if (state == PlayerState.Death) return;

        //cancel any jump in progress so the held-jump logic doesn't fight the knockback
        isJumping = false;
        jumpTimeCounter = 0f;

        //sideways push plus a small hop, velocity is replaced so every hit feels the same
        rb.linearVelocity = new Vector2(Mathf.Sign(dir) * knockbackPower, knockbackUpPower);

        //stun, player can't steer for a bit
        knockbackCounter = knockbackDuration;

        //brief freeze on hit
        StartCoroutine(HitStop(hitStopTime));
    }

    //hollow knight style pogo: hitting something with the down attack launches you straight up, you keep control
    public void Pogo()
    {
        if (state == PlayerState.Death) return;

        //cancel the held jump so it doesn't fight the bounce
        isJumping = false;
        jumpTimeCounter = 0f;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, pogoPower);
    }

    //hollow knight style nail recoil: hitting something sideways nudges you back a little, over very quickly
    //dir: 1 pushes right, -1 pushes left
    public void SideRecoil(int dir)
    {
        if (dir == 0) return;
        if (state == PlayerState.Death) return;

        rb.linearVelocity = new Vector2(Mathf.Sign(dir) * sideRecoilPower, rb.linearVelocity.y);

        //short input lock so the push actually registers (never shortens a longer knockback stun)
        knockbackCounter = Mathf.Max(knockbackCounter, sideRecoilDuration);
    }

    private IEnumerator HitStop(float time)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(time);
        Time.timeScale = 1f;
    }

    //called by PlayerCombat, holds the attack state for the length of the animation
    public void SetAttackState(PlayerState attackState, float duration)
    {
        if (state == PlayerState.Death) return;
        state = attackState;
        attackStateTimer = duration;
    }

    //called by PlayerHealth
    public void SetDead()
    {
        state = PlayerState.Death;
        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateState()
    {
        if (state == PlayerState.Death) return;

        //attack state overrides everything until its timer runs out
        if (attackStateTimer > 0f)
        {
            attackStateTimer -= Time.deltaTime;
            return;
        }

        state = Mathf.Abs(moveInput.x) > 0.1f ? PlayerState.Running : PlayerState.Idle;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    private void Update()
    {
        //tick timers
        if (groundLockoutCounter > 0f)
        {
            groundLockoutCounter -= Time.deltaTime;
        }

        if (knockbackCounter > 0f)
        {
            knockbackCounter -= Time.deltaTime;
        }
        //also locks input when dead
        bool isKnockedBack = knockbackCounter > 0f || state == PlayerState.Death;

        //check if touching floor
        if (IsGrounded() && groundLockoutCounter <= 0f)
        {
            jumpCount = baseJumpCount;
        }

        if (!isKnockedBack)
        {
            //moving logic
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

            //point in direction by flipping sprite (keeps facing the enemy while knocked back)
            if (moveInput.x > 0.1f)
            {
                direction = 1;
                sprite.flipX = false;
            }
            else if (moveInput.x < -0.1f)
            {
                direction = -1;
                sprite.flipX = true;
            }
        }

        //flip children that need to point in direction of movement (weapons)
        foreach (Transform t in flipChildren)
        {
            t.localScale = new Vector3(direction, t.localScale.y, t.localScale.z);
        }

        //jump logic
        if (!isKnockedBack && isJumping && jumpTimeCounter > 0f)
        {
            //while holding jump keeps applying speed until timer reaches max
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpTimeCounter -= Time.deltaTime;
        }

        //horizontal look, instant
        float horizontalOffset = lookInputHorizontal * cameraOffsetHorizontal;

        //vertical look default
        float verticalOffset = cameraOffsetVerticalDefault / 2;
        if (IsGrounded())
        {
            verticalOffset = cameraOffsetVerticalDefault;
        }

        //if theres horizontal input, disable vertical
        if (Mathf.Abs(lookInputHorizontal) > .1f)
        {
            //reset timer
            verticalHoldCounter = 0;
        }
        else
        {
            //no horizontal, vertical is allowed
            if (Mathf.Abs(lookInputVertical) > .1f && IsGrounded())
            {
                ///tick timer
                verticalHoldCounter += Time.deltaTime;

                if (verticalHoldCounter >= verticalHoldTime)
                {
                    //add vertical offset to default
                    verticalOffset = cameraOffsetVertical * Mathf.Sign(lookInputVertical);
                }
            }
            else
            {
                //reset timer
                verticalHoldCounter = 0f;
            }
        }

        //apply final camera point positions
        cameraPoint.localPosition = new Vector2(horizontalOffset, verticalOffset);

        //faster falling
        if (rb.linearVelocity.y < 0f)
        {
            //is moving down, sets speed to gravity * multiplier
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
        }

        //work out the current state and send it to the animator
        UpdateState();
        anim.SetInteger("state", (int)state);
    }
}