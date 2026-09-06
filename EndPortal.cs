using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EndPortal : MonoBehaviour {

    public PurificationManager purificationScript;
    public Death deathScript;
    public SceneLoader sceneLoaderScene;
    public bool activated;

    public AudioSource source, ghostSource;
    public AudioClip enterClip, leaveClip, correctClip, ghostDeathClip;

    public Animator portalAnimation;
    public Enemy ghostScript;

    public SaveDataHandler saveSystem;

    private void OnTriggerEnter(Collider other) {
        if(activated && other.name == "Player") {
            GetComponent<BoxCollider>().enabled = false;
            //AudioController.FadeOutAudio(this, purificationScript.cursedObjectScript.pSourceA, 1f);
            //AudioController.FadeOutAudio(this, purificationScript.cursedObjectScript.pSourceB, 1f);

            StartCoroutine(EndGoalTimer());
        }
        else if(other.name == "Player") {
            source.PlayOneShot(enterClip);
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.name == "Player") {
            GetComponent<InteractPrompt>().InteractWithObject();
            source.PlayOneShot(leaveClip);
        }   
    }

    private IEnumerator EndGoalTimer() {
        //purificationScript.GetComponent<CurseGameManager>().purifyState = purificationScript.potentialCursedItem.name == "Goal Curse" ? enterClip : enterWrongClip
        source.PlayOneShot(enterClip);

        yield return new WaitForSeconds(1f);
       

        /*
        if(purificationScript.potentialCursedItem.name == "Goal Curse") {
            //saveSystem.SetLevel(1);
            saveSystem.SetMissionData(1, purificationScript.GetComponent<CurseGameManager>().timeSpent, purificationScript.GetComponent<Death>().lives.Value,
                purificationScript.GetComponent<CurseGameManager>().timeSpotted, purificationScript.GetComponent<CurseGameManager>().longestChase, purificationScript.GetComponent<CurseGameManager>().purifyState);

            portalAnimation.Play("WipeAwayAnim");
            source.PlayOneShot(correctClip);
            yield return new WaitForSeconds(.5f);
            GameObject.Find("Ghost Enemy").GetComponent<Enemy>().allowedToMove.Value = false;
            for(int i = 0; i < ghostScript.gameObject.transform.childCount; i++) {
                if(ghostScript.transform.GetChild(i).gameObject.activeSelf) {
                    Debug.Log(ghostScript.transform.GetChild(i).name);
                    ghostScript.transform.GetChild(i).GetComponent<Animator>().SetTrigger("Death");
                    ghostScript.GetComponent<NavMeshAgent>().speed = 0;
                    ghostSource.PlayOneShot(ghostDeathClip); 
                    break;
                }
            }
            //ghostScript.GetComponent<Animator>().SetTrigger("Death");
            
            yield return new WaitForSeconds(7f);
            sceneLoaderScene.LoadScene(3);
        }
        else {
            deathScript.Jumpscare(false);
        }
        */
    }
}
