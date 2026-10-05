using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour {
    public Transform groundCheck;
    public float groundDistance = 0.4f, groundCheckRadius = 0.5f, footstepVolume;
    public LayerMask groundMask;
    public bool isGrounded = false, inAirFromJump = false;

    [SerializeField] private Animator playerAnimator, armsAnimator;

    public string currentTag = "Grass";

    public AudioSource footSource;
    [SerializeField] private AudioClip[]
        tileStepClips, tileStepEchoClips,
        tileDirtyStepClips, tileDirtyStepEchoClips,
        woodStepClips, woodStepEchoClips,
        waterStepClips, waterStepEchoClips, 
        carpetStepClips, afterlifeStepClips;

    public bool afterlife = false;

    public AudioSource windSource;
    public AudioClip[] crashingSounds;
    public float minimumAirTimeForAudio = 2f, crashTimeMultiplier = 1f;
    private float airTime = 0f;
    private float maximumFallSpeed;
    private bool hasCheckedGround;
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    private PlayerMovement movementScript;

    /*
     * Goal is to check the ground beneath the user.
     * Change sound of footsteps based on material
     */
    //Types: Concrete, Metal, Wood, Snow, Vent, Water, Tile

    void Start() {
        if(!gameObject.transform.parent.parent.GetComponent<PlayerHandler>().IsOwner) {
            enabled = false;
            return;
        }

        movementScript = transform.parent.GetComponent<PlayerMovement>();
    }

    private AudioClip GetRandomClip(AudioClip[] footstepList) {
        int Index = Random.Range(0, footstepList.Length);
        return footstepList[Index];
    }

    // Reset the tracker for how long the player has been in the air.
    // Called by Movement, when wall-hopping.
    public void ResetAirTime() {
        airTime = 0;
        maximumFallSpeed = 0f;
        windSource.volume = 0f;
    }

    // Update is called once per frame
    void Update() {
        RaycastHit hit;
        bool priorState = isGrounded;

        isGrounded = Physics.SphereCast(
            groundCheck.position,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundDistance,
            groundMask
        );
        bool justLanded = hasCheckedGround && !priorState && isGrounded;
        hasCheckedGround = true;

        // Ground Normal is the angle of the surface. Movement uses this for sliding.
        if(isGrounded) {
            GroundNormal = hit.normal;
        }
        else {
            GroundNormal = Vector3.up;
        }

        playerAnimator.SetBool("Grounded", isGrounded);
        armsAnimator.SetBool("Grounded", isGrounded);

        // ==========================================
        // AIRBORNE SFX
        // ==========================================

        if(!isGrounded && !movementScript.isWallSticking) {
            airTime += Time.deltaTime;
            maximumFallSpeed = Mathf.Max(maximumFallSpeed, -movementScript.GetCurrentVelocity().y);

            // Begin fading in wind after 2 seconds
            if(airTime > minimumAirTimeForAudio) {
                float windLerp = Mathf.InverseLerp(minimumAirTimeForAudio, 4f, airTime);
                windSource.volume = Mathf.Lerp(0f, 2f, windLerp);
            }
        }


        // ==========================================
        // LANDING SFX
        // ==========================================

        if(justLanded) {
            currentTag = hit.collider.tag;

            playerAnimator.SetBool("InAirFromJump", false);
            if(airTime > minimumAirTimeForAudio) {
                playerAnimator.SetInteger("FallImpact", 2);
            }
            else if(airTime > (minimumAirTimeForAudio / 2)) {
                playerAnimator.SetInteger("FallImpact", 1);
            }
            else {
                playerAnimator.SetInteger("FallImpact", 0);
            }

            // ==========================================
            // CRASH SOUND
            // ==========================================
            int crashIndex = 0;

            if(airTime > minimumAirTimeForAudio && crashingSounds.Length > 0) {
                float crashVolume = Mathf.InverseLerp(minimumAirTimeForAudio, 8f, airTime * crashTimeMultiplier);

                crashIndex = Mathf.Clamp(
                    Mathf.FloorToInt(crashVolume * crashingSounds.Length),
                    0,
                    crashingSounds.Length - 1
                );

                footSource.PlayOneShot(
                    crashingSounds[crashIndex],
                    crashVolume
                );
            }

            PlayerHandler cameraOwner = transform.parent.parent.GetComponent<PlayerHandler>();
            if(cameraOwner != null && cameraOwner.cameraReference != null) {
                float impactSpeed = Mathf.Max(maximumFallSpeed, -movementScript.GetCurrentVelocity().y);

                if(crashIndex != crashingSounds.Length - 1) {
                    cameraOwner.cameraReference.PlayLandingImpulse(impactSpeed);
                }
                else {
                    cameraOwner.cameraReference.PlayGreatLandingImpulse();
                }
            }

            // Reset wind
            windSource.volume = 0f;

            // Reset airtime
            airTime = 0f;
            maximumFallSpeed = 0f;
        }


        // ==========================================
        // GROUND TAG AND GROUND SFX
        // ==========================================

        if(isGrounded) {
            currentTag = hit.collider.tag;
            //playingFromClips = AssignList();
        }
    }

    public void UpdateClips() {
        //playingFromClips = AssignList();
    }

    public void PlaySound() {
        footSource.pitch = (Random.Range(0.87f, 0.93f));
        //AudioClip clip = GetRandomClip(playingFromClips);
        AudioClip clip = GetRandomClip(AssignList());
        footSource.PlayOneShot(clip, footstepVolume);
    }

    // AssignList returns an array of AudioClips based on currentTag's value.
    // CurrentTag must already be assigned to return the proper value.
    private AudioClip[] AssignList() {
        if(afterlife) return afterlifeStepClips;

        footstepVolume = currentTag.Contains("Echo") ? 2 : 1;

        if(currentTag == "Tile") return tileStepClips;
        else if(currentTag == "TileEcho") return tileStepEchoClips;
        else if(currentTag == "TileDirty") return tileDirtyStepClips;
        else if(currentTag == "TileDirtyEcho") return tileDirtyStepEchoClips;
        else if(currentTag == "Wood") return woodStepClips;
        else if(currentTag == "WoodEcho") return woodStepEchoClips;
        else if(currentTag == "Water") return waterStepClips;
        else if(currentTag == "WaterEcho") return waterStepEchoClips;
        else return tileStepClips; // This default can be different for each level.
    }
}
