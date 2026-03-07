using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;

public class CursedObject : MonoBehaviour {

    public enum CursedTypes { Glowing, EMF, Aura, Thermo, Unholy, Sound}
    public List<CursedTypes> cursesList;

    public GameObject distortion;
    public Light geistLight;
    public ParticleSystem geistLightParticles;
    public int emfLevel = 7, temperature = -20;

    public ToolController toolControllerScript;
    public List<int> index;
    private Coroutine lightRoutine;
    public float charge = 0f, defaultMinLight = 0f, defaultMaxLight = 0.1f;

    private bool lowering = false;

    public AudioSource source, geistAudioA, geistAudioB;
    public AudioClip geistlightClip, cameraWhooshClip;
    public AudioClip[] cursedAudioClips;

    public ParticleSystem purificationParticles;
    public GameObject purificationCanvas;
    public Slider purificationSlider;
    public AudioSource pSourceA, pSourceB;

    public int goalCurseThirdAspectIndex = -1;
    public bool tutorialCurse = false, pinged = false;

    void Start() {
        pSourceA.Play();
        pSourceA.Stop();

        pSourceB.Play();
        pSourceB.Stop();

        purificationParticles.Play();
        purificationParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Awake() {
        var LAC = purificationCanvas.GetComponent<LookAtConstraint>();
        LAC.locked = false;
        var source = new ConstraintSource {
            sourceTransform = Camera.main.transform,
            weight = 1f
        };

        LAC.AddSource(source);
        LAC.locked = true;
    }

    public void SetRandomGoal() {
        //Debug.Log("Random Goals " + cursesList.Count);
        if(cursesList.Count == 3) return;
        else {
            CursedTypes curseToAdd;
            int rand = Random.Range(0, 6);
            if(cursesList.Count == 2) goalCurseThirdAspectIndex = rand; // We keep track of the last aspect's index for the goal curse.
            if(rand == 0) curseToAdd = CursedTypes.Glowing;
            else if(rand == 1) curseToAdd = CursedTypes.EMF;
            else if(rand == 2) curseToAdd = CursedTypes.Aura;
            else if(rand == 3) {
                curseToAdd = CursedTypes.Thermo;
                temperature = -20;
            }
            else if(rand == 4) curseToAdd = CursedTypes.Unholy;
            else curseToAdd = CursedTypes.Sound;

            if(!cursesList.Contains(curseToAdd)) {
                cursesList.Add(curseToAdd);
                //Debug.Log("Goal Curse: " + curseToAdd.ToString());
                index.Add(rand);
            }
            SetRandomGoal();
            
        }
    }

    public void SetRandomCurses() {
        if(cursesList.Count == 3) return;
        else {
            int antiInt = GameObject.Find("Game Manager").GetComponent<CurseGameManager>().goalCurse.GetComponentInChildren<CursedObject>().goalCurseThirdAspectIndex;

            CursedTypes curseToAdd;
            int rand = Random.Range(0, 6);
            Debug.Log("anti int " + antiInt);
            if(rand == antiInt) {
                if(antiInt == 0 && rand == 0) rand += Random.Range(1, 4);
                else if(antiInt == 5 && rand == 5) rand -= Random.Range(1, 4);
                else rand += 1;
            }
            // if curse count is 2 (we only want to check the last and third curse. As in it's ok if 2/3 of the curses match up, but not the last one.
            // and rand = index of curse
            // also, because we are not remembering previous rands, we can have repeats. (only showing 2 or 1 curse).
            if(rand == 0) curseToAdd = CursedTypes.Glowing;
            else if(rand == 1) curseToAdd = CursedTypes.EMF;
            else if(rand == 2) curseToAdd = CursedTypes.Aura;
            else if(rand == 3) {
                curseToAdd = CursedTypes.Thermo;
                temperature = -20;
            }
            else if(rand == 4) curseToAdd = CursedTypes.Unholy;
            else curseToAdd = CursedTypes.Sound;

            if(!cursesList.Contains(curseToAdd)) {
                cursesList.Add(curseToAdd);
            }
            SetRandomCurses();
            
        }
    }

    private void OnTriggerEnter(Collider other) {
        if(other.name == "Player") {
            toolControllerScript.objectsList.Add(this);

            //if(cursesList.Contains(CursedTypes.Thermo)) {
            //    toolControllerScript.defaultTemp = temperature;
            //}
            if(cursesList.Contains(CursedTypes.EMF)) {
                toolControllerScript.defaultEMF = emfLevel;
            }
        }
    }

    public void Update() {
        if(charge > 0) {
            charge -= 10 * Time.deltaTime;
            lowering = true;
        }
        
        if(charge <= 0 && lowering) {
            lowering = false;
            DisplayCurse(CursedTypes.Glowing, false);
        }

    }


    private void OnTriggerExit(Collider other) {
        if(other.name == "Player") {
            toolControllerScript.objectsList.Remove(this); //flawed. What if another curse removes itself before
                                                           // we have a chance to for this specific one?
           // if(cursesList.Contains(CursedTypes.Thermo)) {
           //     toolControllerScript.defaultTemp = 60;
           // }
            if(cursesList.Contains(CursedTypes.EMF)) {
                toolControllerScript.defaultEMF = 0;
            }
        }
    }

    public void DisplayCurse(CursedTypes type, bool state) {
        bool found = false;
        foreach(CursedTypes curCurse in cursesList) {
            if(type == curCurse) found = true;
        }
        // run a check to see if the "type" is even in our list of curses in "cursesList".
        if(found && type == CursedTypes.Glowing) {
            //geistLight.gameObject.SetActive(state);
            //Debug.Log("starting routine");
            if(lightRoutine != null) StopCoroutine(lightRoutine);
            lightRoutine = StartCoroutine(LerpLight(state));
            if(state) {
                geistAudioA.volume = 0.773f;
                geistAudioA.Play();
                geistAudioB.volume = 1f;
                geistAudioB.Play();
                geistLightParticles.Play();
            }
            else {
                AudioController.FadeOutAudio(this, geistAudioA, .1f);
                AudioController.FadeOutAudio(this, geistAudioB, .1f);
                geistLightParticles.Stop();
            }
            //if(state) source.PlayOneShot(geistlightClip);
        }
        if(found && type == CursedTypes.Aura) {
            distortion.SetActive(state);
            if(!source.isPlaying) source.PlayOneShot(cameraWhooshClip, 1);
            // play jumpscare sound? Something very light. Perhaps even from a small random array of them.
            // is this a common thing amongst other curse reveals?..
        }
        if(found && type == CursedTypes.Sound) {
            source.pitch = Random.Range(.8f, 1.2f);
            source.PlayOneShot(cursedAudioClips[Random.Range(0, cursedAudioClips.Length)]);
        }

        if(tutorialCurse) GameObject.Find("TutorialManager").GetComponent<Tutorial>().toolDone = true;
    }

    private IEnumerator LerpLight(bool state) {
        float start, end;
        
        if(state) {
            start = geistLight.intensity;
            end = 0.1f;
        }
        else {
            start = geistLight.intensity;
            end = 0f;
        }

        float time = 0f;
        // geistLight.intensity = start;

        while(time < 3) {
            time += Time.deltaTime;
            float t = time / 1;

            geistLight.intensity = Mathf.Lerp(start, end, t);
            yield return null;
        }

        geistLight.intensity = end; // ensure final value hits exactly 1
    }
}
