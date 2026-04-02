using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour {
    public Transform groundCheck;
    public float groundDistance = 0.4f, footstepVolume;
    public LayerMask groundMask;
    public bool isGrounded = false;

    private string currentTag = "Grass";

    public AudioSource footSource;
    public AudioClip[] normalStepClips, metalStepClips, woodStepClips, ventStepClips, waterStepClips, tileStepClips, carpetStepClips, rockStepClips;

    public AudioClip[] normalLandClips, metalLandClips, woodLandClips, ventLandClips, waterLandClips, tileLandClips, carpetLandClips, rockLandClips;
    private AudioClip[] playingFromClips;
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
        playingFromClips = normalStepClips;
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
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, out hit, groundDistance, groundMask);
        if(!priorState && isGrounded) {
            currentTag = hit.collider.tag;

           // Debug.Log("Landed.");

            AudioClip[] playingLandingClips = AssignList(false);

            footSource.pitch = (Random.Range(0.87f, 0.93f)); //(Random.Range(0.78f, 0.87f));
            AudioClip clip = GetRandomClip(playingLandingClips);
            footSource.PlayOneShot(clip, footstepVolume);
        }

        if(isGrounded && !hit.collider.CompareTag(currentTag)) {
            currentTag = hit.collider.tag;

            playingFromClips = AssignList(true);
        }
    }


    public void PlaySound() {
        footSource.pitch = (Random.Range(0.87f, 0.93f)); //(Random.Range(0.78f, 0.87f));
        AudioClip clip = GetRandomClip(playingFromClips);
        footSource.PlayOneShot(clip, footstepVolume);
        //footSource.pitch = 1f;
    }

    private AudioClip[] AssignList(bool walking) {
        if(currentTag.Equals("Metal")) return walking ? metalStepClips : metalLandClips;
        else if(currentTag.Equals("Wood")) return walking ? woodStepClips : woodLandClips;
        else if(currentTag.Equals("Vent")) return walking ? ventStepClips : ventLandClips;
        else if(currentTag.Equals("Water")) return walking ? waterStepClips : waterLandClips;
        else if(currentTag.Equals("Tile")) return walking ? tileStepClips : tileLandClips;
        else if(currentTag.Equals("Carpet")) return walking ? carpetStepClips : carpetLandClips;
        else if(currentTag.Equals("Rock")) return walking ? rockStepClips : rockLandClips;
        else return walking ? normalStepClips : normalLandClips;
    }
}
