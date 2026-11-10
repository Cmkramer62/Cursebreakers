using System;
using UnityEngine;
using TMPro;

public class HubManager : MonoBehaviour {
    [SerializeField]
    private TMP_InputField joinCodeInput;
    [SerializeField] private SceneLoader loaderScript;
    [SerializeField] private TextMeshProUGUI levelSelectionNotifyText, errorCodeText;
    [SerializeField] private GameObject startButton;

    [SerializeField] private GameObject titleUI, mainSelectionUI;

    private void Start() {
        if(MultiplayerManager.Instance == null) {
            Debug.LogError("HubManager could not find MultiplayerManager.");
            return;
        }

        MultiplayerManager.Instance.OnStringChanged += OnErrorCodeChanged;
        MultiplayerManager.Instance.OnPartyChanged += RefreshPartyStatus;
        MultiplayerManager.Instance.StartLocalHubNetwork();
        RefreshPartyStatus();
    }

    private void OnDestroy() {
        if(MultiplayerManager.Instance != null) {
            MultiplayerManager.Instance.OnStringChanged -= OnErrorCodeChanged;
            MultiplayerManager.Instance.OnPartyChanged -= RefreshPartyStatus;
        }
    }

    private void OnErrorCodeChanged(string newCode) {
        RefreshPartyStatus();
    }

    private void RefreshPartyStatus() {
        if(errorCodeText == null) return;
        string message = MultiplayerManager.Instance != null ? MultiplayerManager.Instance.latestUpdateMessage : "";
        string partyStatus = MultiplayerManager.Instance != null ? MultiplayerManager.Instance.GetPartyStatusText() : "";
        errorCodeText.text = string.IsNullOrEmpty(partyStatus) ? message : $"{message}\n{partyStatus}";
    }

    // Hook this to the Hub1 pause-menu ready button.
    public void ToggleReady() {
        if(MultiplayerManager.Instance != null) MultiplayerManager.Instance.ToggleReady();
    }

    public void JoinGame() {
        MultiplayerManager.Instance.JoinGame(joinCodeInput.text);
    }

    public void CreateGame() {
        MultiplayerManager.Instance.CreateGame();
    }

    public void SetLevel(string levelName) {
        MultiplayerManager.Instance.gameSceneName = levelName;
        MultiplayerManager.Instance.SetSelectedPartyMap(levelName);
        levelSelectionNotifyText.GetComponent<TextAdder>().endWord = levelName;
        levelSelectionNotifyText.GetComponent<TextAdder>().StartAddingText();
        startButton.SetActive(true);
    }

    // Hook the party launch button to this method after the host has selected a map.
    public void StartPartyMatch() {
        if(MultiplayerManager.Instance != null) MultiplayerManager.Instance.StartPartyMatch();
    }
}
