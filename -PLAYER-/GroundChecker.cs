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
    public AudioClip[] normalStepClips, metalStepClips, woodStepClips, ventStepClips, waterStepClips, tileStepClips, carpetStepClips, rockStepClips;

    public AudioClip[] normalLandClips, metalLandClips, woodLandClips, ventLandClips, waterLandClips, tileLandClips, carpetLandClips, rockLandClips;
    public AudioClip[] afterlifeStepClips;

    public bool afterlife = false;
    public AudioClip[] playingFromClips;

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
        playingFromClips = ventStepClips;
    }

    private AudioClip GetRandomClip(AudioClip[] footstepList) {
        int Index = Random.Range(0, footstepList.Length);
        return footstepList[Index];
    }

    // Update is called once per frame
    void Update() {
        //isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

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
        // AIRBORNE
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
        // LANDING
        // ==========================================

        if(!priorState && isGrounded) {
            currentTag = hit.collider.tag;

            // Landing sound
            /*
            if(!footSource.isPlaying) {
                AudioClip[] playingLandingClips = AssignList(false);

                footSource.pitch = Random.Range(0.87f, 0.93f);

                AudioClip clip = GetRandomClip(playingLandingClips);

                footSource.PlayOneShot(clip, footstepVolume);
            }
            */
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
        // GROUND TAG
        // ==========================================

        if(isGrounded && !hit.collider.CompareTag(currentTag)) {
            currentTag = hit.collider.tag;

            playingFromClips = AssignList(true);
        }
    }

    public void UpdateClips() {
        playingFromClips = AssignList(true);
    }

    public void PlaySound() {
        footSource.pitch = (Random.Range(0.87f, 0.93f)); //(Random.Range(0.78f, 0.87f));
        AudioClip clip = GetRandomClip(playingFromClips);
        footSource.PlayOneShot(clip, footstepVolume);
        //footSource.pitch = 1f;
    }

    private AudioClip[] AssignList(bool walking) {
        footstepVolume = currentTag == "Vent" ? 2 : 1;
        if(afterlife) return afterlifeStepClips;

        if(currentTag == "Metal") return walking ? metalStepClips : metalLandClips;
        else if(currentTag == "Wood") return walking ? woodStepClips : woodLandClips;
        else if(currentTag == "Vent") return walking ? ventStepClips : ventLandClips;
        else if(currentTag == ("Water")) return walking ? waterStepClips : waterLandClips;
        else if(currentTag == ("Tile")) return walking ? tileStepClips : tileLandClips;
        else if(currentTag == ("Carpet")) return walking ? carpetStepClips : carpetLandClips;
        else if(currentTag == ("Rock")) return walking ? rockStepClips : rockLandClips;
        else return walking ? normalStepClips : normalLandClips;
    }
}
