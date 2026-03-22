using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*
 * This script is run at the start of the Hub, when the player is coming back from a mission.
 * hubSave != -1 ..?
 * A console in the Hub can be used to access this menu again to see the last results.
 * This is called whether the user fails or succeeds their mission.
 * Upon enabling, it reveals each mission stat, then the grade, then spews and gives the crystals if this is the first time viewing.
 */
public class PostRoundManager : MonoBehaviour {
    // This should be the soul shard container's actual transform.
    [SerializeField] private RectTransform endingTransform, startingCornerTransform;
    [SerializeField] private GameObject soulShardPrefab, postRoundUI, pauseNormalUI;
    [SerializeField] private float force = 10, shardSpeed = 1f, spawningTotalTime = 2f, timeBeforeSpewing = 5f;
    [SerializeField] private TextMeshProUGUI missionSuccessText, purifiedText, timeSpentText, livesLeftText, timeSpottedText, longestChaseText, shardAmountText, shardGainedText, gradeText;
    [SerializeField] private PauseGame pauseScript;
    [SerializeField] private SaveDataHandler saveSystem;
    [SerializeField] private PlayerMovement playerScript;

    private List<GameObject> temporaryShards;
    private bool startMovingShards = true;
    private string[] gradeSystemList = { "F-", "F", "F+", "D-", "D", "D+", "C-", "C", "C+", "B-", "B", "B+", "A-", "A", "A+", "S-", "S", "S+"}; // 17 total.

    // Start is called before the first frame update
    void Start() {
        temporaryShards = new List<GameObject>();
    }

    // Update is called once per frame
    void Update() {
        if(startMovingShards) {
            for(int i = 0; i < temporaryShards.Count; i++) {
                var temptransf = temporaryShards[i].GetComponent<RectTransform>();
                temptransf.position = Vector3.Lerp(temptransf.position, endingTransform.position, shardSpeed * Time.deltaTime);
            }
        }
    }

    public void DisplayPostRoundResults(bool success, int secondsSpent, int livesLeft, int timeSpotted, int longestChase, int purified) {
        Debug.Log("Recieved Post");
        missionSuccessText.GetComponent<TextAdder>().endWord = success ? "Mission: Success" : "Mission: Failed";
        string purifyResult = "Incorrect object purified";
        if(purified == -1) purifyResult = "Purified";
        else if(purified == 0) purifyResult = "Not purified";
        purifiedText.GetComponent<TextAdder>().endWord = purifyResult;
        timeSpentText.GetComponent<TextAdder>().endWord = (secondsSpent / 60) + " min " + (secondsSpent % 60) + " sec";
        livesLeftText.GetComponent<TextAdder>().endWord = livesLeft.ToString();
        timeSpottedText.GetComponent<TextAdder>().endWord = timeSpotted.ToString();
        longestChaseText.GetComponent<TextAdder>().endWord = longestChase + " seconds";

        postRoundUI.SetActive(true);

        int pointTotal = 0;

        int timeCalc = 15;
        if(secondsSpent > 660) timeCalc = 0;
        else if(secondsSpent > 300 && secondsSpent <= 660) timeCalc = 5;
        else if(secondsSpent > 60 && secondsSpent <= 300) timeCalc = 10;

        pointTotal += (success ? 10 : -33) + timeCalc + (livesLeft * 2) - (timeSpotted - 2) + (10 - longestChase);
        // points possible = 10(success) + 15(under a minute) + 6(3 lives left) + 2(0 times spotted) + 10(longest chase 0 seconds) = 43.
        //

        if(pointTotal < 0) pointTotal = 0;
        StartCoroutine(WaitForSpew(pointTotal));

        gradeText.GetComponent<TextAdder>().endWord = gradeSystemList[(pointTotal * 17) / 43]; 
    }

    private IEnumerator WaitForSpew(int spewAmount) {
        playerScript.lockCursor = false;
        pauseScript.PauseGameHandler();
        Cursor.lockState = CursorLockMode.None;
        postRoundUI.SetActive(false);
        pauseNormalUI.SetActive(false);

        yield return new WaitForSeconds(2f);
        Cursor.lockState = CursorLockMode.None;
        postRoundUI.SetActive(true);
        yield return new WaitForSeconds(timeBeforeSpewing);
        if(spewAmount > 0) SpewShards(spewAmount);
        else {
            shardGainedText.text = "+" + 0;
            UpdateShardAmounts(0);
        }
    }

    public void ResumeNormalGame() {
        pauseScript.PauseGameHandler();
        postRoundUI.SetActive(false);
        pauseNormalUI.SetActive(true);
    }

    #region SHARDS
    public void SpewShards(int amount) {
        StartCoroutine(SpawningTimer(amount));
    }

    private IEnumerator SpawningTimer(int amount) {
        shardGainedText.text = "+" + amount.ToString();
        for(int i = 0; i < amount; i++) {
            var shard = GameObject.Instantiate(soulShardPrefab, startingCornerTransform);
            temporaryShards.Add(shard);
            shard.GetComponent<Rigidbody2D>().AddRelativeForce(Random.insideUnitCircle * force);
            if(i % 3 == 0) shard.GetComponent<AudioSource>().enabled = true;
            yield return new WaitForSeconds(spawningTotalTime / amount);
        }

        //yield return new WaitForSeconds(spawningTotalTime);

        //start lerping them toward the goal
        startMovingShards = true;
        yield return new WaitForSeconds(spawningTotalTime * 2);

        // shortly after, x seconds, kill the momentum \/
        foreach(GameObject shard in temporaryShards) {
            shard.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            shard.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            shard.GetComponent<Rigidbody2D>().angularVelocity = 0f;
        }
        
        yield return new WaitForSeconds(spawningTotalTime * 2);
        UpdateShardAmounts(amount);
        // play good sound effect, for updating your soul shard count with a higher number
        yield return new WaitForSeconds(spawningTotalTime * 3);
        ClearOnScreenShards();
    }

    private void UpdateShardAmounts(int deltaAmount) {
        saveSystem.SetShards(deltaAmount);
        saveSystem.SetLevel(0);
        // AKA, once we display the post round AND you've gained your shards, then the "last" mission is null
        // And we won't come back to this screen.
        shardAmountText.text = saveSystem.soulShards.ToString();
    }

    public void ClearOnScreenShards() {
        foreach(GameObject shard in temporaryShards) {
            GameObject.Destroy(shard);
        }
        temporaryShards.Clear();
    }
    #endregion

}
