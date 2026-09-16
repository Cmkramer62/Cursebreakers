using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using Unity.Netcode;

public class CursedObject : NetworkBehaviour {

    // How do I make the below a synchronized thing? Data type is not normal.
    public enum CurseType { StellaeTrait, RadiatioTrait, FulgorTrait, AlgorTrait, ProfanusTrait, EvocareTrait}
    public NetworkList<int> cursesList = new NetworkList<int>();
    public NetworkVariable<bool> goalCurse = new NetworkVariable<bool>(false);

    public Light geistLight;
    [SerializeField] private ParticleSystem geistLightParticles, distortion;
    public int emfLevel = 7, temperature = 60;

   // public ToolController toolControllerScript;
    private Coroutine lightRoutine;
    public float charge = 0f, defaultMinLight = 0f, defaultMaxLight = 0.1f;

    private bool lowering = false;

    public AudioSource source, geistAudioA;
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
        if(cursesList.Count == 3) {
            return;
        }
        else {
            CurseType curseToAdd = CurseGivenIndex(Random.Range(0, 6));

            if(!cursesList.Contains((int)curseToAdd)) {
                cursesList.Add((int)curseToAdd);
            }
            SetRandomGoal();
        }
    }

    // Rather than randomly assign the curses, this method is used to give the goal curse specified curses.
    public void SetSpecificGoal(int freebieCurseIndex, int enviroCurseIndex, int auraCurseIndex) {
        cursesList.Add((int)CurseGivenIndex(freebieCurseIndex));
        cursesList.Add((int)CurseGivenIndex(enviroCurseIndex));
        cursesList.Add((int)CurseGivenIndex(auraCurseIndex));
    }

    private CurseType CurseGivenIndex(int index) {
        
        CurseType curseToAdd;
        
        if(index == 0) {
            curseToAdd = CurseType.StellaeTrait;
        }
        else if(index == 1) {
            curseToAdd = CurseType.RadiatioTrait;
        }
        else if(index == 2) {
            curseToAdd = CurseType.FulgorTrait;
        }
        else if(index == 3) {
            curseToAdd = CurseType.AlgorTrait;
            temperature = -20;
        }
        else if(index == 4) {
            curseToAdd = CurseType.ProfanusTrait;
        }
        else {
            curseToAdd = CurseType.EvocareTrait;
        }

        return curseToAdd;
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

            CurseType curseToAdd;
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
            curseToAdd = CurseGivenIndex(rand);

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
                if(cursesList.Contains((int)CurseType.RadiatioTrait)) {
                    toolControllerScript.defaultEMF.Value = emfLevel;
                }
                else if(toolControllerScript.defaultEMF.Value != 7) {
                    toolControllerScript.defaultEMF.Value = Random.Range(0, 6);
                }

                if(cursesList.Contains((int)CurseType.ProfanusTrait)) {
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
            DisplayCurse(CurseType.StellaeTrait, false);
        }

    }

    // Will this trigger for my tool controller if another player triggers this?
    private void OnTriggerExit(Collider other) {
        if(other.CompareTag("Player")) {
            var toolControllerScript = other.GetComponent<ToolController>();

            toolControllerScript.cursedObjectsWithinRange.Remove(this); //flawed. What if another curse removes itself before

            if(toolControllerScript.IsServer) {
                // If leaving an EMF, set value to 0.
                if(cursesList.Contains((int)CurseType.RadiatioTrait)) {
                    toolControllerScript.defaultEMF.Value = 0;
                }
                // If this isn't an EMF and they're not currently in a real EMF, set value to 0;
                else if(toolControllerScript.defaultEMF.Value != 7) {
                    toolControllerScript.defaultEMF.Value = 0;
                }

                if(cursesList.Contains((int)CurseType.ProfanusTrait)) {
                    // Wait random amount of time? Then,
                    toolControllerScript.CheckHolyWater();
                    Debug.Log("Left and this curse-" + gameObject.name + " does have unholy");

                }
            }

        }
    }

    public void DisplayCurse(CurseType type, bool state) {
        Debug.Log("Displaying curse ");
        bool found = false;
        foreach(CurseType curCurse in cursesList) {
            if(type == curCurse) found = true;
        }
        // run a check to see if the "type" is even in our list of curses in "cursesList".
        if(found && type == CurseType.StellaeTrait) {
            //geistLight.gameObject.SetActive(state);
            //Debug.Log("starting routine");
            if(lightRoutine != null) StopCoroutine(lightRoutine);
            lightRoutine = StartCoroutine(LightHelper.LerpLight(state, geistLight, 3, .1f)); //StartCoroutine(LerpLight(state));
            if(state) {
                geistAudioA.volume = 1f;
                geistAudioA.Play();

                geistLightParticles.Play();
            }
            else {
                AudioController.FadeOutAudio(this, geistAudioA, 2f);
                geistLightParticles.Stop();
            }
        }
        if(found && type == CurseType.FulgorTrait) {
            if(state) distortion.Play();
            
            if(!source.isPlaying) source.PlayOneShot(cameraWhooshClip, 1);
            // play jumpscare sound? Something very light. Perhaps even from a small random array of them.
            // is this a common thing amongst other curse reveals?..
        }
        if(found && type == CurseType.EvocareTrait) {
            source.pitch = Random.Range(.8f, 1.2f);
            source.PlayOneShot(cursedAudioClips[Random.Range(0, cursedAudioClips.Length)]);
        }

        if(tutorialCurse) GameObject.Find("TutorialManager").GetComponent<Tutorial>().toolDone = true;
    }

}
