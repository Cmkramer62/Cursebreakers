using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour {
    
    public GroundChecker groundCheckerScript;

    public float speed = 12f, staminaRecoveryRate = 1f, staminaDuration = 40f, jumpHeight = 1;



    public float crouchHeight = -0.696f, currentHeight = 0f, gravityStrength = -22f;

    [SerializeField]
    private bool shouldBeSlowed = false, sliding = false;
    public bool allowedToMove = true, allowedToCrouch = true, isSprinting = false,
        isCrouched = false, isHiding = false, isTired = false, playerAlive = true;

    [SerializeField]
    public CharacterController controller;
    public bool lockCursor = true;
    public Transform groundCheck;
    public float amountCrouchSpots = 0f;
    public LayerMask groundMask;

    [SerializeField] Animator playerAnimator, armsAnimator;

    public ConeLOSDetector enemyVisionScript;
    #region SOUND VARIABLES
    public AudioSource source;
    public AudioClip breathClip, crouchClip, ghostJump, slideClip;
    public AudioClip[] jumpClip;
    #endregion

    public LightFlickerNonNetworked lanternReference;


    [SerializeField] private PlayerHandler playerHandlerScript;
    [SerializeField] private Transform headTransform;

    [SerializeField] private ParticleSystem feathersVFXA, feathersVFXB;



    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float groundedStickForce = 8f;

    // =================
    // WALL HOP VARIABLES
    // =================
    [SerializeField] private float wallCastRadius = 0.18f;
    [SerializeField] private float wallCastDistance = 0.35f;
    [SerializeField] private float wallCastHeight = 1.1f;
    [SerializeField] private float wallSlideDuration = 1f;
    [SerializeField] private float wallSlideSpeed = 1.25f;
    [SerializeField] private float wallHopOutwardSpeed = 6f;
    [SerializeField] private float wallHopForwardSpeed = 6f;
    [SerializeField] private float sideWallHopUpwardMultiplier = 1f;
    [SerializeField] private float forwardWallHopUpwardMultiplier = 1f;
    [SerializeField] private float maximumWallNormalY = 0.25f;
    [HideInInspector] public bool isWallSticking;
    private bool wallSlideUsedForCurrentContact;
    private Vector3 currentWallNormal;
    private bool currentWallIsSide;
    private bool currentWallIsRightSide;
    private float wallStickTimer;
    private LayerMask wallMask;

    // =================
    // SLIDE VARIABLES
    // =================
    [SerializeField] private bool slideConsumesAllStamina = false;
    [SerializeField] private float staminaConsumtionOnSlide = 0f, slideCooldown = 100f, slideSpeed = 4f, slideDuration = 2f;
    public bool slideOnCooldown = false;
    private Vector3 slideDirection;
    [SerializeField] private float slideDeceleration = 25f;
    [SerializeField] private float downhillSlideAcceleration = 15f;
    [SerializeField] private float uphillSlideDeceleration = 35f;
    [SerializeField] private float slideEndSpeed = 11f;
    [SerializeField] private float slopeEffect = 15f; // How much the degree of the slope effects the change in acceleration of a slide.
    
    // =================
    // CROUCH VARIABLES
    // =================
    [SerializeField] private float crouchedControllerHeight = 1.2f;
    [SerializeField] private float crouchSpeed = 4f;
    [SerializeField] private LayerMask standClearanceMask = ~0;

    private float standingControllerHeight;
    private float targetControllerHeight;
    private Vector3 standingControllerCenter;
    private bool standUpRequested;
    public KeyCode crouchKey = KeyCode.LeftControl;


    // =================
    // PRIVATE VARIABLES
    // =================
    private Vector3 horizontalVelocity;
    private Vector3 verticalVelocity;
    private float afterlifeFallAugment = 1f, afterlifeSpeedAugment = 1f, afterlifeJumpAugment = 1f;
    private bool afterlife = false;
    private Vector3 fallingVelocity, originalScale, originalHeadHeight;
    private Transform cachedTransform;
    private Coroutine airRoutine;
    private float originalSpeed, crouchingSpeed, sprintActualMultiplier = 1f, sprintMultiplier = 3f;
    

    private void Awake() {

        //sprintRemaining = sprintDuration;
        cachedTransform = transform.parent.GetComponent<Transform>();
        originalScale = cachedTransform.localScale;
        originalHeadHeight = headTransform.localPosition;
        currentHeight = originalHeadHeight.y;

        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        wallMask = 0;
        if(wallLayerIndex >= 0) wallMask |= 1 << wallLayerIndex;
        if(groundLayerIndex >= 0) wallMask |= 1 << groundLayerIndex;
    }

    private void Start() {

        originalSpeed = speed;
        crouchingSpeed = speed / 2;
        if(lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
        }

        standingControllerHeight = controller.height;
        standingControllerCenter = controller.center;
        targetControllerHeight = isCrouched
            ? Mathf.Max(crouchedControllerHeight, controller.radius * 2f)
            : standingControllerHeight;
    }

    public override void OnNetworkSpawn() {
        if(!IsOwner) return;

        StartCoroutine(FindUIManager());
    }

    // The OnNetworkSpawn occurs before the scene has fully loaded. So we wait until it has, and find what we need.
    private IEnumerator FindUIManager() {
        var uiManagerInstance = UIManager.Instance;
        while(uiManagerInstance == null) {
            uiManagerInstance = UIManager.Instance;
            yield return null;
        }

        // Pass this self to the client-side UI Manager so it can handle the UI of this.
        UIManager.Instance.RegisterPlayer(this);
        UIManager.Instance.RegisterToolController(transform.parent.GetComponent<ToolController>());

        feathersVFXA.Stop();
        feathersVFXB.Stop();

        GetComponentInParent<Death>().afterlifePlayer.OnValueChanged += OnAfterlifeChanged;
    }

    private void OnAfterlifeChanged(bool oldState, bool newState) {
        // Accordingly (as onafterlife may trigger to enter but also to leave).
        // increase speed.
        // increase hop dist.
        // lower gravity rate.
        // disable slide ability.
        // disable crouch ability?

        afterlifeFallAugment = newState ? .5f : 1f;
        afterlifeSpeedAugment = newState ? 1.5f : 1f;
        afterlifeJumpAugment = newState ? 1.4f : 1f;
        afterlife = newState;
    }

    public void ResetVarsToDefaults() {
        allowedToMove = true;
        allowedToCrouch = true;

        if(isCrouched) Crouch(true);   
    }

    public void Crouch(bool affectSpeed) {
        bool shouldCrouch = !isCrouched;

        if(!shouldCrouch && !HasStandingClearance()) {
            standUpRequested = true;
            return;
        }

        ApplyCrouchState(shouldCrouch, affectSpeed);
    }

    private void ApplyCrouchState(bool crouched, bool affectSpeed) {
        if(isCrouched == crouched) return;

        isCrouched = crouched;
        standUpRequested = false;
        if(affectSpeed) speed = crouched ? crouchingSpeed : originalSpeed;
        SetControllerCrouched(crouched);
        CrouchEffectsServerRpc();
    }

    private bool HasStandingClearance() {
        Vector3 worldCenter = controller.transform.TransformPoint(standingControllerCenter);
        float scaleY = controller.transform.lossyScale.y;
        float scaleXZ = Mathf.Max(controller.transform.lossyScale.x, controller.transform.lossyScale.z);
        float radius = controller.radius * scaleXZ;
        float height = Mathf.Max(standingControllerHeight * scaleY, radius * 2f);
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 capsuleBottom = worldCenter - controller.transform.up * halfSegment;
        Vector3 capsuleTop = worldCenter + controller.transform.up * halfSegment;

        Collider[] overlaps = Physics.OverlapCapsule(
            capsuleBottom,
            capsuleTop,
            radius,
            standClearanceMask,
            QueryTriggerInteraction.Ignore
        );

        for(int i = 0; i < overlaps.Length; i++) {
            Transform hitTransform = overlaps[i].transform;
            if(hitTransform == controller.transform || hitTransform.IsChildOf(controller.transform)) continue;
            return false;
        }

        return true;
    }

    private void SetControllerCrouched(bool crouched) {
        targetControllerHeight = crouched
            ? Mathf.Max(crouchedControllerHeight, controller.radius * 2f)
            : standingControllerHeight;
    }

    private void UpdateControllerHeight() {
        controller.height = Mathf.MoveTowards(
            controller.height,
            targetControllerHeight,
            crouchSpeed * Time.deltaTime
        );
        controller.center = standingControllerCenter
            - Vector3.up * ((standingControllerHeight - controller.height) * 0.5f);
    }

    [ServerRpc]
    private void CrouchEffectsServerRpc() {
        CrouchEffectsClientRpc();
    }

    [ClientRpc]
    private void CrouchEffectsClientRpc() {
        source.PlayOneShot(crouchClip);
    }

    private void Jump() {
        verticalVelocity.y = Mathf.Sqrt( jumpHeight * -2f * afterlifeJumpAugment * gravityStrength );
        PlayJumpEffects();
    }

    private bool TryFindWall(out Vector3 wallNormal, out bool isSideWall, out bool isRightSide) {
        wallNormal = Vector3.zero;
        isSideWall = false;
        isRightSide = false;

        Vector3 forward = Vector3.ProjectOnPlane(cachedTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cachedTransform.right, Vector3.up).normalized;
        Vector3 castOrigin = controller.bounds.center + Vector3.up * (wallCastHeight - controller.height * 0.5f);
        Vector3[] directions = { forward, right, -right };
        RaycastHit hit;

        // Check the front first, then either side. This gives corner contacts a stable priority.
        for(int i = 0; i < directions.Length; i++) {
            if(Physics.SphereCast(castOrigin, wallCastRadius, directions[i], out hit, wallCastDistance, wallMask, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.normal.y) <= maximumWallNormalY) {
                wallNormal = hit.normal;
                isSideWall = i != 0;
                isRightSide = i == 1;
                return true;
            }
        }

        // Sphere casts can miss a surface when the cast sphere already overlaps it.
        Collider[] nearbyColliders = Physics.OverlapSphere(castOrigin, wallCastRadius, wallMask, QueryTriggerInteraction.Ignore);
        float bestDirectionScore = 0.5f;
        for(int i = 0; i < nearbyColliders.Length; i++) {
            Vector3 closestPoint = nearbyColliders[i].ClosestPoint(castOrigin);
            Vector3 normal = castOrigin - closestPoint;
            if(normal.sqrMagnitude < 0.0001f) continue;

            normal.Normalize();
            if(Mathf.Abs(normal.y) > maximumWallNormalY) continue;

            Vector3 towardWall = -normal;
            for(int directionIndex = 0; directionIndex < directions.Length; directionIndex++) {
                float directionScore = Vector3.Dot(directions[directionIndex], towardWall);
                if(directionScore > bestDirectionScore) {
                    bestDirectionScore = directionScore;
                    wallNormal = normal;
                    isSideWall = directionIndex != 0;
                    isRightSide = directionIndex == 1;
                }
            }
        }

        return wallNormal != Vector3.zero;
    }

    private void StartWallHop() {
        Vector3 outward = Vector3.ProjectOnPlane(currentWallNormal, Vector3.up).normalized;
        horizontalVelocity = outward * wallHopOutwardSpeed;
        if(currentWallIsSide) {
            Vector3 forwardAlongWall = Vector3.ProjectOnPlane(cachedTransform.forward, outward).normalized;
            if(Vector3.Dot(forwardAlongWall, cachedTransform.forward) < 0f) forwardAlongWall = -forwardAlongWall;
            // A 45 degree launch between the wall's outward normal and forward travel along it.
            horizontalVelocity = outward * wallHopOutwardSpeed + forwardAlongWall * wallHopForwardSpeed;
        }

        float upwardMultiplier = currentWallIsSide ? sideWallHopUpwardMultiplier : forwardWallHopUpwardMultiplier;
        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * afterlifeJumpAugment * gravityStrength) * upwardMultiplier;
        isWallSticking = false;
        wallStickTimer = 0f;

        // Reuse the normal jump presentation without overwriting the wall-hop launch velocity.
        PlayJumpEffects();
        if(currentWallIsSide) {
            // The clip name follows the direction of travel: a hop from a right wall moves left.
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.SetTrigger(currentWallIsRightSide ? "WallHopRight" : "WallHopLeft");

            armsAnimator.ResetTrigger("Jump");
            armsAnimator.SetTrigger(currentWallIsRightSide ? "WallHopRight" : "WallHopLeft");
        }
        else {
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.SetTrigger("WallHopFront");

            armsAnimator.ResetTrigger("Jump");
            armsAnimator.SetTrigger("WallHopFront");
        }
    }

    private void PlayJumpEffects() {
        PlayerHandler cameraOwner = transform.parent.GetComponent<PlayerHandler>();
        if(cameraOwner != null && cameraOwner.cameraReference != null) {
            cameraOwner.cameraReference.PlayJumpImpulse();
        }

        int spellHeldIndex = transform.parent.GetComponent<ToolController>().heldIndex.Value;
        source.PlayOneShot(afterlife ? ghostJump : jumpClip[Random.Range(0, jumpClip.Length)]);
        playerAnimator.SetTrigger("Jump");
        playerAnimator.SetBool("InAirFromJump", true);
        if(spellHeldIndex == 0 || spellHeldIndex == 3 || spellHeldIndex == 4 || spellHeldIndex == 7) armsAnimator.SetTrigger("Jump");
        armsAnimator.SetBool("Grounded", false);
        if(airRoutine != null) StopCoroutine(airRoutine);
        airRoutine = StartCoroutine(InAirFromJumpTimer());
    }

    // Wrong.
    private IEnumerator InAirFromJumpTimer() {
        yield return new WaitForSeconds(1f);
        playerAnimator.SetBool("InAirFromJump", false);
    }

    public bool TiredState() { return isTired; }

    public bool SlidingState() { return sliding; }

    /// <summary>Returns the current world-space movement velocity for ragdoll handoff.</summary>
    public Vector3 GetCurrentVelocity() {
        return horizontalVelocity + verticalVelocity;
    }

    /// <summary>Clears saved movement momentum and transient movement states after ragdoll recovery.</summary>
    public void ResetMovementVelocity() {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        slideDirection = Vector3.zero;
        sliding = false;
        allowedToCrouch = true;
        isSprinting = false;
        isWallSticking = false;
        wallStickTimer = 0f;
        wallSlideUsedForCurrentContact = false;
        StopSlideEffectsServerRpc();

        if(isCrouched) {
            standUpRequested = true;
            if(HasStandingClearance()) ApplyCrouchState(false, true);
        }
    }

    public float GetRemainingStam() {
        return transform.parent.GetComponent<PlayerHandler>().stamina.Value / staminaDuration;
    }

    void Update() {
        if(!IsOwner) {
            enabled = false;
            return;
        }

        if(!playerAlive) return;

        // Keep a released crouch request pending until the standing capsule fits.
        if(standUpRequested && HasStandingClearance()) {
            ApplyCrouchState(false, true);
        }
        UpdateControllerHeight();

        // =========================
        // MOVEMENT / JUMP / GRAVITY
        // =========================
        float horiz = 0f;
        float vert = 0f;
        // Horizontal Movement
        if(allowedToMove) {
            horiz = Input.GetAxis("Horizontal");
            vert = Input.GetAxis("Vertical");
            Vector3 inputVector = cachedTransform.right * horiz + cachedTransform.forward * vert;

            float targetHeadHeight = isCrouched ? crouchHeight : originalHeadHeight.y;
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeadHeight, crouchSpeed * Time.deltaTime);
            headTransform.localPosition = new Vector3(originalHeadHeight.x, currentHeight, originalHeadHeight.z);



            // SPRINT & SPRINT UI Section
            if(groundCheckerScript.isGrounded) {
                if(isSprinting && !isTired) {
                    transform.parent.GetComponent<PlayerHandler>().stamina.Value -= 1 * Time.deltaTime;
                }
                else {
                    transform.parent.GetComponent<PlayerHandler>().stamina.Value = Mathf.Clamp(transform.parent.GetComponent<PlayerHandler>().stamina.Value += staminaRecoveryRate * Time.deltaTime, 0, staminaDuration);
                }

                if(transform.parent.GetComponent<PlayerHandler>().stamina.Value <= 0) {
                    source.PlayOneShot(breathClip);
                }
                if(transform.parent.GetComponent<PlayerHandler>().stamina.Value == staminaDuration) {
                    isTired = false;
                }

                StaminaUpdate();
                if((Input.GetKey(KeyCode.W) && groundCheckerScript.isGrounded && Input.GetKey(KeyCode.LeftShift) && !isTired && allowedToMove && !isCrouched) || sliding) {
                    isSprinting = true;
                    sprintActualMultiplier = sprintMultiplier;
                }
                else if((isTired || groundCheckerScript.isGrounded) || (!Input.GetKey(KeyCode.W) || !Input.GetKey(KeyCode.LeftShift))) // or is Grounded (we don't want to disable sprinting 
                {
                    isSprinting = false;
                    sprintActualMultiplier = 1;
                }

                if(!sliding && allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey)) && !isSprinting && !afterlife) {
                    Crouch(true);
                }
                // Start Slide
                else if(!sliding && allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey))
                    && isSprinting && groundCheckerScript.isGrounded && !slideOnCooldown && !afterlife) {

                    sliding = true;
                    slideDirection = cachedTransform.forward;
                    horizontalVelocity = slideDirection * slideSpeed * speed;
                    Crouch(false);
                    playerAnimator.SetTrigger("Slide");
                    armsAnimator.SetTrigger("Slide");
                    allowedToCrouch = false;
                    SlideEffectsServerRpc();
                    transform.parent.GetComponent<PlayerHandler>().stamina.Value -= staminaConsumtionOnSlide;
                    if(transform.parent.GetComponent<PlayerHandler>().stamina.Value < 0) transform.parent.GetComponent<PlayerHandler>().stamina.Value = 0;
                }
                // Sliding
                if(sliding) {
                    // Gradually lose momentum while sliding.
                    Vector3 groundNormal = groundCheckerScript.GroundNormal;
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    float slopeDot = Vector3.Dot(slideDirection, slopeDirection);

                    float currentSlideDeceleration = slideDeceleration - (slopeDot * slopeEffect);

                    horizontalVelocity = Vector3.MoveTowards(
                        horizontalVelocity,
                        Vector3.zero,
                        currentSlideDeceleration * Time.deltaTime
                    );

                    // End the slide once momentum becomes sufficiently low.
                    if(horizontalVelocity.magnitude <= slideEndSpeed) {
                        //horizontalVelocity = Vector3.zero;
                        sliding = false;
                        Crouch(false);
                        allowedToCrouch = true;
                        StopSlideEffectsServerRpc();
                    }
                }
                else {
                    // Normal movement on the ground.
                    Vector3 targetHorizontalVelocity =
                    inputVector * sprintActualMultiplier * speed;

                    horizontalVelocity = Vector3.MoveTowards(
                        horizontalVelocity,
                        targetHorizontalVelocity,
                        groundAcceleration * Time.deltaTime
                    );
                }
            }
            else {
                // Normal movement in the air.
                Vector3 targetHorizontalVelocity =
                    inputVector * sprintActualMultiplier * speed;

                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetHorizontalVelocity,
                    airAcceleration * Time.deltaTime
                );
            }
            

           
        }

        // Wall contact is evaluated before this frame's controller move so it can affect movement immediately.
        bool hasGroundContact = groundCheckerScript.isGrounded || controller.isGrounded;
        if(hasGroundContact) {
            isWallSticking = false;
            wallSlideUsedForCurrentContact = false;
            wallStickTimer = 0f;
        }
        else {
            bool hasWallContact = TryFindWall(out Vector3 detectedWallNormal, out bool detectedWallIsSide, out bool detectedWallIsRightSide);

            if(hasWallContact) {
                currentWallNormal = detectedWallNormal;
                currentWallIsSide = detectedWallIsSide;
                currentWallIsRightSide = detectedWallIsRightSide;

                if(!wallSlideUsedForCurrentContact && !isWallSticking) {
                    isWallSticking = true;
                    wallSlideUsedForCurrentContact = true;
                    wallStickTimer = wallSlideDuration;

                    PlayerHandler cameraOwner = transform.parent.GetComponent<PlayerHandler>();
                    if(cameraOwner != null && cameraOwner.cameraReference != null) {
                        if(!currentWallIsSide) cameraOwner.cameraReference.PlayWallGrabFrontImpulse();
                        else if(currentWallIsRightSide) cameraOwner.cameraReference.PlayWallGrabRightImpulse();
                        else cameraOwner.cameraReference.PlayWallGrabLeftImpulse();
                    }
                }
            }
            else {
                isWallSticking = false;
                wallSlideUsedForCurrentContact = false;
            }
        }

        if(isWallSticking) {
            if(!currentWallIsSide) {
                playerAnimator.SetBool("WallHangFront", true);
                playerAnimator.SetBool("WallRunningRight", false);
                playerAnimator.SetBool("WallRunningLeft", false);

                armsAnimator.SetBool("WallHangFront", true);
                armsAnimator.SetBool("WallRunningRight", false);
                armsAnimator.SetBool("WallRunningLeft", false);
            }
            else if(currentWallIsRightSide) {
                playerAnimator.SetBool("WallHangFront", false);
                playerAnimator.SetBool("WallRunningRight", true);
                playerAnimator.SetBool("WallRunningLeft", false);

                armsAnimator.SetBool("WallHangFront", false);
                armsAnimator.SetBool("WallRunningRight", true);
                armsAnimator.SetBool("WallRunningLeft", false);
            }
            else {
                playerAnimator.SetBool("WallHangFront", false);
                playerAnimator.SetBool("WallRunningLeft", true);
                playerAnimator.SetBool("WallRunningRight", false);

                armsAnimator.SetBool("WallHangFront", false);
                armsAnimator.SetBool("WallRunningLeft", true);
                armsAnimator.SetBool("WallRunningRight", false);
            }
        }
        else {
            playerAnimator.SetBool("WallHangFront", false);
            playerAnimator.SetBool("WallRunningLeft", false);
            playerAnimator.SetBool("WallRunningRight", false);

            armsAnimator.SetBool("WallHangFront", false);
            armsAnimator.SetBool("WallRunningLeft", false);
            armsAnimator.SetBool("WallRunningRight", false);
        }
        if(Input.GetButtonDown("Jump") && allowedToMove && hasGroundContact) {
            isWallSticking = false;
            wallSlideUsedForCurrentContact = false;
            Jump();
        }
        else if(isWallSticking && Input.GetButtonDown("Jump") && allowedToMove) {
            StartWallHop();
            groundCheck.GetComponent<GroundChecker>().ResetAirTime();
        }

        if(isWallSticking) {
            wallStickTimer -= Time.deltaTime;
            if(wallStickTimer <= 0f) {
                isWallSticking = false;
            }
            else {
                // Keep existing along-wall momentum, but prevent air input from driving into the wall.
                horizontalVelocity = Vector3.ProjectOnPlane(horizontalVelocity, currentWallNormal);
                // Preserve jump ascent; apply the controlled wall slide only after upward momentum ends.
                if(verticalVelocity.y <= 0f) verticalVelocity.y = -wallSlideSpeed;
            }
        }

        // Grounding
        if(hasGroundContact && verticalVelocity.y < 0f) {
            verticalVelocity.y = -groundedStickForce;
        }

        // Gravity
        if(!isWallSticking || verticalVelocity.y > 0f) {
            verticalVelocity.y += gravityStrength * afterlifeFallAugment * Time.deltaTime;
        }

        // Final Movement
        controller.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);

        // Animations
        playerAnimator.SetFloat("Vertical", vert);
        playerAnimator.SetFloat("Horizontal", horiz);
        playerAnimator.SetBool("Sprinting", isSprinting);
        armsAnimator.SetBool("Sprinting", isSprinting);
        playerAnimator.SetBool("Crouching", isCrouched);
       // playerAnimator.SetBool("Sliding", sliding);
        playerAnimator.SetBool("Walking", horiz != 0 || vert != 0);
    }

    // Called from inside this.Update();
    // Any changes here must be mirrored in the UIManager version.
    private void StaminaUpdate() {
        if(isSprinting && !isTired && !afterlife) playerHandlerScript.stamina.Value -= 1 * Time.deltaTime;
        else playerHandlerScript.stamina.Value = Mathf.Clamp(playerHandlerScript.stamina.Value += staminaRecoveryRate * Time.deltaTime, 0, staminaDuration);
        
        if(playerHandlerScript.stamina.Value <= 0) {
            isTired = true;
            source.PlayOneShot(breathClip);
        }
        if(playerHandlerScript.stamina.Value == staminaDuration) {
            isTired = false;
        }
    }


    [ServerRpc]
    private void SlideEffectsServerRpc() {
        SlideEffectsClientRpc();
    }

    [ClientRpc]
    private void SlideEffectsClientRpc() {
        Debug.Log("PLAYING DAMN FEATHERS.");
        feathersVFXA.Play();
        feathersVFXB.Play();
        source.PlayOneShot(slideClip);
    }

    [ServerRpc]
    private void StopSlideEffectsServerRpc() {
        StopEffectsClientRpc();
    }


    [ClientRpc]
    private void StopEffectsClientRpc() {
        feathersVFXA.Stop();
        feathersVFXB.Stop();
    }

    private IEnumerator SlideCooldown() {
        slideOnCooldown = true;
        yield return new WaitForSeconds(slideCooldown);
        slideOnCooldown = false;
    }

}
