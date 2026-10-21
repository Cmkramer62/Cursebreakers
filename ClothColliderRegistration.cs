using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using System.Collections;
using Unity.Netcode;

public class ClothColliderRegistration : MonoBehaviour {
    [SerializeField] private Collider playerCollider;

    private IEnumerator Start() {
        yield return WaitForNetworkSceneLoad();

        Cloth[] cloths = FindObjectsOfType<Cloth>();

        foreach(Cloth cloth in cloths) {
            cloth.capsuleColliders = AddCollider(
                cloth.capsuleColliders,
                playerCollider
            );
        }
    }

    private CapsuleCollider[] AddCollider(
        CapsuleCollider[] existing,
        Collider collider) {
        CapsuleCollider capsule = collider as CapsuleCollider;

        if(capsule == null)
            return existing;

        CapsuleCollider[] result =
            new CapsuleCollider[existing.Length + 1];

        existing.CopyTo(result, 0);
        result[result.Length - 1] = capsule;

        return result;
    }

    private IEnumerator WaitForNetworkSceneLoad() {
        if(!gameObject.GetComponentInParent<PlayerHandler>().IsServer && SceneManager.GetActiveScene().isLoaded)
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