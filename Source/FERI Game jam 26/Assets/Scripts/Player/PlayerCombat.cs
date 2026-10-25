using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer sprite;
    private Animator anim;

    [Header("vars")]
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] PolygonCollider2D atkColisionRight;
    [SerializeField] PolygonCollider2D atkColisionLeft;
    [SerializeField] PolygonCollider2D atkColisionUp;
    [SerializeField] PolygonCollider2D atkColisionDown;

    [Header("stats")]
    [SerializeField] float baseDamage;
    [SerializeField] float finalDamage;
    [SerializeField] float atkCooldown;
    [SerializeField] float atkCooldownTimer;
    [SerializeField] float atkAnimTime = 0.25f;

    private bool attackInput;
    private Vector2 atkDirection;
    private Vector2 lockedDirection;
    private PlayerState lockedState = PlayerState.Attack;
    private PolygonCollider2D debugCollider;

    private PlayerMeter playerMeter;

    public void OnAttack(InputAction.CallbackContext context)
    {
        //when player pressed attack
        if (!context.performed) return;
        attackInput = true;
    }

    public void OnAttackDirection(InputAction.CallbackContext context)
    {
        //attack direction input
        atkDirection = context.ReadValue<Vector2>();
    }

    private PolygonCollider2D GetAttackCollider()
    {
        //up/down were already decided (including the airborne check) when the swing started
        if (lockedState == PlayerState.AttackUp) return atkColisionUp;
        if (lockedState == PlayerState.AttackDown) return atkColisionDown;

        if (lockedDirection.x > 0.5f) return atkColisionRight;
        if (lockedDirection.x < -0.5f) return atkColisionLeft;

        //no horizontal input, attack the way the player is facing
        return playerMovement.direction == 1 ? atkColisionRight : atkColisionLeft;
    }

    //called by an event on the attack animations
    public void Attack()
    {
        if (playerMovement.state == PlayerState.Death) return;
        PolygonCollider2D activeCol = GetAttackCollider();

        //tell movement which attack state to show
        PlayerState atkState = PlayerState.Attack;
        if (activeCol == atkColisionUp) atkState = PlayerState.AttackUp;
        else if (activeCol == atkColisionDown) atkState = PlayerState.AttackDown;
        playerMovement.SetAttackState(atkState, atkAnimTime);

        //select which attack gets gizmos drawn
        debugCollider = activeCol;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(LayerMask.GetMask("Enemy", "Pumpkin"));

        //collect all hit enemies, currently limited to 16 enemies per hit
        Collider2D[] hits = new Collider2D[16];
        int hitCount = activeCol.Overlap(filter, hits);

        bool hitSomething = false;
        for (int i = 0; i < hitCount; i++)
        {
            EnemyHealth enemyHealth = hits[i].GetComponent<EnemyHealth>();
            Boss boss = hits[i].GetComponent<Boss>();

            if (boss != null)
            {
                boss.TakeDamage(finalDamage);
                hitSomething = true;
            }
            else if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(finalDamage);
                hitSomething = true;
            }
        }

        //one bounce per swing, no matter how many enemies were hit
        if (hitSomething)
        {
            if (activeCol == atkColisionDown) playerMovement.Pogo();
            else if (activeCol == atkColisionRight) playerMovement.SideRecoil(-1);
            else if (activeCol == atkColisionLeft) playerMovement.SideRecoil(1);
            //up attacks give no recoil
            playerMeter.AddMeter();
        }

        Debug.Log("Player attacked");
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        playerMeter = GetComponent<PlayerMeter>();

        //temp code in case we add some sort of damage multiplier or smtn
        finalDamage = baseDamage;
    }

    void Update()
    {
        //a dead player can't attack
        if (playerMovement.state == PlayerState.Death) return;

        //tick timers
        atkCooldownTimer -= Time.deltaTime;

        if (atkCooldownTimer <= 0 && attackInput)
        {
            //only sets the state, the hit comes from the animation event calling Attack()
            PlayerState atkState = PlayerState.Attack;

            //vertical has priority, horizontal only used if there's no vertical input
            if (atkDirection.y > 0.5f)
            {
                atkState = PlayerState.AttackUp;
            }
            else if (atkDirection.y < -0.5f && !playerMovement.IsGrounded())
            {
                atkState = PlayerState.AttackDown;
            }

            //lock the choice now so it can't change before the animation event fires
            lockedDirection = atkDirection;
            lockedState = atkState;
            playerMovement.SetAttackState(atkState, atkAnimTime);

            attackInput = false;
            atkCooldownTimer = atkCooldown;
        }
        else attackInput = false;
    }
}