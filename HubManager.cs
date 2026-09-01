using UnityEngine;
using Unity.Netcode;

public class HubManager : MonoBehaviour {

    public string sceneName;

    public void CreateGame() {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;

        NetworkManager.Singleton.StartHost();
    }


    private void OnServerStarted() {
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;

        NetworkManager.Singleton.SceneManager.LoadScene(
            sceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}