using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour {
    public Transform groundCheck;
    public float groundDistance = 0.4f, footstepVolume;
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
    }

    private AudioClip GetRandomClip(AudioClip[] footstepList) {
        int Index = Random.Range(0, footstepList.Length);
        return footstepList[Index];
    }

    // Update is called once per frame
    void Update() {
        RaycastHit hit;
        bool priorState = isGrounded;

        isGrounded = Physics.Raycast(
            groundCheck.position,
            Vector3.down,
            out hit,
            groundDistance,
            groundMask
        );

        playerAnimator.SetBool("Grounded", isGrounded);
        armsAnimator.SetBool("Grounded", isGrounded);

        // ==========================================
        // AIRBORNE SFX
        // ==========================================

        if(!isGrounded) {
            airTime += Time.deltaTime;

            // Begin fading in wind after 2 seconds
            if(airTime > minimumAirTimeForAudio) {
                float windLerp = Mathf.InverseLerp(minimumAirTimeForAudio, 4f, airTime);
                windSource.volume = Mathf.Lerp(0f, 2f, windLerp);
            }
        }


        // ==========================================
        // LANDING SFX
        // ==========================================

        if(!priorState && isGrounded) {
            currentTag = hit.collider.tag;

            playerAnimator.SetBool("InAirFromJump", false);

            // ==========================================
            // CRASH SOUND
            // ==========================================

            if(airTime > minimumAirTimeForAudio && crashingSounds.Length > 0) {
                float crashVolume = Mathf.InverseLerp(minimumAirTimeForAudio, 8f, airTime * crashTimeMultiplier);

                int crashIndex = Mathf.Clamp(
                    Mathf.FloorToInt(crashVolume * crashingSounds.Length),
                    0,
                    crashingSounds.Length - 1
                );

                footSource.PlayOneShot(
                    crashingSounds[crashIndex],
                    crashVolume
                );
            }


            // Reset wind
            windSource.volume = 0f;

            // Reset airtime
            airTime = 0f;
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
