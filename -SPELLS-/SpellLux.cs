using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SpellLux : NetworkBehaviour {

    public GameObject spawnedLightInstance;
    public GameObject lightPrefab;
    private bool previouslySpawnedVFX = false;
    public Transform magicFollowPoint;
    public GameObject stellaeSpell;

    public override void OnNetworkSpawn() {
        StartCoroutine(DelayedSpawning());
        // Lerp increase light
    }

    private IEnumerator DelayedSpawning() {

        yield return WaitForNetworkSceneLoad();

        if(!previouslySpawnedVFX) {
            previouslySpawnedVFX = true;

            spawnedLightInstance = GameObject.Instantiate(lightPrefab);
            gameObject.transform.parent.GetComponentInChildren<SpellStellae>().spawnedLightInstance = spawnedLightInstance;
            spawnedLightInstance.GetComponent<DelayFollow>().target = magicFollowPoint;

            spawnedLightInstance.transform.GetChild(0).gameObject.SetActive(false);
            spawnedLightInstance.transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
            spawnedLightInstance.transform.GetChild(2).GetComponent<ParticleSystem>().Play();
        }

        gameObject.SetActive(false);
    }


    private void OnEnable() {
        if(spawnedLightInstance != null) {
            spawnedLightInstance.transform.GetChild(0).gameObject.SetActive(true);
            spawnedLightInstance.transform.GetChild(1).GetComponent<ParticleSystem>().Play();
            //particles.GetComponent<DelayFollow>().enabled = true; always is true
        }
        // play expand anim on sphere?
    }

    private void OnDisable() {
        if(spawnedLightInstance != null && !stellaeSpell.activeSelf) {
            spawnedLightInstance.transform.GetChild(0).gameObject.SetActive(false);
            spawnedLightInstance.transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
            spawnedLightInstance.transform.GetChild(2).GetComponent<ParticleSystem>().Play();
        }
        // play shrink anim on sphere?
    }

    private IEnumerator WaitForNetworkSceneLoad() {
        MultiplayerManager multiplayerManager = MultiplayerManager.Instance;
        string hubSceneName = multiplayerManager != null ? multiplayerManager.hubSceneName : "Hub 1";
        bool loadingMatchFromHub = SceneManager.GetActiveScene().name == hubSceneName &&
            multiplayerManager != null && multiplayerManager.IsMatchTransitioning;

        // Hub1 is already a complete playable scene. Only wait when it is the temporary
        // active scene during the coordinated transition into a match.
        if(SceneManager.GetActiveScene().isLoaded && !loadingMatchFromHub)
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

