using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using TMPro;

public class MultiplayerManager : MonoBehaviour {
    public static MultiplayerManager Instance { get; private set; }

    [Header("Scenes")]
    public string gameSceneName;
    public string hubSceneName;

    [Header("Session")]
    [SerializeField]
    private int maxPlayers = 4;

    //[Header("UI")]
   // public TMP_InputField inputField;

    private ISession currentSession;
    public string multiplayerJoinCode;

    private string latestUpdateMessageStored;
    [HideInInspector] public string latestUpdateMessage {
        get => latestUpdateMessageStored;
        set {
            if(latestUpdateMessageStored == value) {
                return;
            }

            latestUpdateMessageStored = value;
            OnStringChanged?.Invoke(latestUpdateMessageStored);
        }
    }

    public event System.Action<string> OnStringChanged;

    private async void Awake() {
        if(Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeServices();
    }


    private async System.Threading.Tasks.Task InitializeServices() {
        try {
            await Unity.Services.Core.UnityServices.InitializeAsync();
            latestUpdateMessage = "Unity Services initialized.";
            Debug.Log(latestUpdateMessage);

            if(!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) {
                await Unity.Services.Authentication.AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
                latestUpdateMessage = $"Signed in anonymously. Player ID: " +
                    $"{Unity.Services.Authentication.AuthenticationService.Instance.PlayerId}";

                Debug.Log(latestUpdateMessage);
            }
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to initialize Unity Services: {e}";
            Debug.LogError(latestUpdateMessage);
        }
    }


    // ============================================================
    // CREATE GAME
    // ============================================================

    public async void CreateGame() {
        try {
            var options = new SessionOptions {
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();

            currentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);
            latestUpdateMessage = "Relay session created!";
            Debug.Log(latestUpdateMessage);
            latestUpdateMessage = $"Join Code: {currentSession.Code}";
            Debug.Log(latestUpdateMessage);
            multiplayerJoinCode = currentSession.Code;

            CheckNetworkStarted();
            //GameObject.FindAnyObjectByType<SceneLoader>().StartLoadingScreen();

        }
        catch(Exception e) {
            GameObject.FindAnyObjectByType<SceneLoader>().CancelLoadingScreen();
            latestUpdateMessage = $"Failed to create Relay session: {e}";
            Debug.LogError(latestUpdateMessage);
        }
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
        latestUpdateMessage = $"Network state changed: {state}";
        Debug.Log(latestUpdateMessage);

        if(state == NetworkState.Started) {
            currentSession.Network.StateChanged -= OnNetworkStateChanged;

            LoadGameScene();
        }
    }


    private void LoadGameScene() {
        latestUpdateMessage = "Relay network started! Loading Game scene.";
        Debug.Log(latestUpdateMessage);

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );
    }


    // ============================================================
    // JOIN GAME
    // ============================================================

    public async void JoinGame(string joinCode) {
        try {
            joinCode = joinCode.Trim().ToUpper();

            currentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
            latestUpdateMessage = "Successfully joined Relay session!";
            Debug.Log(latestUpdateMessage);
            latestUpdateMessage = $"Session ID: {currentSession.Id}";
            Debug.Log(latestUpdateMessage);
            //GameObject.FindAnyObjectByType<SceneLoader>().StartLoadingScreen();
            // Now being done directly by Hub Manager.
        }
        catch(Exception e) {
            GameObject.FindAnyObjectByType<SceneLoader>().CancelLoadingScreen();
            latestUpdateMessage = $"Failed to join Relay session: {e}";
            Debug.LogError(latestUpdateMessage);
        }
    }


    // ============================================================
    // LEAVE GAME
    // ============================================================

    public async void LeaveGame() {
        try {
            if(currentSession != null) {
                await currentSession.LeaveAsync();

                currentSession = null;
                latestUpdateMessage = "Left multiplayer session.";
                Debug.Log(latestUpdateMessage);
            }
            GameObject.FindAnyObjectByType<SceneLoader>().StartLoadingScreen();
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to leave multiplayer session: {e}";
            Debug.LogError(latestUpdateMessage);
        }

        if(NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening) {
            NetworkManager.Singleton.Shutdown();
        }

        // DONT do the below if the HUB becomes 3d and it doesn't start with a canvas.
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(hubSceneName);
    }

    // ============================================================
    // EXIT GAME
    // ============================================================

    public async void ExitGame() {
        try {
            if(currentSession != null) {
                await currentSession.LeaveAsync();

                currentSession = null;
                latestUpdateMessage = "Left multiplayer session.";
                Debug.Log(latestUpdateMessage);
            }
            GameObject.FindAnyObjectByType<SceneLoader>().StartLoadingScreen();
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to leave multiplayer session: {e}";
            Debug.LogError(latestUpdateMessage);
        }

        if(NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening) {
            NetworkManager.Singleton.Shutdown();
        }

        Application.Quit();
    }


    // ============================================================
    // HOST DISCONNECT
    // ============================================================

    private void OnEnable() {
        if(NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }


    private void OnDisable() {
        if(NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }


    private void OnClientDisconnect(ulong clientId) {
        // If we're the client and the server/host disconnected,
        // return to the Hub.

        if(!NetworkManager.Singleton.IsServer &&
            clientId == NetworkManager.Singleton.LocalClientId) {
            StartCoroutine(ReturnToHub());
        }
    }


    private IEnumerator ReturnToHub() {
        GameObject.FindAnyObjectByType<SceneLoader>().StartLoadingScreen();
        yield return null;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(hubSceneName);
    }
}