using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using TMPro;

public class HubManager : MonoBehaviour {
    public string sceneName;

    [SerializeField]
    private int maxPlayers = 4;

    private ISession currentSession;

    public TMP_InputField inputField; 

    public async void CreateGame() {
        try {
            var options = new SessionOptions {
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();

            currentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);

            Debug.Log("Relay session created!");
            Debug.Log($"Join Code: {currentSession.Code}");
            FindAndUpdateMultiplayerManager();
            CheckNetworkStarted();
        }
        catch(Exception e) {
            Debug.LogError($"Failed to create Relay session: {e}");
        }
    }

    private void FindAndUpdateMultiplayerManager() {
        MultiplayerManager managerScript = GameObject.FindAnyObjectByType<MultiplayerManager>();
        Debug.Log($"Transmitting Code: {currentSession.Code}");
        managerScript.multiplayerJoinCode = currentSession.Code;
    }

    private void CheckNetworkStarted() {
        if(currentSession.Network.State == NetworkState.Started) {
            LoadGameScene();
        }
        else {
            currentSession.Network.StateChanged += OnNetworkStateChanged;
        }
    }

    private void OnNetworkStateChanged(NetworkState state) {
        Debug.Log($"Network state changed: {state}");

        if(state == NetworkState.Started) {
            currentSession.Network.StateChanged -= OnNetworkStateChanged;
            LoadGameScene();
        }
    }

    private void LoadGameScene() {
        Debug.Log("Relay network started! Loading Game scene.");

        NetworkManager.Singleton.SceneManager.LoadScene(
            sceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
   

    public async void JoinGame() {

        try {

            string joinCode = inputField.text.Trim().ToUpper();

            currentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

            Debug.Log("Successfully joined Relay session!");
            Debug.Log($"Session ID: {currentSession.Id}");
        }
        catch(Exception e) {
            Debug.LogError($"Failed to join Relay session: {e}");
        }
    }
}