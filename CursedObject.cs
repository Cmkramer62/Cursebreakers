using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using Unity.Netcode;

public class CursedObject : NetworkBehaviour {

    // How do I make the below a synchronized thing? Data type is not normal.
    public enum CursedTypes { Glowing, EMF, Aura, Thermo, Unholy, Sound}
    public NetworkList<int> cursesList = new NetworkList<int>();

    public Light geistLight;
    [SerializeField] private ParticleSystem geistLightParticles, distortion;
    public int emfLevel = 7, temperature = -20;

   // public ToolController toolControllerScript;
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

    [HideInInspector] public CurseGameManager curseGameManager;

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

            if(!cursesList.Contains((int)curseToAdd)) {
                cursesList.Add((int)curseToAdd);
                //Debug.Log("Goal Curse: " + curseToAdd.ToString());
                //index.Add(rand);
            }
            SetRandomGoal();
            
        }
    }

    public void SetRandomCurses() {
        if(cursesList.Count == 3) return;
        else {
            GameObject potentialGoalCurse = null;
            if(curseGameManager.goalCurse.Value.TryGet(out NetworkObject networkObject)) {
                potentialGoalCurse = networkObject.gameObject;
            }
            int antiInt = -1;
            if(potentialGoalCurse != null) antiInt = potentialGoalCurse.GetComponentInChildren<CursedObject>().goalCurseThirdAspectIndex;
            else Debug.Log("ERROR IN CURSED OBJECT, COULD NOT GET GOALCURSE.");

            CursedTypes curseToAdd;
            int rand = Random.Range(0, 6);
            //Debug.Log("anti int " + antiInt);
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

            if(!cursesList.Contains((int)curseToAdd)) {
                cursesList.Add((int)curseToAdd);
            }
            SetRandomCurses();
            
        }
    }

    // Will this trigger for my tool controller if another player triggers this?
    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player")) {
            var toolControllerScript = other.GetComponent<ToolController>();
            toolControllerScript.cursedObjectsWithinRange.Add(this);

            if(toolControllerScript.IsServer) {
                // EMF Section
                if(cursesList.Contains((int)CursedTypes.EMF)) {
                    toolControllerScript.defaultEMF.Value = emfLevel;
                }
                else if(toolControllerScript.defaultEMF.Value != 7) {
                    toolControllerScript.defaultEMF.Value = Random.Range(0, 6);
                }

                // Thermometer Section
                //if(cursesList.Contains(CursedTypes.Thermo)) {
                //    toolControllerScript.defaultTemp = temperature;
                //}
                if(cursesList.Contains((int)CursedTypes.Thermo)) {
                    //toolControllerScript.defaultTemp.Value = temperature;
                }
                else if(toolControllerScript.defaultTemp.Value != -20) {
                    //toolControllerScript.defaultTemp.Value = Random.Range(57, 63);
                }

                if(cursesList.Contains((int)CursedTypes.Unholy)) {
                    // Wait random amount of time? Then,
                    toolControllerScript.CheckHolyWater();
                }
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

    // Will this trigger for my tool controller if another player triggers this?
    private void OnTriggerExit(Collider other) {
        if(other.CompareTag("Player")) {
            var toolControllerScript = other.GetComponent<ToolController>();

            toolControllerScript.cursedObjectsWithinRange.Remove(this); //flawed. What if another curse removes itself before
                                                           // we have a chance to for this specific one?
                                                           // if(cursesList.Contains(CursedTypes.Thermo)) {
                                                           //     toolControllerScript.defaultTemp = 60;
                                                           // }

            if(toolControllerScript.IsServer) {
                // If leaving an EMF, set value to 0.
                if(cursesList.Contains((int)CursedTypes.EMF)) {
                    toolControllerScript.defaultEMF.Value = 0;
                }
                // If this isn't an EMF and they're not currently in a real EMF, set value to 0;
                else if(toolControllerScript.defaultEMF.Value != 7) {
                    toolControllerScript.defaultEMF.Value = 0;
                }

                // If leaving a Thermo, set value to 60.
                if(cursesList.Contains((int)CursedTypes.Thermo)) {
                    //toolControllerScript.defaultTemp.Value = 60;
                    Debug.Log("Left and this curse-" + gameObject.name + " does have EMF");
                }
                // If this isn't a Thermo and they're not currently in a real Thermo, set value to 60;
                else if(toolControllerScript.defaultTemp.Value != -20) {
                    //toolControllerScript.defaultTemp.Value = 60;
                    Debug.Log("Left and this curse-" + gameObject.name + " does NOT have EMF, and the torch is not -20");
                }
                else {
                    Debug.Log("Left and this curse-" + gameObject.name + " does NOT have EMF, and the torch is -20");
                }

                if(cursesList.Contains((int)CursedTypes.Unholy)) {
                    // Wait random amount of time? Then,
                    toolControllerScript.CheckHolyWater();
                    Debug.Log("Left and this curse-" + gameObject.name + " does have unholy");

                }
            }

        }
    }

    public void DisplayCurse(CursedTypes type, bool state) {
        Debug.Log("Displaying curse ");
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
            if(state) distortion.Play();
            
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
