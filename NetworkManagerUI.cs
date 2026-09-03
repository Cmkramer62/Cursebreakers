using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour {

    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private GameObject background;

    private void Awake() {
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            TurnOffButtons();
        });
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            TurnOffButtons();
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            TurnOffButtons();
        });
    }

    private void TurnOffButtons() {
        serverBtn.gameObject.SetActive(false);
        hostBtn.gameObject.SetActive(false);
        clientBtn.gameObject.SetActive(false);
        background.SetActive(false);
    }

}
