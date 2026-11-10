using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour {

    [SerializeField] private GameObject titleScreen, mainSelection;
    private bool anyKeyPressed = false;

    private void Update() {
        if(anyKeyPressed || !Input.anyKeyDown) return;

        anyKeyPressed = true;
        StartGame();
    }

    public void StartGame() {
        titleScreen.SetActive(false);
        mainSelection.SetActive(true);
    }
}
