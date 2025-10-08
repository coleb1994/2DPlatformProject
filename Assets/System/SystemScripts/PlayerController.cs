using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[SelectionBase]
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    public PlayerControllerStatsSO playerStats;

    // Movement
    private PlayerMovementStateSO currentMovementState;
    private bool isInWater = false;

    // Jump
    private PlayerJumpStateSO currentJumpValues;
    private bool isJumping = false;
    private bool jumpQueued = false;
    private float lastJumpQueueTime = 0;
    private float lastJumpTime = -1;
    private float lastJumpDuration = -1;
    private float jumpStartedThreshold = 0.1f;
    private int jumpsSinceGroundTouch = 0;
    private int currentSequenceJump = -1;
    private float sequenceJumpMinimumtime = 0.135f; // Prevents short hops from triggering sequence jump
    private Transform currentSpawnPoint;
    private Transform startSpawnPoint;

    // Crouching
    private bool isCrouching = false;
    private Vector2 originalColliderSize = Vector2.one;
    private Vector2 originalColliderOffset = Vector2.zero;

    [Space]
    [Header("Miscellaneous")]
    [Tooltip("The global y coordinates that will cause the player to respawn if they fall below.")]
    public float deathHeight = -301f;
    [Tooltip("The sound that plays when the player jumps.")]
    public AudioClip jumpSound;
    [Tooltip("The sound that plays when the player hits a wall.")]
    public AudioClip bumpSound;
    [Tooltip("The sound that plays when the player dies.")]
    public AudioClip deathSound;

    // System Variables
    [Space]
    public PlayerSystemVariables systemVariables;
    private bool controlEnabled = true;
    private bool isOnGround = false;
    private float lastOnGroundTime = 0;
    private float lastLandTime = 0;
    private bool onWallLeft = false;
    private bool onWallRight = false;
    private bool isFacingLeft = false;

    // Component References
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Rigidbody2D rigidbody2D;
    private BoxCollider2D collider2D;
    private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance != null)
        {
            Debug.Log("WARNING: More than one player detected!");
        }
        instance = this;

        if (playerStats == null)
        {
            Debug.Log("WARNING: PlayerStats is null. Drag in a PlayerControllerStatsSO object into the playerStats field");
        }

        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["move"];

        rigidbody2D = GetComponent<Rigidbody2D>();
        collider2D = GetComponent<BoxCollider2D>();
        audioSource = GetComponentInChildren<AudioSource>();

        currentMovementState = playerStats.groundMovementValues;

        originalColliderSize = collider2D.size;
        originalColliderOffset = collider2D.offset;

        startSpawnPoint = new GameObject().transform;
        startSpawnPoint.position = transform.position;
        startSpawnPoint.gameObject.name = "InitialSpawnPoint";
        currentSpawnPoint = startSpawnPoint;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 moveDirection = moveAction.ReadValue<Vector2>();

        CheckGroundState();
        CheckWallStates(moveDirection);
        DetermineMovementState();

        if (controlEnabled && isCrouching)
        {
            SetFriction(currentMovementState.crouchFriction);
        }
        else if (controlEnabled && !IsTryingToStop(moveDirection))
        {
            SetFriction(currentMovementState.moveFriction);
            rigidbody2D.AddForce(new Vector3(moveDirection.x * currentMovementState.moveAcceleration, 0, 0));
            if (moveDirection.x != 0) isFacingLeft = moveDirection.x < 0;
        }
        else
        {
            SetFriction(currentMovementState.stopFriction);
        }

        if ((IsOnGround() || (playerStats.resetDoubleJumpsOnWall && playerStats.allowWallJump && IsOnWall())) && (Time.time - lastJumpTime) > jumpStartedThreshold)
        {
            jumpsSinceGroundTouch = 0;
        }

        if (controlEnabled && jumpQueued)
        {
            if (CanJump() && (Time.time - lastJumpQueueTime) < playerStats.jumpQueueTime)
            {
                jumpQueued = false;
                DetermineJumpValues();
                PerformJump();
            }
        }

        DetermineGravityScale();

        rigidbody2D.linearVelocity = new Vector2(
            Mathf.Clamp(rigidbody2D.linearVelocity.x, -currentMovementState.maxSpeed, currentMovementState.maxSpeed),
            Mathf.Clamp(rigidbody2D.linearVelocity.y, -currentMovementState.maxFallSpeed, playerStats.maxJumpSpeed)
        );

        SetAnimatorStates();

        if (transform.position.y < deathHeight) Respawn();
    }

    private void SetFriction(float aFriction)
    {
        if (IsOnGround())
        {
            PhysicsMaterial2D material = rigidbody2D.sharedMaterial;
            material.friction = aFriction;
            rigidbody2D.sharedMaterial = material;
            rigidbody2D.linearDamping = currentMovementState.airDrag;
        }
        else
        {
            rigidbody2D.linearDamping = aFriction;
        }
    }


    // --------- Movement States ---------

    private void DetermineMovementState()
    {
        if (IsOnGround())
        {
            currentMovementState = playerStats.groundMovementValues;
            isJumping = false;
        }
        else if (IsOnWall())
        {
            currentMovementState = playerStats.wallMovementValues;
            isJumping = false;
        }
        else
        {
            currentMovementState = playerStats.airMovementValues;
        }
    }

    private void CheckGroundState()
    {
        bool onGroundLastFrame = isOnGround;
        Physics2D.queriesHitTriggers = true;
        Vector3 worldCenter = systemVariables.groundCheckCollider.transform.TransformPoint(systemVariables.groundCheckCollider.offset);
        Vector3 worldHalfExtents = systemVariables.groundCheckCollider.transform.TransformVector(systemVariables.groundCheckCollider.size * 0.5f); // only necessary when collider is scaled by non-uniform transform
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(worldCenter, worldHalfExtents, systemVariables.groundCheckCollider.transform.rotation.z, systemVariables.groundMask);
        isOnGround = false;
        isInWater = false;
        foreach (Collider2D eachOverlap in overlaps)
        {
            if (!eachOverlap.isTrigger)
            {
                isOnGround = true;
            }
            if (eachOverlap.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                isOnGround = playerStats.canSwim;
                isInWater = true;
            }
        }

        if (onGroundLastFrame && !isOnGround)
        {
            lastOnGroundTime = Time.time;
        }

        if (!onGroundLastFrame && isOnGround)
        {
            lastLandTime = Time.time;
            if (systemVariables.landParticle != null) systemVariables.landParticle.Play();
            if(!IsOnWall()) PlaySfx(bumpSound,1);
        }
    }

    private bool IsOnGround()
    {
        return isOnGround;
    }

    private bool IsOnGroundWithCoyoteTime()
    {
        return IsOnGround() || ((jumpsSinceGroundTouch <= 0) && (Time.time - lastOnGroundTime) < playerStats.coyoteTime);
    }

    private void CheckWallStates(Vector2 moveDirection)
    {
        bool wasOnWallLeft = onWallLeft;
        Physics2D.queriesHitTriggers = false;
        Vector3 worldCenter = systemVariables.leftWallChecker.transform.TransformPoint(systemVariables.leftWallChecker.offset);
        Vector3 worldHalfExtents = systemVariables.leftWallChecker.transform.TransformVector(systemVariables.leftWallChecker.size * 0.5f);
        onWallLeft = Physics2D.OverlapBox(worldCenter, worldHalfExtents, systemVariables.leftWallChecker.transform.rotation.z, systemVariables.groundMask);
        if (!wasOnWallLeft && onWallLeft)
        {
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.leftWallHitParticle != null) systemVariables.leftWallHitParticle.Play();
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.leftWallSlideParticle != null && !systemVariables.leftWallSlideParticle.isPlaying)
            {
                systemVariables.leftWallSlideParticle.Play();
            }
            PlaySfx(bumpSound,1);
        }
        if (!IsOnGround() && moveDirection != Vector2.zero && !onWallLeft && systemVariables.leftWallSlideParticle != null && systemVariables.leftWallSlideParticle.isPlaying)
        {
            systemVariables.leftWallSlideParticle.Stop();
        }

        bool wasOnWallRight = onWallRight;
        worldCenter = systemVariables.rightWallChecker.transform.TransformPoint(systemVariables.rightWallChecker.offset);
        worldHalfExtents = systemVariables.rightWallChecker.transform.TransformVector(systemVariables.rightWallChecker.size * 0.5f);
        onWallRight = Physics2D.OverlapBox(worldCenter, worldHalfExtents, systemVariables.rightWallChecker.transform.rotation.z, systemVariables.groundMask);
        if (!wasOnWallRight && onWallRight)
        {
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.rightWallHitParticle != null) systemVariables.rightWallHitParticle.Play();
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.rightWallSlideParticle != null && !systemVariables.rightWallSlideParticle.isPlaying)
            {
                systemVariables.rightWallSlideParticle.Play();
            }
            PlaySfx(bumpSound,1);
        }
        if (!IsOnGround() && moveDirection != Vector2.zero && !onWallRight && systemVariables.rightWallSlideParticle != null && systemVariables.rightWallSlideParticle.isPlaying)
        {
            systemVariables.rightWallSlideParticle.Stop();
        }
    }

    private bool IsOnWall()
    {
        return onWallLeft || onWallRight;
    }

    private bool IsTryingToStop(Vector2 moveDirection)
    {
        if (!IsOnWall() && moveDirection == Vector2.zero) return true;
        if (!IsOnGround() && onWallLeft && moveDirection.x < -0.1f) return true;
        if (!IsOnGround() && onWallRight && moveDirection.x > 0.1f) return true;
        return false;
    }



    private void DetermineGravityScale()
    {
        if (isCrouching)
        {
            rigidbody2D.gravityScale = currentMovementState.crouchGravity;
        }
        else if (isJumping && Mathf.Abs(rigidbody2D.linearVelocity.y) < currentJumpValues.airHangThreshold && (Time.time - lastJumpTime) > jumpStartedThreshold)
        {
            rigidbody2D.gravityScale = currentJumpValues.airHangGravity;
        }
        else if (isJumping && rigidbody2D.linearVelocity.y > 0)
        {
            rigidbody2D.gravityScale = currentJumpValues.jumpGravity;
        }
        else
        {
            rigidbody2D.gravityScale = currentMovementState.fallGravity;
        }
    }


    // --------- Jumping ---------

    private bool CanJump()
    {
        return IsOnGroundWithCoyoteTime()
            || (IsOnWall() && playerStats.allowWallJump)
            || (jumpsSinceGroundTouch <= playerStats.doubleJumps && !isInWater);
    }

    private void DetermineJumpValues()
    {
        if (!IsOnGround() && IsOnWall())
        {
            currentJumpValues = playerStats.wallJumpValues;
        }
        else if (IsOnGroundWithCoyoteTime()) // Ground Sequence Jumps
        {
            if ((Time.time - lastLandTime > playerStats.sequenceJumpTime) || isCrouching || lastJumpDuration < sequenceJumpMinimumtime)
            {
                currentSequenceJump = -1;
            }

            if (currentSequenceJump < 0)
            {
                currentJumpValues = playerStats.jumpValues;
            }
            else
            {
                currentJumpValues = playerStats.sequenceJumpValues[Mathf.Clamp(currentSequenceJump, 0, Mathf.Min(playerStats.sequenceJumpValues.Length - 1, playerStats.sequenceJumps))];
            }
            currentSequenceJump++;
        }
        else // Air Multi Jumps
        {
            currentJumpValues = playerStats.doubleJumpValues[Mathf.Clamp(jumpsSinceGroundTouch - 1, 0, playerStats.doubleJumpValues.Length - 1)];
        }
    }

    private void PerformJump()
    {
        rigidbody2D.gravityScale = currentJumpValues.jumpGravity;
        rigidbody2D.linearVelocityY = Mathf.Max(0, rigidbody2D.linearVelocityY);
        rigidbody2D.AddForce(new Vector3(0, currentJumpValues.jumpForce, 0));
        if (currentJumpValues is PlayerWallJumpStateSO)
        {
            PlayerWallJumpStateSO wallJumpValues = currentJumpValues as PlayerWallJumpStateSO;
            if (onWallRight)
            {
                rigidbody2D.AddForce(new Vector3(-wallJumpValues.horizontalJumpForce, 0, 0));
            }
            else if (onWallLeft)
            {
                rigidbody2D.AddForce(new Vector3(wallJumpValues.horizontalJumpForce, 0, 0));
            }
        }
        isJumping = true;
        lastJumpTime = Time.time;
        jumpsSinceGroundTouch++;
        SetAnimatorTrigger("jump");
        PlaySfx(jumpSound,
            Random.Range(1f - systemVariables.randomJumpSoundPitchFluctuation, 1f + systemVariables.randomJumpSoundPitchFluctuation)
                 + (jumpsSinceGroundTouch * systemVariables.jumpSoundSequencePitchIncrease)
                 + (Mathf.Min(currentSequenceJump,playerStats.sequenceJumpValues.Length) * systemVariables.jumpSoundSequencePitchIncrease)
            );
    }

    private void QueueJump()
    {
        jumpQueued = true;
        lastJumpQueueTime = Time.time;
    }


    // --------- Input ---------

    public void JumpButtonPressed(InputAction.CallbackContext aContext)
    {
        if (controlEnabled && aContext.phase == InputActionPhase.Performed)
        {
            QueueJump();
        }
        else if (aContext.phase == InputActionPhase.Canceled)
        {
            isJumping = false;
            rigidbody2D.gravityScale = currentMovementState.fallGravity;
            lastJumpDuration = Time.time - lastJumpTime;
            ResetAnimatorTrigger("jump");
        }
    }

    public void CrouchButtonPressed(InputAction.CallbackContext aContext)
    {
        if (controlEnabled && aContext.phase == InputActionPhase.Performed && playerStats.allowCrouch)
        {
            isCrouching = true;
            collider2D.size = new Vector2(originalColliderSize.x, playerStats.crouchColliderHeight);
            collider2D.offset = new Vector2(originalColliderOffset.x, -(playerStats.crouchColliderHeight / 2));
            if (systemVariables.slideParticle != null && !systemVariables.slideParticle.isPlaying)
            {
                systemVariables.slideParticle.Play();
            }
        }
        else if (aContext.phase == InputActionPhase.Canceled)
        {
            isCrouching = false;
            collider2D.size = originalColliderSize;
            collider2D.offset = originalColliderOffset;
            if (systemVariables.slideParticle != null && systemVariables.slideParticle.isPlaying)
            {
                systemVariables.slideParticle.Stop();
            }
        }
    }

    // --------- Spawning ---------

    public void Respawn()
    {
        if (systemVariables.deathParticlePrefab != null)
        {
            GameObject.Instantiate(systemVariables.deathParticlePrefab, transform.position, Quaternion.identity);
        }
        StartCoroutine((ResetCameraCoroutine()));
        PlaySfx(deathSound, 1);

        if (currentSpawnPoint != null)
        {
            transform.position = currentSpawnPoint.position;
        }
        else if (startSpawnPoint != null)
        {
            transform.position = startSpawnPoint.position;
        }
    }

    private IEnumerator ResetCameraCoroutine()
    {
        CameraFollow.instance.SetCameraTarget(null);
        EnablePlayerControl(false);
        if (systemVariables.spritesParent != null) systemVariables.spritesParent.SetActive(false);
        yield return new WaitForSeconds(systemVariables.waitToResetCameraAfterDeath);
        CameraFollow.instance.ResetCameraToPlayer();
        if (systemVariables.spritesParent != null) systemVariables.spritesParent.SetActive(true);
        EnablePlayerControl(true);
    }

    public void SetSpawnPoint(Transform aPoint)
    {
        currentSpawnPoint = aPoint;
    }


    // --------- Animation ---------

    private void SetAnimatorStates()
    {
        SetAnimatorBool("onGround", IsOnGround());
        SetAnimatorBool("falling", !IsOnGround() && IsFalling());
        SetAnimatorBool("crouching", isCrouching);
        SetAnimatorBool("wallLeft", onWallLeft);
        SetAnimatorBool("wallRight", onWallRight);
        SetAnimatorFloat("VerticalSpeed", rigidbody2D.linearVelocity.y);
        SetAnimatorFloat("HorizontalSpeed", rigidbody2D.linearVelocity.x);
        foreach (SpriteRenderer each in systemVariables.flipSprites)
        {
            if (each != null) each.flipX = isFacingLeft;
        }
    }

    private void SetAnimatorTrigger(string aTrigger)
    {
        foreach (Animator each in systemVariables.animators)
        {
            if (each != null) each.SetTrigger(aTrigger);
        }
    }

    private void ResetAnimatorTrigger(string aTrigger)
    {
        foreach (Animator each in systemVariables.animators)
        {
            if (each != null) each.ResetTrigger(aTrigger);
        }
    }

    private void SetAnimatorBool(string aBoolName, bool aBoolValue)
    {
        foreach (Animator each in systemVariables.animators)
        {
            if (each != null) each.SetBool(aBoolName, aBoolValue);
        }
    }

    private void SetAnimatorFloat(string aName, float aValue)
    {
        foreach (Animator each in systemVariables.animators)
        {
            if (each != null) each.SetFloat(aName, aValue);
        }
    }

    public bool IsFalling()
    {
        return rigidbody2D.linearVelocityY < 0;
    }

    public bool IsFacingLeft()
    {
        return isFacingLeft;
    }

    // --------- Audio ---------

    private void PlaySfx(AudioClip clip, float pitch)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, systemVariables.sfxVolume);
        }
    }

    // --------- External Manipulation ---------

    public void EnablePlayerControl(bool enable)
    {
        controlEnabled = enable;
        if (!controlEnabled)
        {
            isCrouching = false;
            isJumping = false;
            jumpQueued = false;
        }
    }

    public void SetDoubleJumpCount(int amount)
    {
        playerStats.doubleJumps = amount;
    }

    public void AdjustDoubleJumpCount(int amount)
    {
        playerStats.doubleJumps += amount;
    }

    public void SetSequenceJumpCount(int amount)
    {
        playerStats.sequenceJumps = amount;
    }

    public void AdjustSequenceJumpCount(int amount)
    {
       playerStats. sequenceJumps += amount;
    }

    public void EnableCrouch(bool enable)
    {
        playerStats.allowCrouch = enable;
    }

    public void EnableWallJump(bool enable)
    {
        playerStats.allowWallJump = enable;
    }

    public void EnableWallsResetDoubleJumps(bool enable)
    {
        playerStats.resetDoubleJumpsOnWall = enable;
    }

    public void EnableSwim(bool enable)
    {
        playerStats.canSwim = enable;
    }

    void OnDrawGizmos()
    {
        // Draw line at deathHeight
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawLine(new Vector3(-5000, deathHeight, 0), new Vector3(5000, deathHeight, 0));
    }
}

[System.Serializable]
public class PlayerSystemVariables
{
    public LayerMask groundMask;
    public BoxCollider2D groundCheckCollider;
    public BoxCollider2D leftWallChecker;
    public BoxCollider2D rightWallChecker;
    public GameObject spritesParent;
    public Animator[] animators;
    public SpriteRenderer[] flipSprites;
    public float waitToResetCameraAfterDeath = 2;
    public float sfxVolume = 1;
    public float randomJumpSoundPitchFluctuation = 0.05f;
    public float jumpSoundSequencePitchIncrease = 0.1f;

    [Space]
    [Header("Particles")]
    public ParticleSystem landParticle;
    public ParticleSystem leftWallHitParticle;
    public ParticleSystem rightWallHitParticle;
    public ParticleSystem slideParticle;
    public ParticleSystem leftWallSlideParticle;
    public ParticleSystem rightWallSlideParticle;
    public GameObject deathParticlePrefab;
}