using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;

public class SpellFulgor : NetworkBehaviour {

    public GameObject lightFlash;
    public AudioSource source;
    public AudioClip flashClip;
    public AudioClip[] airWhooshClips;

    private bool flashOnCooldown = false;
    public float staminaRemaining = 5f, sprintDuration = 5f;
    public Image sprintBar;
    public GameObject cameraUI;

    [SerializeField] private ParticleSystem auraFlashParticles;
    private GameObject fulgorShadow;

    // Sanity Jumpscare
    public int playerSawMeCount = 0;
    private float timer = 0f;
    [SerializeField] private LayerMask targetLayer;

    public override void OnNetworkSpawn() {
        StartCoroutine(SetShadow());
    }

    private IEnumerator SetShadow() {
        yield return WaitForNetworkSceneLoad();
        if(IsOwner) {
            fulgorShadow = GameObject.FindGameObjectWithTag("FulgorShadow");

            LookAtConstraint constraint = fulgorShadow.GetComponent<LookAtConstraint>();
            List<ConstraintSource> sources = new List<ConstraintSource>();
            constraint.GetSources(sources);
            ConstraintSource newSource = new ConstraintSource();
            newSource.sourceTransform = transform;
            newSource.weight = 1.0f;
            sources.Add(newSource);
            constraint.SetSources(sources);
            constraint.enabled = true;
            constraint.constraintActive = true;

            //fulgorShadow.SetActive(false);
            fulgorShadow.GetComponent<FulgorShadow>().SetState(false);
        }

        gameObject.SetActive(false);
    }

    public void OnEnable() {
        //cameraUI.SetActive(true);
       
    }

    private void OnDisable() {
        lightFlash.SetActive(false);
        //cameraUI.SetActive(false);
        flashOnCooldown = false;
        fulgorShadow.GetComponent<FulgorShadow>().SetState(false);
    }

    // Update is called once per frame
    void Update() {
        if(!IsOwner) return;

        if(Input.GetKeyDown(KeyCode.F) && !flashOnCooldown) {
            TakePictureServerRpc();
            
            StartCoroutine(Cooldown());
        }

        timer += Time.deltaTime;

        if(timer >= 15f) {
            timer = 0f;
            if(playerSawMeCount > 0) {
                playerSawMeCount--;
            }
        }
    }

    [ServerRpc]
    void TakePictureServerRpc() {
        TakePictureClientRpc();
        if(fulgorShadow.activeInHierarchy) StartCoroutine(FlashPersonalEffects());
    }

    [ClientRpc]
    void TakePictureClientRpc() {
        StartCoroutine(FlashVisualEffects());
        transform.parent.parent.parent.GetComponent<ToolController>().CameraAnimation();
        auraFlashParticles.Play();
        source.PlayOneShot(airWhooshClips[Random.Range(0, airWhooshClips.Length)]);
    }

    private IEnumerator FlashVisualEffects() {
        source.PlayOneShot(flashClip, Random.Range(0.85f, 1f));
        staminaRemaining = 0;

        lightFlash.SetActive(true);
        TriggerCurse(true);
        yield return new WaitForSeconds(.05f);

        lightFlash.SetActive(false);
        yield return new WaitForSeconds(.05f);

        lightFlash.SetActive(true);
        yield return new WaitForSeconds(.05f);

        lightFlash.SetActive(false);
    }

    // Fulgor shadow should be on a light layer that is only shone by the fulgor light.
    // This layer everyone can see. In which case, this is not needed, and ppl other than
    // Caster can see the fulgor shadow. But for now, only the caster can see.
    private IEnumerator FlashPersonalEffects() {
        // Check if the player saw me.
        Vector3 origin = transform.position;
        Vector3 direction = (fulgorShadow.transform.position - origin).normalized;
        float angle = Vector3.Angle(transform.forward, direction);

        if(angle <= 35f) {
            if(Physics.Raycast(origin, direction, out RaycastHit hit, 100f, targetLayer)) {
                // Hit something on targetLayer
                if(!fulgorShadow.GetComponent<FulgorShadow>().onCooldown && hit.collider.CompareTag("FulgorShadow")) {
                    playerSawMeCount++;

                    if(playerSawMeCount >= Random.Range(3, 6)) {
                        playerSawMeCount = 0;
                        // fulgor shadow is not seen this time, out in the world.
                        fulgorShadow.GetComponent<FulgorShadow>().StartCooldown();
                        GetComponentInParent<Death>().JumpscareFulgor();
                    }
                }

            }

        }


        fulgorShadow.GetComponent<FulgorShadow>().ShadowTrigger();
        fulgorShadow.GetComponent<FulgorShadow>().SetState(true);
        yield return new WaitForSeconds(.05f);

        fulgorShadow.GetComponent<FulgorShadow>().SetState(false);
        yield return new WaitForSeconds(.05f);

        fulgorShadow.GetComponent<FulgorShadow>().SetState(true);
        yield return new WaitForSeconds(.05f);

        fulgorShadow.GetComponent<FulgorShadow>().SetState(false);
    }

    private void TriggerCurse(bool state) {
        foreach(CursedObject objectee in gameObject.transform.parent.parent.parent.GetComponent<ToolController>().cursedObjectsWithinRange) {
            objectee.DisplayCurse(CursedObject.CurseType.FulgorTrait, state);
        }
    }

    private IEnumerator Cooldown() {
        flashOnCooldown = true;
        yield return new WaitForSeconds(sprintDuration);
        flashOnCooldown = false;
    }

    public void CameraUIUpdate() {
        staminaRemaining = Mathf.Clamp(staminaRemaining += 1f * Time.deltaTime, 0, sprintDuration);
        float sprintRemainingPercent = staminaRemaining / sprintDuration;
        //sprintBar.rectTransform.sizeDelta = new Vector2(sprintRemainingPercent * 175, sprintBar.rectTransform.sizeDelta.y);
    }

    private IEnumerator WaitForNetworkSceneLoad() {
        if(!IsServer && SceneManager.GetActiveScene().isLoaded)
            yield break;

        bool sceneLoaded = false;

        void OnSceneEvent(SceneEvent sceneEvent) {
            if(sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted) {
                sceneLoaded = true;
            }
        }

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;

        while(!sceneLoaded) {
            yield return null;
        }

        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
    }
}
