using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour {

    public string hubSceneName;

    private void Start() {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDestroy() {
        if(NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    public void LeaveGame() {
        NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(hubSceneName);
    }

    private void OnClientDisconnect(ulong clientId) {
        // If we're the client and the server disconnected us,
        // return to the Hub.

        if(!NetworkManager.Singleton.IsServer &&
            clientId == NetworkManager.Singleton.LocalClientId) {
            StartCoroutine(ReturnToHub());
        }
    }

    private IEnumerator ReturnToHub() {
        yield return null;

        // replace with scene loading script.
        SceneManager.LoadScene(hubSceneName);
    }

}