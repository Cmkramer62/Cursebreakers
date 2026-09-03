using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour {

    public void ReturnToHub() {
        MultiplayerManager.Instance.LeaveGame();
    }

}