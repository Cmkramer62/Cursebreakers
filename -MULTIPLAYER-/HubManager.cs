using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using TMPro;

public class HubManager : MonoBehaviour {
    [SerializeField]
    private TMP_InputField joinCodeInput;
    [SerializeField] private SceneLoader loaderScript;
    [SerializeField] private TextMeshProUGUI levelSelectionNotifyText, errorCodeText;
    [SerializeField] private GameObject startButton;

    [SerializeField] private GameObject titleUI, mainSelectionUI;
    private bool started = false;

    private void Start() {
        MultiplayerManager.Instance.OnStringChanged += OnErrorCodeChanged;

    }

    private void Update() {
        if(!started && Input.anyKeyDown) {
            titleUI.SetActive(false);
            mainSelectionUI.SetActive(true);
            started = true;
        }
    }

    private void OnErrorCodeChanged(string newCode) {
        errorCodeText.text = newCode;
    }

    public void JoinGame() {
        loaderScript.StartLoadingScreen();
        MultiplayerManager.Instance.JoinGame(joinCodeInput.text);
    }

    public void CreateGame() {
        loaderScript.StartLoadingScreen();
        MultiplayerManager.Instance.CreateGame();
    }

    public void SetLevel(string levelName) {
        MultiplayerManager.Instance.gameSceneName = levelName;
        levelSelectionNotifyText.GetComponent<TextAdder>().endWord = levelName;
        levelSelectionNotifyText.GetComponent<TextAdder>().StartAddingText();
        startButton.SetActive(true);
    }
}