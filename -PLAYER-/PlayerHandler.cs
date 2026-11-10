using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerHandler : NetworkBehaviour {

    public PlayerMovement playerMovementScript;
    private ToolController toolControllerScript;
    private Enemy ghostScript;

    public NetworkVariable<float> stamina = new NetworkVariable<float>(40, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public Transform cameraHolder;

    [SerializeField] private SkinnedMeshRenderer[] bodySkinnedRenderers;
    [SerializeField] private MeshRenderer[] bodyMeshRenderers;
    [SerializeField] private Animator animatorRef;
    [SerializeField] private SpellStellae flashlightScript;

    // Both the toolbelt and the main camera script need to match the camera's rotation. Tools for the obv.
    // Camera for the reason of mimicing the position of the cam's sphere child.
    [SerializeField] private MatchRotation rotationMatchingScriptCamhead, rotationMatchingScriptToolbelt;
    public CameraFollow cameraReference;
    public ParticleSystem channelParticles, spawnParticles;
    [HideInInspector] public bool spawnParticlesPlaying = false;
    private float spawnParticlesStartTime;
    [SerializeField] private float spawnParticlesGracePeriod = 0.15f;
    private Coroutine sceneBindingRoutine;

    public override void OnNetworkSpawn() {
        // Whether owner or not, turn on the spawning particles (beam of light).
        TurnOnSpawnParticlesServerRpc();

        if(!IsOwner) {
            rotationMatchingScriptCamhead.enabled = false;
            rotationMatchingScriptToolbelt.enabled = false;
            return;
        }
        Debug.Log("Player OnNetworkSpawn");

        SceneManager.sceneLoaded += OnSceneLoaded;
        BeginSceneBinding(SceneManager.GetActiveScene());
    }

    public override void OnNetworkDespawn() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if(sceneBindingRoutine != null) {
            StopCoroutine(sceneBindingRoutine);
            sceneBindingRoutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if(IsOwner) BeginSceneBinding(scene);
    }

    private void BeginSceneBinding(Scene scene) {
        if(sceneBindingRoutine != null) StopCoroutine(sceneBindingRoutine);
        cameraReference = null;
        sceneBindingRoutine = StartCoroutine(BindSceneReferences(scene));
    }

    public void SetSpawnPosition(Vector3 position) {
        CharacterController controller = GetComponent<CharacterController>();

        controller.enabled = false;

        transform.position = position;

        controller.enabled = true;
    }

    [ClientRpc]
    public void SetSpawnPositionClientRpc( Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default) {
        GetComponent<CharacterController>().enabled = false;
        //transform.SetPositionAndRotation(position, rotation);
        transform.position = position;
        GetComponent<CharacterController>().enabled = true;
    }

    private void Update() {
        if(!spawnParticlesPlaying)
            return;

        // Allow the networked transform and ground checker to catch up.
        if(Time.time - spawnParticlesStartTime < spawnParticlesGracePeriod)
            return;

        if(GetComponentInChildren<GroundChecker>().isGrounded) {
            spawnParticles.Stop();
            spawnParticlesPlaying = false;
        }
    }

    [ServerRpc]
    public void TurnOnSpawnParticlesServerRpc() {
        Debug.Log("Spawn Particle server.");
        TurnOnSpawnParticlesClientRpc();
    }

    [ClientRpc]
    public void TurnOnSpawnParticlesClientRpc() {
        Debug.Log("Spawn Particle recieved.");
        if(!IsOwner) {
            Debug.Log("Spawn Particle recieved and working.");
            spawnParticles.Play();
            spawnParticlesPlaying = true;
            spawnParticlesStartTime = Time.time;
        }
    }



    private IEnumerator BindSceneReferences(Scene scene) {
        CameraFollow sceneCamera = null;
        float elapsed = 0f;
        while(sceneCamera == null && scene.IsValid() && scene.isLoaded && elapsed < 5f) {
            CameraFollow[] cameras = FindObjectsOfType<CameraFollow>(true);
            foreach(CameraFollow candidate in cameras) {
                if(candidate.gameObject.scene == scene) {
                    sceneCamera = candidate;
                    break;
                }
            }
            if(sceneCamera == null) {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if(sceneCamera == null) {
            Debug.LogWarning($"No CameraFollow found in loaded scene '{scene.name}' for local player.");
            sceneBindingRoutine = null;
            yield break;
        }

        cameraReference = sceneCamera;

        cameraReference.SetTarget(cameraHolder);
        cameraReference.GetComponent<PingCreator>().playerScript = playerMovementScript;
        cameraReference.transform.GetChild(3).GetComponent<HeadBob>().playerMovement = playerMovementScript;
        cameraReference.GetComponent<MouseLook>().playerBody = playerMovementScript.transform.parent;
        cameraReference.GetComponent<MouseLook>().cameraAnimator = animatorRef;

        // Head Look Tracking
        GetComponent<PlayerVisualLook>().SetLocalMouseLook(
            cameraReference.GetComponent<MouseLook>()
        );

        rotationMatchingScriptCamhead.goFollow = cameraReference.gameObject;
        rotationMatchingScriptToolbelt.goFollow = cameraReference.gameObject;

        ghostScript = null;
        elapsed = 0f;
        string hubSceneName = MultiplayerManager.Instance != null ? MultiplayerManager.Instance.hubSceneName : "Hub 1";
        while(scene.name != hubSceneName && ghostScript == null && elapsed < 5f && scene.IsValid() && scene.isLoaded) {
            Enemy[] enemies = FindObjectsOfType<Enemy>(true);
            foreach(Enemy candidate in enemies) {
                if(candidate.gameObject.scene == scene) {
                    ghostScript = candidate;
                    break;
                }
            }
            if(ghostScript == null) {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if(ghostScript != null) {
            playerMovementScript.enemyVisionScript = ghostScript.GetComponent<ConeLOSDetector>();
            if(playerMovementScript.enemyVisionScript != null)
                playerMovementScript.enemyVisionScript.AddTarget(playerMovementScript.transform);
            ghostScript.GetComponent<GhostRandomizer>().deathScript = GetComponent<Death>();
        }
        Cursor.lockState = CursorLockMode.Locked;

        foreach(SkinnedMeshRenderer bodyRenderer in bodySkinnedRenderers) {
            bodyRenderer.enabled = false;
        }
        foreach(MeshRenderer meshRenderObj in bodyMeshRenderers) {
            meshRenderObj.enabled = false;
        }

        flashlightScript.raycastScript = cameraReference.GetComponent<InteractRaycast>();
        sceneBindingRoutine = null;
    }
}

