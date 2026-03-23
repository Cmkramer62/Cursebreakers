using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class PlayerMovement : MonoBehaviour {
    
    public GroundChecker groundCheckerScript;

    [SerializeField]
    private float speed = 12f, OGspeed, HFspeed, gravity = -9.81f, jumpHeight = 3f, pitchValue = 1f, sprintMultiplier = 2f,
        sprintRemaining = 40, sprintRecoveryDec = 1f, sprintDuration = 5f, crouchHeight = 0.75f, currentHeight = .75f;

    private float sprintActualMultiplier = 1f;

    [SerializeField]
    private bool shouldBeSlowed = false, isTired = false, useSprintBar = true, hideBarWhenFull = true;
    public bool allowedToMove = true, allowedToCrouch = true, isSprinting = false, isCrouched = false, isHiding = false;

    [SerializeField]
    public Image sprintBarBG, sprintBar;
    public CharacterController controller;
    public bool lockCursor = true;
    public Transform groundCheck;
    public float groundDistance = 0.4f, amountCrouchSpots = 0f;
    public LayerMask groundMask;
    public KeyCode crouchKey = KeyCode.LeftControl;


    public CanvasGroup sprintBarCG;

    public ConeLOSDetector enemyVisionScript;
    #region SOUND VARIABLES
        public AudioSource source;
        public AudioClip breathClip, crouchClip;
        public AudioClip[] jumpClip;
    #endregion

    public LightFlicker lanternReference;

    private Vector3 fallingVelocity, originalScale;
    private Color stamBarUIColor;
    private Transform cachedTransform;

    private void Awake() {
        //sprintRemaining = sprintDuration;
        cachedTransform = GetComponent<Transform>();
        originalScale = cachedTransform.localScale;
        stamBarUIColor = sprintBar.color;
    }

    private void Start() {
        
        OGspeed = speed;
        HFspeed = speed / 2;
        if(lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void ResetVarsToDefaults() {
        allowedToMove = true;
        allowedToCrouch = true;

        if(isCrouched) Crouch();   
    }

    public void Crouch() {
        source.PlayOneShot(crouchClip);
        speed = isCrouched ? OGspeed : HFspeed;
        currentHeight = isCrouched ? crouchHeight : originalScale.y;
        enemyVisionScript.fieldOfViewAngle += isCrouched ? 30 : -30;
        isCrouched = !isCrouched;
    }

    private void Jump() {
        fallingVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        source.PlayOneShot(jumpClip[Random.Range(0, jumpClip.Length)]);
    }

    public bool TiredState() {
        return isTired;
    }

    public float GetRemainingStam() {
        return sprintRemaining / sprintDuration;
    }

    void Update() {
        // MOVEMENT Section
        Vector3 inputVector = cachedTransform.right * Input.GetAxis("Horizontal") + cachedTransform.forward * Input.GetAxis("Vertical");
        if(inputVector.magnitude > 1)
            inputVector.Normalize();
        if(allowedToMove)
            controller.Move(inputVector * sprintActualMultiplier * speed * Time.deltaTime);

        // CROUCH Section
        cachedTransform.localScale = new Vector3(originalScale.x, Mathf.Clamp(currentHeight -= (isCrouched ? 2f : -2f) * Time.deltaTime, crouchHeight, originalScale.y), originalScale.z);

        if(allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey)) ) {
            isCrouched = !Input.GetKeyDown(crouchKey);
            Crouch();
        }
       
        // JUMP Section
        if(groundCheckerScript.isGrounded && fallingVelocity.y < 0)
            fallingVelocity.y = -2f;

        if(Input.GetButtonDown("Jump") && allowedToMove && groundCheckerScript.isGrounded)
            Jump();
        
        fallingVelocity.y += gravity * Time.deltaTime;

        if(allowedToMove)
            controller.Move(fallingVelocity * Time.deltaTime);
        

        // SPRINT & SPRINT UI Section
        if(isSprinting && !isTired) {
            sprintRemaining -= 1 * Time.deltaTime;
            if(hideBarWhenFull && useSprintBar) { sprintBarCG.alpha += 5 * Time.deltaTime; }
        }
        else {
            sprintRemaining = Mathf.Clamp(sprintRemaining += sprintRecoveryDec * Time.deltaTime, 0, sprintDuration);
        }

        if(useSprintBar) sprintBar.rectTransform.sizeDelta = new Vector2(sprintRemaining / sprintDuration * 175, sprintBar.rectTransform.sizeDelta.y);

        if(sprintRemaining <= 0) {
            isTired = true;
            sprintBar.color = Color.red;
            source.PlayOneShot(breathClip);
        }
        if(sprintRemaining == sprintDuration) {
            isTired = false;
            if(hideBarWhenFull && useSprintBar) sprintBarCG.alpha -= 3 * Time.deltaTime;
            if(useSprintBar) sprintBar.color = stamBarUIColor;
        }

        if((Input.GetKey(KeyCode.W)) && groundCheckerScript.isGrounded && Input.GetKey(KeyCode.LeftShift) && useSprintBar && !isTired && allowedToMove && !isCrouched) {
            isSprinting = true;
            sprintActualMultiplier = sprintMultiplier;
        }
        else if((isTired || groundCheckerScript.isGrounded) || (!Input.GetKey(KeyCode.W) || !Input.GetKey(KeyCode.LeftShift))) // or is Grounded (we don't want to disable sprinting 
        {
            isSprinting = false;
            sprintActualMultiplier = 1;
        }

    }



}
