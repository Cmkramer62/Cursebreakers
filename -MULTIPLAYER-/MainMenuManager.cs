using UnityEngine;
using TMPro;

public class MainMenuManager : MonoBehaviour {

    public GameObject startNewGameButton, startOverButton, continueButton;
    public TextMeshProUGUI levelText;
    public SceneLoader sceneLoaderScript;
    public int firstScene;

    // if player has no save data, mm says - start, settings, exit
    // if player has save data, mm says - start, continue, settings, exit

    // Start is called before the first frame update
    void Start() {
        bool hasData = PlayerPrefs.HasKey("levelNumber");
        if(hasData) levelText.text = PlayerPrefs.GetInt("levelNumber").ToString();
        continueButton.SetActive(hasData);
        startNewGameButton.SetActive(!hasData);
        startOverButton.SetActive(hasData);
    }

    /*
     * Done when the player clicks on "Start", while already having saved data.
     */
    public void ClearAndLoad() {
        ClearData();
        sceneLoaderScript.LoadScene(firstScene);
    }

    public void ClearData() {
        PlayerPrefs.DeleteKey("levelNumber");
        PlayerPrefs.DeleteKey("invNames");
        PlayerPrefs.DeleteKey("invAmounts");
    }

    public void ContinueButton() {
        sceneLoaderScript.LoadScene(PlayerPrefs.GetInt("levelNumber"));
    }

    public void SetLevelDebug(int level) {
        PlayerPrefs.SetInt("levelNumber", level + 1);
        Debug.Log("set " + (level + 1));
    }
}
