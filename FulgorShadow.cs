using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FulgorShadow : MonoBehaviour {

    private GameObject ghost;
    public Animator animator;
    private string[] animationNames = { "Waving" , "Down", "Up", "Left Center", "Left Down", "Contort", "Kneel", "Hide", "Rush", "Beg", "Up2", "Contort Down", "Right" };
    public GameObject eyes;
    public SkinnedMeshRenderer meshRen;
    public bool keepOn = false; // off by default

    public float cooldownDuration = 30f;
    private Coroutine cooldownRoutine;
    public bool onCooldown = false;

    private IEnumerator Start() {
        while(ghost == null) {
            ghost = GameObject.FindGameObjectWithTag("Ghost");
            ghost.GetComponent<GhostRandomizer>().fulgorShadow = gameObject;
            yield return null;
        }
        gameObject.SetActive(keepOn);
    }


    public void ShadowTrigger() {
        gameObject.transform.position = ghost.transform.position;

        // 2. Choose the pose
        animator.Play(animationNames[Random.Range(0, animationNames.Length)], 0, 0f);

        // 3. Force Animator to immediately evaluate it
        animator.Update(0f);
    }

    public void SetState(bool state) {
        if(onCooldown && state) {
            return;
        }

        meshRen.enabled = state;
        eyes.SetActive(state);
    }

    public void StartCooldown() {
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown() {
        onCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        onCooldown = false;
    }
}
