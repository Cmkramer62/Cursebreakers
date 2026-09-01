using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour {
    
    public GroundChecker groundCheckerScript;

    public float speed = 12f, staminaRecoveryRate = 1f, staminaDuration = 40f, jumpHeight = 1, slideSpeed = 4f, slideCooldown = 100f;



    private float originalSpeed, crouchingSpeed, sprintActualMultiplier = 1f, gravity = -22f, sprintMultiplier = 3f;
    public float crouchHeight = -0.696f, currentHeight = 0f;

    [SerializeField]
    private bool shouldBeSlowed = false, sliding = false;
    public bool allowedToMove = true, allowedToCrouch = true, isSprinting = false,
        isCrouched = false, isHiding = false, isTired = false, slideOnCooldown = false, playerAlive = true;

    [SerializeField]
    public CharacterController controller;
    public bool lockCursor = true;
    public Transform groundCheck;
    public float groundDistance = 0.4f, amountCrouchSpots = 0f;
    public LayerMask groundMask;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [SerializeField] Animator playerAnimator, armsAnimator;

    public ConeLOSDetector enemyVisionScript;
    #region SOUND VARIABLES
    public AudioSource source;
    public AudioClip breathClip, crouchClip, ghostJump;
    public AudioClip[] jumpClip;
    #endregion

    public LightFlickerNonNetworked lanternReference;

    private Vector3 fallingVelocity, originalScale, originalHeadHeight;
    private Transform cachedTransform;
    [SerializeField] private PlayerHandler playerHandlerScript;
    [SerializeField] private Transform headTransform;
    private Coroutine airRoutine;

    [SerializeField] private ParticleSystem feathersVFXA, feathersVFXB;

    private float afterlifeFallAugment = 1f, afterlifeSpeedAugment = 1f, afterlifeJumpAugment = 1f;
    private bool afterlife = false;

    private void Awake() {

        //sprintRemaining = sprintDuration;
        cachedTransform = transform.parent.GetComponent<Transform>();
        originalScale = cachedTransform.localScale;
        originalHeadHeight = headTransform.localPosition;
    }

    private void Start() {

        originalSpeed = speed;
        crouchingSpeed = speed / 2;
        if(lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
        }
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

        if(isCrouched) Crouch();   
    }

    public void Crouch() {
        Debug.Log("Crouch called.");
        source.PlayOneShot(crouchClip);
        speed = isCrouched ? originalSpeed : crouchingSpeed;
        currentHeight = isCrouched ? crouchHeight : originalHeadHeight.y;
        //enemyVisionScript.fieldOfViewAngle += isCrouched ? 30 : -30;
        isCrouched = !isCrouched;
    }

    private void Jump() {
        fallingVelocity.y = Mathf.Sqrt(jumpHeight * -2f * afterlifeJumpAugment * gravity);
        source.PlayOneShot(afterlife ? ghostJump : jumpClip[Random.Range(0, jumpClip.Length)]);

        playerAnimator.SetTrigger("Jump");
        playerAnimator.SetBool("InAirFromJump", true);
        if(transform.parent.GetComponent<ToolController>().heldIndex.Value == 0) armsAnimator.SetTrigger("Jump"); // might this sometimes not be properly set?
        armsAnimator.SetBool("Grounded", false);
        if(airRoutine != null) StopCoroutine(airRoutine);
        airRoutine = StartCoroutine(InAirFromJumpTimer());
    }

    private IEnumerator InAirFromJumpTimer() {
        yield return new WaitForSeconds(1f);
        playerAnimator.SetBool("InAirFromJump", false);
    }

    public bool TiredState() { return isTired; }

    public bool SlidingState() { return sliding; }

    public float GetRemainingStam() {
        return transform.parent.GetComponent<PlayerHandler>().stamina.Value / staminaDuration;
    }

    void Update() {
        if(!IsOwner) {
            enabled = false;
            return;
        }

        if(!playerAlive) return;

        // MOVEMENT Section
        var horiz = Input.GetAxis("Horizontal");
        var vert = Input.GetAxis("Vertical");
        Vector3 inputVector = cachedTransform.right * horiz + cachedTransform.forward * vert;
        
        playerAnimator.SetFloat("Vertical", vert);
        playerAnimator.SetFloat("Horizontal", horiz);

        if(inputVector.magnitude > 1) {
            inputVector.Normalize();
        }

        playerAnimator.SetBool("Walking", horiz != 0 || vert != 0);
        if(!sliding && allowedToMove)
            controller.Move(inputVector * sprintActualMultiplier * speed * afterlifeSpeedAugment * Time.deltaTime);

        // CROUCH Section
        //cachedTransform.localScale = new Vector3(originalScale.x, Mathf.Clamp(currentHeight -= (isCrouched ? 2f : -2f) * Time.deltaTime, crouchHeight, originalScale.y), originalScale.z);
        headTransform.localPosition = new Vector3(originalHeadHeight.x, Mathf.Clamp(currentHeight -= (isCrouched ? 2f : -2f) * Time.deltaTime, crouchHeight, originalHeadHeight.y), originalHeadHeight.z);

        if(!sliding && allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey)) && !isSprinting && !afterlife) {
            isCrouched = !Input.GetKeyDown(crouchKey);
            Crouch();
        }
        else if(!sliding && allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey)) 
            && isSprinting && groundCheckerScript.isGrounded && !slideOnCooldown && !afterlife) {
            StartCoroutine(SlideRoutine());
        }

        // JUMP Section
        if(groundCheckerScript.isGrounded && fallingVelocity.y < 0)
            fallingVelocity.y = -2f;

        if(Input.GetButtonDown("Jump") && allowedToMove && groundCheckerScript.isGrounded)
            Jump();

        fallingVelocity.y += gravity * afterlifeFallAugment * Time.deltaTime;

        if(allowedToMove)
            controller.Move(fallingVelocity * Time.deltaTime);


        // SPRINT & SPRINT UI Section
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
        playerAnimator.SetBool("Sprinting", isSprinting);
        playerAnimator.SetBool("Crouching", isCrouched);

        if(sliding) {
            Vector3 inputVectorSliding = cachedTransform.right * 0 + cachedTransform.forward * 1;
            controller.Move(inputVectorSliding * slideSpeed * speed * Time.deltaTime);
        }
     
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

    private IEnumerator SlideRoutine() {
        //StartCoroutine(SlideCooldown());
        playerAnimator.SetTrigger("Slide");
        sliding = true;

        allowedToCrouch = false;
        //allowedToMove = false;
        //isCrouched = true;
        Debug.Log("PLAYING DAMN FEATHERS.");
        feathersVFXA.Play();
        feathersVFXB.Play();

        Crouch();

        yield return new WaitForSeconds(2f);
        sliding = false;
        //isCrouched = false;

        Crouch();
        feathersVFXA.Stop();
        feathersVFXB.Stop();
        allowedToCrouch = true;
        //allowedToMove = true;
        transform.parent.GetComponent<PlayerHandler>().stamina.Value = 0;
    }

    private IEnumerator SlideCooldown() {
        slideOnCooldown = true;
        yield return new WaitForSeconds(slideCooldown);
        slideOnCooldown = false;
    }

}
