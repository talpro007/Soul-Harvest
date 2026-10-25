using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private SpriteRenderer[] sprites;
    private float[] baseAlphas;

    [Header("stats")]
    [SerializeField] int baseHealth;
    [SerializeField] int health;
    [SerializeField] private float invincibleTime = 1.2f;
    [SerializeField] private float invincibleCounter = 0f;
    [SerializeField] private float flickerAlpha = 0.4f;
    [SerializeField] private float flickerSpeed = 20f;
    public bool CanHeal => health > 0 && health < baseHealth;

    [Header("UI")]
    [SerializeField] HealthUI HealthUI;

    public bool IsInvincible => invincibleCounter > 0f;

    public void TakeDamage(int damage)
    {
        //ignore damage during i-frames or when already dead
        if (IsInvincible || health <= 0) return;

        health = Mathf.Max(health - damage, 0);
        HealthUI.SetHealth(health);
        
        Debug.Log("Player took damage.");

        //start i-frames
        invincibleCounter = invincibleTime;

        if (health <= 0)
        {
            Die();
        }
    }

    

    public void Heal(int amount)
    {
        if (!CanHeal) return;

        health = Mathf.Min(health + amount, baseHealth);
        HealthUI.SetHealth(health);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            //no damage and no knockback while invincible or dead
            if (IsInvincible || health <= 0) return;

            //damage on touching enemy
            TakeDamage(1);

            //check which side the enemy is on and apply knockback in other direction
            int dir = transform.position.x > collision.gameObject.transform.position.x ? 1 : -1;
            playerMovement.Knockback(dir);
        }
    }

    public void Die()
    {
        Animator animator = GetComponent<Animator>();
        Debug.Log("Player died");
        animator.SetTrigger("Death");
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Start()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();

        //grabs this object's and all children's sprite renderers (including inactive ones, like weapons)
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        baseAlphas = new float[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            baseAlphas[i] = sprites[i].color.a;
        }
        health = baseHealth;

        HealthUI.SetMaxHealth(baseHealth);
        HealthUI.SetHealth(health);
    }

    private void Update()
    {
        //tick i-frames and fade the sprite between full and flickerAlpha while active
        if (invincibleCounter > 0f)
        {
            invincibleCounter -= Time.deltaTime;

            bool faded = Mathf.FloorToInt(invincibleCounter * flickerSpeed) % 2 == 0;
            SetAlpha(faded ? flickerAlpha : 1f);

            if (invincibleCounter <= 0f)
            {
                SetAlpha(1f);
            }
        }
    }

    //multiplier of each renderer's own starting alpha, 1 = normal
    private void SetAlpha(float multiplier)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            Color c = sprites[i].color;
            c.a = baseAlphas[i] * multiplier;
            sprites[i].color = c;
        }
    }
}