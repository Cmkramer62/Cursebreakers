using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour {
    private const string ReadyProperty = "ready";
    private const string SelectedMapProperty = "selectedMap";
    private const string MatchStateProperty = "matchState";
    private const string PreparedProperty = "preparedForMatch";
    private bool preparingForMatch;
    public bool IsMatchTransitioning { get; private set; }
    public static MultiplayerManager Instance { get; private set; }

    [Header("Scenes")]
    public string gameSceneName;
    public string hubSceneName = "Hub 1";

    [Header("Session")]
    [SerializeField] private int maxPlayers = 4;

    private ISession currentSession;
    private Task servicesInitializationTask;
    private NetworkManager subscribedNetworkManager;

    public string multiplayerJoinCode;
    public ISession CurrentSession => currentSession;
    public bool HasParty => currentSession != null;
    public bool IsPartyHost => currentSession != null && currentSession.IsHost;
    public bool IsReady => currentSession != null &&
        currentSession.CurrentPlayer.Properties.TryGetValue(ReadyProperty, out var readyProperty) &&
        readyProperty.Value == "true";
    public string SelectedPartyMap => currentSession != null &&
        currentSession.Properties.TryGetValue(SelectedMapProperty, out var mapProperty) ? mapProperty.Value : null;
    public bool CanHostStartMatch => IsPartyHost && !string.IsNullOrWhiteSpace(SelectedPartyMap) &&
        currentSession.PlayerCount > 0 && currentSession.Players.Count > 0 &&
        currentSession.Players.All(IsPlayerReady);

    private string latestUpdateMessageStored;
    [HideInInspector] public string latestUpdateMessage {
        get => latestUpdateMessageStored;
        set {
            if(latestUpdateMessageStored == value) return;

            latestUpdateMessageStored = value;
            OnStringChanged?.Invoke(latestUpdateMessageStored);
        }
    }

    public event Action<string> OnStringChanged;
    public event Action OnPartyChanged;

    private void SubscribeToPartyChanges() {
        if(currentSession == null) return;
        currentSession.PlayerPropertiesChanged += HandlePartyChanged;
        currentSession.PlayerJoined += HandlePlayerJoined;
        currentSession.PlayerHasLeft += HandlePlayerLeft;
        currentSession.SessionPropertiesChanged += HandleMatchStateChanged;
        OnPartyChanged?.Invoke();
    }

    private void UnsubscribeFromPartyChanges() {
        if(currentSession == null) return;
        currentSession.PlayerPropertiesChanged -= HandlePartyChanged;
        currentSession.PlayerJoined -= HandlePlayerJoined;
        currentSession.PlayerHasLeft -= HandlePlayerLeft;
        currentSession.SessionPropertiesChanged -= HandleMatchStateChanged;
    }

    private void HandlePartyChanged() => OnPartyChanged?.Invoke();
    private void HandlePlayerJoined(string playerId) => OnPartyChanged?.Invoke();
    private void HandlePlayerLeft(string playerId) => OnPartyChanged?.Invoke();

    private bool IsPlayerPrepared(IReadOnlyPlayer player) =>
        player.Properties.TryGetValue(PreparedProperty, out var value) && value.Value == "true";

    private void HandleMatchStateChanged() {
        OnPartyChanged?.Invoke();
        if(currentSession != null && !currentSession.IsHost &&
            currentSession.Properties.TryGetValue(MatchStateProperty, out var state) && state.Value == "Preparing") {
            PrepareForPartyMatch();
        }
    }

    private bool IsPlayerReady(IReadOnlyPlayer player) =>
        player.Properties.TryGetValue(ReadyProperty, out var ready) && ready.Value == "true";

    private void Awake() {
        if(Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        servicesInitializationTask = InitializeServices();
    }

    private async Task InitializeServices() {
        try {
            await Unity.Services.Core.UnityServices.InitializeAsync();
            latestUpdateMessage = "Unity Services initialized.";
            Debug.Log(latestUpdateMessage);

            if(!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) {
                await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            latestUpdateMessage = "Signed in to Unity Services.";
            Debug.Log(latestUpdateMessage);
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to initialize Unity Services: {e.Message}";
            Debug.LogError(e);
            throw;
        }
    }

    private async Task EnsureServicesInitialized() {
        if(servicesInitializationTask == null) {
            throw new InvalidOperationException("Unity Services initialization has not started.");
        }

        await servicesInitializationTask;
    }

    /// <summary>
    /// Starts a one-player Netcode host for this player's personal hub.
    /// This uses the local transport and does not create a Relay allocation.
    /// </summary>
    public bool StartLocalHubNetwork() {
        NetworkManager networkManager = NetworkManager.Singleton;
        if(networkManager == null) {
            latestUpdateMessage = "Cannot start the personal hub: NetworkManager is missing.";
            Debug.LogError(latestUpdateMessage);
            return false;
        }

        SubscribeToNetworkCallbacks(networkManager);

        if(networkManager.IsListening) {
            latestUpdateMessage = "Personal hub network is already running.";
            return true;
        }

        if(!networkManager.StartHost()) {
            latestUpdateMessage = "Failed to start the personal hub network.";
            Debug.LogError(latestUpdateMessage);
            return false;
        }

        latestUpdateMessage = "Personal hub is running locally.";
        Debug.Log(latestUpdateMessage);
        return true;
    }

    public void StopLocalHubNetwork() {
        NetworkManager networkManager = NetworkManager.Singleton;
        if(networkManager == null || !networkManager.IsListening) return;

        networkManager.Shutdown();
        latestUpdateMessage = "Personal hub network stopped.";
    }

    private void SubscribeToNetworkCallbacks(NetworkManager networkManager) {
        if(subscribedNetworkManager == networkManager) return;

        if(subscribedNetworkManager != null) {
            subscribedNetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        subscribedNetworkManager = networkManager;
        subscribedNetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    // ============================================================
    // PARTY SESSION
    // ============================================================

    /// <summary>
    /// Creates the persistent party session without starting Relay.
    /// Relay is started later when the party is ready to enter a match.
    /// </summary>
    public async void CreateGame() {
        try {
            await EnsureServicesInitialized();

            if(currentSession != null) {
                latestUpdateMessage = "You are already in a party.";
                return;
            }

            var options = new SessionOptions {
                MaxPlayers = maxPlayers,
                Type = "CursebreakersParty"
            };

            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            SubscribeToPartyChanges();
            multiplayerJoinCode = currentSession.Code;
            latestUpdateMessage = $"Party created. Join code: {currentSession.Code}";
            Debug.Log(latestUpdateMessage);
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to create party: {e.Message}";
            Debug.LogError(e);
        }
    }

    public async void JoinGame(string joinCode) {
        try {
            await EnsureServicesInitialized();

            if(currentSession != null) {
                latestUpdateMessage = "Leave your current party before joining another.";
                return;
            }

            joinCode = joinCode.Trim().ToUpperInvariant();
            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
            SubscribeToPartyChanges();
            multiplayerJoinCode = currentSession.Code;
            latestUpdateMessage = $"Joined party {currentSession.Id}.";
            Debug.Log(latestUpdateMessage);
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to join party: {e.Message}";
            Debug.LogError(e);
        }
    }

    /// <summary>Sets this player's ready state in the shared party session.</summary>
    public async void ToggleReady() {
        if(currentSession == null) {
            latestUpdateMessage = "Create or join a party first.";
            return;
        }

        try {
            bool newReadyState = !IsReady;
            currentSession.CurrentPlayer.SetProperty(
                ReadyProperty,
                new PlayerProperty(newReadyState ? "true" : "false", VisibilityPropertyOptions.Member));
            await currentSession.SaveCurrentPlayerDataAsync();
            latestUpdateMessage = newReadyState ? "You are ready." : "You are not ready.";
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to update ready status: {e.Message}";
            Debug.LogError(e);
        }
    }

    /// <summary>Only the party host may choose the map for the next match.</summary>
    public async void SetSelectedPartyMap(string sceneName) {
        if(!IsPartyHost) {
            latestUpdateMessage = "Only the party host can choose the match map.";
            return;
        }
        if(string.IsNullOrWhiteSpace(sceneName)) return;

        try {
            gameSceneName = sceneName;
            IHostSession hostSession = currentSession.AsHost();
            hostSession.SetProperty(SelectedMapProperty,
                new SessionProperty(sceneName, VisibilityPropertyOptions.Member));
            await hostSession.SavePropertiesAsync();
            latestUpdateMessage = $"Match map selected: {sceneName}. Party members must ready up.";
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to share the selected map: {e.Message}";
            Debug.LogError(e);
        }
    }

    /// <summary>Validates readiness and begins the coordinated party network transition.</summary>
    public async void StartPartyMatch() {
        if(!IsPartyHost) { latestUpdateMessage = "Only the party host can start the match."; return; }
        if(!CanHostStartMatch) { latestUpdateMessage = "Select a map and wait for every party member to ready up."; return; }
        if(preparingForMatch) return;

        try {
            IHostSession hostSession = currentSession.AsHost();
            IsMatchTransitioning = true;
            hostSession.SetProperty(MatchStateProperty, new SessionProperty("Preparing", VisibilityPropertyOptions.Member));
            await hostSession.SavePropertiesAsync();
            PrepareForPartyMatch();
            latestUpdateMessage = "Party is preparing to enter the match.";
        }
        catch(Exception e) {
            latestUpdateMessage = $"Could not start the match: {e.Message}";
            Debug.LogError(e);
        }
    }

    private async void PrepareForPartyMatch() {
        if(preparingForMatch || currentSession == null) return;
        preparingForMatch = true;
        IsMatchTransitioning = true;
        try {
            StopLocalHubNetwork();
            currentSession.CurrentPlayer.SetProperty(PreparedProperty,
                new PlayerProperty("true", VisibilityPropertyOptions.Member));
            await currentSession.SaveCurrentPlayerDataAsync();

            if(!currentSession.IsHost) {
                latestUpdateMessage = "Waiting for the party host to connect the match.";
                return;
            }

            latestUpdateMessage = "Waiting for party members to prepare...";
            float timeoutAt = Time.realtimeSinceStartup + 30f;
            while(Time.realtimeSinceStartup < timeoutAt && currentSession.Players.Any(p => !IsPlayerPrepared(p)))
                await Task.Delay(200);

            if(currentSession.Players.Any(p => !IsPlayerPrepared(p))) {
                latestUpdateMessage = "Match launch timed out while waiting for party members.";
                preparingForMatch = false;
                IsMatchTransitioning = false;
                StartLocalHubNetwork();
                return;
            }

            latestUpdateMessage = "Connecting party through Relay...";
            await currentSession.AsHost().Network.StartRelayNetworkAsync(new RelayNetworkOptions());
            await WaitForNetworkClientsAsync(currentSession.PlayerCount, 30f);

            NetworkManager networkManager = NetworkManager.Singleton;
            if(networkManager == null || networkManager.SceneManager == null)
                throw new InvalidOperationException("Netcode scene management is unavailable after Relay startup.");

            var result = networkManager.SceneManager.LoadScene(SelectedPartyMap, LoadSceneMode.Single);
            if(result != SceneEventProgressStatus.Started)
                throw new InvalidOperationException($"Network scene load could not start: {result}.");

            latestUpdateMessage = $"Loading {SelectedPartyMap} for the party.";
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to launch party match: {e.Message}";
            Debug.LogError(e);
            preparingForMatch = false;
        }
    }

    private async Task WaitForNetworkClientsAsync(int expectedPlayers, float timeoutSeconds) {
        float timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;
        while(Time.realtimeSinceStartup < timeoutAt) {
            NetworkManager networkManager = NetworkManager.Singleton;
            if(networkManager != null && networkManager.IsServer && networkManager.ConnectedClientsIds.Count >= expectedPlayers)
                return;
            await Task.Delay(200);
        }
        throw new TimeoutException("Timed out waiting for all party members to connect to Relay.");
    }

    /// <summary>Returns a simple roster for the existing party panel, without avatars.</summary>
    public string GetPartyStatusText() {
        if(currentSession == null) return "Not in a party.";

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"Party code: {currentSession.Code}");
        foreach(var player in currentSession.Players) {
            bool ready = IsPlayerReady(player);
            lines.AppendLine($"Player {player.Id} — {(ready ? "Ready" : "Not Ready")}");
        }
        if(!string.IsNullOrWhiteSpace(SelectedPartyMap)) lines.AppendLine($"Map: {SelectedPartyMap}");
        return lines.ToString();
    }

    public async Task LeavePartyAsync() {
        if(currentSession == null) return;

        UnsubscribeFromPartyChanges();
        await currentSession.LeaveAsync();
        currentSession = null;
        multiplayerJoinCode = null;
        OnPartyChanged?.Invoke();
        latestUpdateMessage = "Left party.";
    }

    // ============================================================
    // MATCH EXIT / GAME EXIT
    // ============================================================

    /// <summary>
    /// Returns from a match while retaining party membership.
    /// The host stops Relay; party clients observe the session network stop.
    /// </summary>
    public async void LeaveGame() {
        try {
            if(currentSession != null && currentSession.IsHost &&
                currentSession.Network.State == NetworkState.Started) {
                await currentSession.AsHost().Network.StopNetworkAsync();
            }

            await ResetMatchTransitionStateAsync();

            if(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
                NetworkManager.Singleton.Shutdown();
            }

            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene(hubSceneName);
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to return to the personal hub: {e.Message}";
            Debug.LogError(e);
        }
    }

    private async Task ResetMatchTransitionStateAsync() {
        preparingForMatch = false;
        IsMatchTransitioning = false;
        if(currentSession == null) return;

        currentSession.CurrentPlayer.SetProperty(PreparedProperty,
            new PlayerProperty("false", VisibilityPropertyOptions.Member));
        await currentSession.SaveCurrentPlayerDataAsync();

        if(currentSession.IsHost) {
            IHostSession hostSession = currentSession.AsHost();
            hostSession.SetProperty(MatchStateProperty, new SessionProperty("Hub", VisibilityPropertyOptions.Member));
            await hostSession.SavePropertiesAsync();
        }
    }

    public async void ExitGame() {
        try {
            await LeavePartyAsync();
        }
        catch(Exception e) {
            latestUpdateMessage = $"Failed to leave party: {e.Message}";
            Debug.LogError(e);
        }

        if(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
            NetworkManager.Singleton.Shutdown();
        }

        Application.Quit();
    }

    private void OnClientDisconnect(ulong clientId) {
        NetworkManager networkManager = NetworkManager.Singleton;
        if(networkManager == null || networkManager.IsServer) return;

        if(clientId == networkManager.LocalClientId) {
            ReturnToHub();
        }
    }

    private async void ReturnToHub() {
        await Task.Yield();
        Cursor.lockState = CursorLockMode.None;
        await ResetMatchTransitionStateAsync();
        SceneManager.LoadScene(hubSceneName);
    }

    private void OnDestroy() {
        if(subscribedNetworkManager != null) {
            subscribedNetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        if(Instance == this) Instance = null;
    }
}
