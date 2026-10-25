using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(AudioSource))]
public class Boss : MonoBehaviour, IKnockbackable
{
    private enum Attack { Slash, HighJump, Dash, Approach }

    // numbers = the Animator "state" value (the Animator conditions must use these numbers)
    private enum BossAnim { Idle = 0, Walk = 1, JumpStart = 2, JumpLoop = 3, Land = 4, Run = 5, Dash = 6, Attack = 7, Death = 8 }

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer sprite;
    private Animator anim;
    private AudioSource audioSource;               // one-shot sounds
    private AudioSource loopSource;                // walk / run loops
    private AudioSource dashSource;                // the dash sound, so it can be cut short
    private Transform player;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private Collider2D playerCol;
    private Color baseColor;
    private BossAnim? currentAnim;                 // null until the first SetAnim
    private float floorY;
    private float centerX;
    private float cooldownTimer;
    private float maxHealth;
    private int originalLayer;
    private int dashLayer;
    private float idleSoundTimer;
    private Attack lastAttack = Attack.HighJump;   // the intro counts as a high jump
    private int attacksSinceDash;
    private bool awakened;                         // false = standing in the middle doing nothing
    private bool enraged;                          // true once HP is below the enrage threshold
    private bool busy = true;                      // true until the intro slam is over
    private bool dead;

    [Header("health")]
    [SerializeField] private float health = 50f;

    [Header("death")]
    [SerializeField] private GameObject enableOnDeath;
    [SerializeField] private float deathAnimDuration = 1f;   // length of the death clip
    [SerializeField] private float deathDelay = 5f;          // wait after the clip, then enable the object

    [Header("phases")]
    [SerializeField, Range(0f, 1f)] private float awakenAtPercent = 0.5f;
    [SerializeField, Range(0f, 1f)] private float enrageAtPercent = 0.25f;
    [SerializeField] private int dashEveryNthAttack = 4;

    [Header("arena")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float arenaHalfWidth = 8f;

    [Header("general")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField] private Color windupColor = new Color(1f, 0.5f, 0.5f);

    [Header("approach")]
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float approachTimeMin = 1.5f;
    [SerializeField] private float approachTimeMax = 3f;
    [SerializeField] private float approachCooldown = 0.3f;

    [Header("slash")]
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private float slashRange = 3f;
    [SerializeField] private float slashWindup = 0.5f;
    [SerializeField] private float slashRecovery = 0.6f;
    [SerializeField] private Vector2 slashSize = new Vector2(3f, 2f);
    [SerializeField] private float slashOffset = 1.8f;
    [SerializeField] private int slashDamage = 1;

    // missing fields
    [SerializeField, Range(0f, 1f)] private float comboChance = 0.4f;
    [SerializeField] private float comboGap = 0.25f;
    [SerializeField] private float comboWindup = 0.35f;

    [Header("dash")]
    [SerializeField] private string dashLayerName = "BossDash";   // create under Project Settings > Tags and Layers
    [SerializeField] private float minDashDistance = 5f;
    [SerializeField] private float dashWindup = 0.6f;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashRecovery = 0.8f;
    [SerializeField] private int dashDamage = 2;
    [SerializeField] private float repositionJumpHeight = 3f;

    [Header("high jump")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float jumpWindup = 0.6f;
    [SerializeField] private float jumpRecovery = 0.9f;
    [SerializeField] private float slamRadius = 3.5f;
    [SerializeField] private int slamDamage = 2;

    [Header("effects")]
    [SerializeField] private GameObject slamEffectPrefab;

    [Header("sfx")]
    [FormerlySerializedAs("idle"), SerializeField] private AudioClip idleSfx;           // occasional, while he stands
    [FormerlySerializedAs("walk"), SerializeField] private AudioClip walkSfx;           // loops while he walks
    [FormerlySerializedAs("run"), SerializeField] private AudioClip runSfx;             // loops while he runs
    [FormerlySerializedAs("jumpStart"), SerializeField] private AudioClip jumpStartSfx; // takeoff
    [FormerlySerializedAs("jumpLand"), SerializeField] private AudioClip landSfx;       // every landing
    [FormerlySerializedAs("attack"), SerializeField] private AudioClip attackSfx;       // the slash
    [FormerlySerializedAs("dashSound"), SerializeField] private AudioClip dashSfx;      // the charge
    [FormerlySerializedAs("die"), SerializeField] private AudioClip dieSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.5f;
    [SerializeField] private float idleSoundMinInterval = 4f;
    [SerializeField] private float idleSoundMaxInterval = 8f;
    [SerializeField] private bool idleSoundOnlyWhileDormant = true;
    [SerializeField] private float dashSoundFade = 0.15f;      // fade-out when the charge ends
    [SerializeField] private bool logSounds;                   // tick while debugging, then untick

    [Header("music")]
    [SerializeField] private AudioClip musicDormant;   // leave empty to keep the level's music
    [SerializeField] private AudioClip musicFight;
    [SerializeField] private AudioClip musicEnraged;
    [SerializeField] private float musicFade = 1.5f;


    private float LeftEndX => centerX - arenaHalfWidth;
    private float RightEndX => centerX + arenaHalfWidth;

    // 1 = full health, 0 = dead
    public float HealthPercent => health / maxHealth;
    public bool IsDead => dead;

    // called by the spawner (a prefab can't hold a reference to a scene object)
    public void Init(Transform center) { arenaCenter = center; }
    public void Init(Transform center, GameObject deathObject)
    {
        arenaCenter = center;
        enableOnDeath = deathObject;
    }

    // super armor: hits don't push the boss around
    public void Knockback(int dir) { }

    // ---------- health ----------
    public void TakeDamage(float damage)
    {
        if (dead) return;

        health -= damage;
        Debug.Log(gameObject.name + " took damage");

        if (health <= 0f)
        {
            health = 0f;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Boss died");
        dead = true;
        busy = true;                            // Update() does nothing from now on
        StopAllCoroutines();
        gameObject.layer = originalLayer;       // in case he died mid-dash
        sprite.color = baseColor;               // in case he died mid-windup
        Stop();
        SetAnim(BossAnim.Death);
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathAnimDuration + deathDelay);
        if (enableOnDeath != null) enableOnDeath.SetActive(true);
    }

    void Awake()
    {
        maxHealth = health;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        baseColor = sprite.color;

        originalLayer = gameObject.layer;
        dashLayer = LayerMask.NameToLayer(dashLayerName);
        if (dashLayer < 0)
        {
            Debug.LogWarning("Layer '" + dashLayerName + "' doesn't exist, boss stays on his normal layer while dashing");
            dashLayer = originalLayer;
        }
        // a clip assigned in the Inspector would play on its own, so clear it
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.loop = false;

        // extra sources: walk/run loops, and the dash (so it can be stopped)
        loopSource = AddSource(true, loopVolume);
        dashSource = AddSource(false, sfxVolume);
        idleSoundTimer = Random.Range(idleSoundMinInterval, idleSoundMaxInterval);

        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();
        playerHealth = player.GetComponent<PlayerHealth>();
        playerCol = player.GetComponent<Collider2D>();

        floorY = arenaCenter != null ? arenaCenter.position.y : col.bounds.min.y;
        centerX = arenaCenter != null ? arenaCenter.position.x : rb.position.x;

        PlayMusic(musicDormant);

        // intro: drop in and slam in the middle of the arena (no windup, height 0)
        if (arenaCenter != null) StartCoroutine(HighJumpAttack(centerX, 0f, 0f));
        else busy = false;
    }

    // ---------- audio helpers ----------
    private AudioSource AddSource(bool loop, float volume)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.loop = loop;
        s.playOnAwake = false;
        s.volume = volume;
        return s;
    }

    private void SetAnim(BossAnim a)
    {
        if (a == currentAnim) return;          // only react when the state actually changes
        currentAnim = a;
        anim.SetInteger("state", (int)a);
        UpdateLoopSound(a);
    }

    private void UpdateLoopSound(BossAnim a)
    {
        AudioClip clip = null;
        if (a == BossAnim.Walk) clip = walkSfx;
        else if (a == BossAnim.Run) clip = runSfx;

        if (clip == null)
        {
            loopSource.Stop();
            loopSource.clip = null;
            return;
        }

        if (loopSource.clip == clip && loopSource.isPlaying) return;

        if (logSounds) Debug.Log("Loop: " + clip.name);
        loopSource.clip = clip;
        loopSource.Play();
    }

    private void TickIdleSound()
    {
        if (idleSfx == null) return;

        idleSoundTimer -= Time.deltaTime;
        if (idleSoundTimer > 0f) return;

        PlaySound(idleSfx);
        idleSoundTimer = Random.Range(idleSoundMinInterval, idleSoundMaxInterval);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (logSounds) Debug.Log("SFX: " + clip.name);
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    // fades a source out, stops it, and puts its volume back so the next use is audible
    private IEnumerator FadeOutAndStop(AudioSource s, float time, float restoreVolume)
    {
        float start = s.volume;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            s.volume = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }

        s.Stop();
        s.volume = restoreVolume;
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (MusicManager.Instance == null)
        {
            Debug.LogWarning("Boss: there is no MusicManager in the scene");
            return;
        }

        Debug.Log("Boss music -> " + clip.name);    // remove once it works
        MusicManager.Instance.Play(clip, musicFade);
    }

    // ---------- helpers ----------
    private void Stop() { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); }

    private int FaceTowards(float x)
    {
        int dir = x >= rb.position.x ? 1 : -1;
        sprite.flipX = spriteFacesRight ? dir == -1 : dir == 1;
        return dir;
    }

    private IEnumerator Windup(float time)
    {
        sprite.color = windupColor;
        yield return new WaitForSeconds(time);
        sprite.color = baseColor;
    }

    private void FinishAttack(float cooldown)
    {
        cooldownTimer = cooldown;
        SetAnim(BossAnim.Idle);
        busy = false;
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(col.bounds.center,
            new Vector2(col.bounds.size.x * 0.9f, col.bounds.size.y),
            0f, Vector2.down, 0.1f, groundLayers);
    }

    private bool PlayerInBox(Vector2 center, Vector2 size)
    {
        Rect box = new Rect(center - size / 2f, size);
        Bounds pb = playerCol.bounds;
        Rect playerRect = new Rect(pb.min, pb.size);
        return box.Overlaps(playerRect);
    }

    private void HurtPlayer(int damage, int knockDir)
    {
        if (playerMovement.state == PlayerState.Death) return;
        if (playerHealth.IsInvincible) return;

        playerHealth.TakeDamage(damage);
        playerMovement.Knockback(knockDir);
    }

    // ---------- decisions ----------
    void Update()
    {
        if (busy || dead) return;

        if (playerMovement.state == PlayerState.Death)
        {
            Stop();
            SetAnim(BossAnim.Idle);
            return;
        }

        float hp = HealthPercent;

        // above the awaken threshold he just stands in the middle and does nothing
        if (!awakened)
        {
            Stop();
            SetAnim(BossAnim.Idle);
            TickIdleSound();
            if (hp > awakenAtPercent) return;

            // threshold reached: fight music, a jump-land attack on the player, then the real fight
            Debug.Log("Boss awakens");
            awakened = true;
            PlayMusic(musicFight);
            StartCoroutine(HighJumpAttack(player.position.x, jumpHeight, jumpWindup));
            return;
        }

        // below the enrage threshold: always runs, the dash unlocks, new music
        if (!enraged && hp < enrageAtPercent)
        {
            Debug.Log("Boss enraged");
            enraged = true;
            attacksSinceDash = 0;
            PlayMusic(musicEnraged);
        }

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f)
        {
            float dist = Mathf.Abs(player.position.x - rb.position.x);
            StartAttack(PickAttack(dist));
            return;
        }

        // breather between attacks: stand still and watch the player
        Stop();
        FaceTowards(player.position.x);
        SetAnim(BossAnim.Idle);
        if (!idleSoundOnlyWhileDormant) TickIdleSound();
    }

    private Attack PickAttack(float dist)
    {
        // every Nth attack in a row is a guaranteed dash (once enraged)
        if (enraged && attacksSinceDash >= dashEveryNthAttack - 1) return Attack.Dash;

        List<Attack> options = new List<Attack>();

        if (dist <= slashRange)
        {
            options.Add(Attack.Slash);
            options.Add(Attack.Slash);              // twice = twice as likely
        }
        else
        {
            options.Add(Attack.Approach);
            if (enraged && dist >= minDashDistance)
            {
                options.Add(Attack.Approach);       // far away: running up is the most common choice
                options.Add(Attack.Dash);           // an early dash needs the player to be far
            }
        }
        options.Add(Attack.HighJump);

        // never repeat the last attack, if there is anything else to pick
        if (options.Exists(a => a != lastAttack))
            options.RemoveAll(a => a == lastAttack);

        return options[Random.Range(0, options.Count)];
    }

    private void StartAttack(Attack attack)
    {
        Debug.Log("Boss picks: " + attack);   // remove once the numbers feel right
        lastAttack = attack;

        if (attack == Attack.Dash) attacksSinceDash = 0;
        else if (attack != Attack.Approach) attacksSinceDash++;   // approaching isn't an "ability"

        switch (attack)
        {
            case Attack.Slash: StartCoroutine(SlashAttack()); break;
            case Attack.Dash: StartCoroutine(DashAttack()); break;
            case Attack.Approach: StartCoroutine(ApproachBehaviour()); break;
            case Attack.HighJump:
                StartCoroutine(HighJumpAttack(player.position.x, jumpHeight, jumpWindup));
                break;
        }
    }

    // ---------- behaviours ----------
    private IEnumerator ApproachBehaviour()
    {
        busy = true;
        float timer = Random.Range(approachTimeMin, approachTimeMax);

        while (timer > 0f)
        {
            float dx = player.position.x - rb.position.x;
            if (Mathf.Abs(dx) <= slashRange * 0.8f) break;               // close enough
            if (playerMovement.state == PlayerState.Death) break;

            bool run = enraged;                                          // walks until enraged
            int dir = FaceTowards(player.position.x);
            rb.linearVelocity = new Vector2(dir * (run ? runSpeed : walkSpeed), rb.linearVelocity.y);
            SetAnim(run ? BossAnim.Run : BossAnim.Walk);

            timer -= Time.deltaTime;
            yield return null;
        }

        Stop();
        FinishAttack(approachCooldown);
    }

    private IEnumerator SlashAttack()
    {
        busy = true;
        Stop();
        yield return StartCoroutine(SlashSwing(BossAnim.Attack, slashWindup, false));

        // combo: sometimes follow up if the player is still close
        float dist = Mathf.Abs(player.position.x - rb.position.x);
        if (Random.value < comboChance && dist <= slashRange * 1.5f)
        {
            yield return new WaitForSeconds(comboGap);
            yield return StartCoroutine(SlashSwing(BossAnim.Attack, comboWindup, true));
        }

        yield return new WaitForSeconds(slashRecovery);
        FinishAttack(attackCooldown);
    }

    private IEnumerator SlashSwing(BossAnim swingAnim, float windup, bool restartClip)
    {
        int dir = FaceTowards(player.position.x);
        SetAnim(swingAnim);
        if (restartClip) anim.Play(attackStateName, 0, 0f);   // state is already "Attack", so restart the clip by hand
        yield return StartCoroutine(Windup(windup));

        PlaySound(attackSfx);
        Vector2 center = (Vector2)col.bounds.center + new Vector2(dir * slashOffset, 0f);
        if (PlayerInBox(center, slashSize)) HurtPlayer(slashDamage, dir);
    }

    // jumps to targetX and returns once he has landed (no damage, doesn't finish the attack)
    private IEnumerator JumpTo(float targetX, float height, float windup)
    {
        FaceTowards(targetX);

        if (windup > 0f)
        {
            SetAnim(BossAnim.JumpStart);
            yield return StartCoroutine(Windup(windup));
        }

        float targetY = floorY + (rb.position.y - col.bounds.min.y);
        rb.linearVelocity = CalculateLaunchVelocity(new Vector2(targetX, targetY), height);
        SetAnim(BossAnim.JumpLoop);
        PlaySound(jumpStartSfx);

        float timeout = Time.time + 6f;
        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => (rb.linearVelocity.y <= 0f && IsGrounded()) || Time.time > timeout);

        rb.linearVelocity = Vector2.zero;
        SetAnim(BossAnim.Land);
        PlaySound(landSfx);
    }

    // high jump / awaken / intro: jump, land with a shockwave, recover
    private IEnumerator HighJumpAttack(float targetX, float height, float windup)
    {
        busy = true;
        Stop();

        yield return StartCoroutine(JumpTo(targetX, height, windup));
        Slam();

        yield return new WaitForSeconds(jumpRecovery);
        FinishAttack(attackCooldown);
    }

    // dash: jump to the arena end farther from the player, then charge across to the other end
    private IEnumerator DashAttack()
    {
        busy = true;
        Stop();

        bool playerOnLeft = player.position.x < centerX;
        float startX = playerOnLeft ? RightEndX : LeftEndX;
        float endX = playerOnLeft ? LeftEndX : RightEndX;

        // 1) reposition jump (no slam)
        yield return StartCoroutine(JumpTo(startX, repositionJumpHeight, jumpWindup));

        // 2) turn around and telegraph the charge
        int dir = FaceTowards(endX);
        SetAnim(BossAnim.Dash);
        yield return StartCoroutine(Windup(dashWindup));

        gameObject.layer = dashLayer;
        // 3) charge to the other end (a timeout stops it hanging if he's blocked)
        if (dashSfx != null)
        {
            if (logSounds) Debug.Log("Dash sound: " + dashSfx.name);
            dashSource.volume = sfxVolume;
            dashSource.clip = dashSfx;
            dashSource.Play();
        }

        bool hitPlayer = false;
        float timeout = Time.time + 4f;
        while (Time.time < timeout && (dir > 0 ? rb.position.x < endX : rb.position.x > endX))
        {
            rb.linearVelocity = new Vector2(dir * dashSpeed, rb.linearVelocity.y);

            if (!hitPlayer && PlayerInBox(col.bounds.center, col.bounds.size))
            {
                HurtPlayer(dashDamage, dir);
                hitPlayer = true;
            }

            yield return null;
        }

        gameObject.layer = originalLayer;
        // the charge is over: fade the dash sound out, even if the clip is longer
        StartCoroutine(FadeOutAndStop(dashSource, dashSoundFade, sfxVolume));

        Stop();
        yield return new WaitForSeconds(dashRecovery);
        FinishAttack(attackCooldown);
    }

    private Vector2 CalculateLaunchVelocity(Vector2 target, float height)
    {
        float g = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y * rb.gravityScale));
        float dy = target.y - rb.position.y;
        float h = Mathf.Max(height, dy + 0.1f);          // the peak must be above the target

        float vy = Mathf.Sqrt(2f * g * h);
        float timeUp = vy / g;
        float timeDown = Mathf.Sqrt(2f * (h - dy) / g);

        float vx = (target.x - rb.position.x) / (timeUp + timeDown);
        return new Vector2(vx, vy);
    }

    private void Slam()
    {
        if (slamEffectPrefab != null)
            Instantiate(slamEffectPrefab, new Vector2(rb.position.x, col.bounds.min.y), Quaternion.identity);

        if (!playerMovement.IsGrounded()) return;       // jump over the shockwave to dodge

        float dx = player.position.x - rb.position.x;
        if (Mathf.Abs(dx) > slamRadius) return;

        HurtPlayer(slamDamage, dx >= 0f ? 1 : -1);
    }

    // ---------- death ----------
    private void OnDied()
    {
        StopAllCoroutines();               // this also ends a dash-sound fade, so stop it by hand
        dashSource.Stop();
        dashSource.volume = sfxVolume;

        busy = true;                       // Update does nothing from now on
        sprite.color = baseColor;          // in case he died during a windup tint
        rb.linearVelocity = Vector2.zero;
        gameObject.tag = "Untagged";       // a dying boss shouldn't hurt on contact
        gameObject.layer = 0;              // ...and your attacks shouldn't hit him any more

        SetAnim(BossAnim.Death);
        PlaySound(dieSfx);

        if (MusicManager.Instance != null) MusicManager.Instance.ReturnToLevelTrack(musicFade);
    }

    // cyan = the arena ends he jumps to and dashes between, yellow = slash box, red = slam radius
    private void OnDrawGizmosSelected()
    {
        float cx = arenaCenter != null ? arenaCenter.position.x : transform.position.x;
        float y = transform.position.y;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(cx - arenaHalfWidth, y - 3f), new Vector3(cx - arenaHalfWidth, y + 3f));
        Gizmos.DrawLine(new Vector3(cx + arenaHalfWidth, y - 3f), new Vector3(cx + arenaHalfWidth, y + 3f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.right * slashOffset, slashSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}