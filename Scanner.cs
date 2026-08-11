using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Animations;

public class Scanner : NetworkBehaviour  {

    public bool allowedToScan = true;
    public AudioSource source;
    public AudioClip beep; // play same beep but at different pitches.??

    [SerializeField] private ToolController toolController;
    [SerializeField] private GameObject scannerVFX, scannerCanvas;
    [SerializeField] private Transform magicFollowPoint, canvasFollowPoint, headTransformLookAt;

    private bool spawnedObj = false;
    // There will be 4 glyphs in total. 4th signifying strongest EMF.
    public GameObject[] magicCanvasGlyphs;
    public GameObject particles, canvas;
    [SerializeField] private GameObject chainsGlowing, chainsNormal;
    /*
     * On a tick, set the scanner's lights to be what the value of defaultEMF is in the Tool Controller.
     * 
     * Bug: I can see the lights work. But only on the host, for the friend's scanner. Host's own scanner and both scanners of the client
     * are not working.
     * 
     * Read chat's response about this.
     */

    public override void OnNetworkSpawn() {
        toolController.defaultEMF.OnValueChanged += OnScannerChanged;
        OnScannerChanged(0, toolController.defaultEMF.Value);

        if(!spawnedObj) {
            spawnedObj = true;

            particles = GameObject.Instantiate(scannerVFX);
            canvas = GameObject.Instantiate(scannerCanvas);

            magicCanvasGlyphs = new GameObject[4] {canvas.transform.GetChild(0).GetChild(0).gameObject,
                canvas.transform.GetChild(0).GetChild(1).gameObject,
                    canvas.transform.GetChild(0).GetChild(2).gameObject,
                        canvas.transform.GetChild(0).GetChild(3).gameObject};

            particles.GetComponent<DelayFollow>().target = magicFollowPoint;
            canvas.GetComponent<DelayFollow>().target = canvasFollowPoint;

            // Setting EMF Magic Canvas's LookAtConstraint to be towards our headTransform. (Of the individual spawned player)
            LookAtConstraint constraint = canvas.GetComponent<LookAtConstraint>();
            List<ConstraintSource> sources = new List<ConstraintSource>();
            constraint.GetSources(sources);
            ConstraintSource newSource = new ConstraintSource();
            newSource.sourceTransform = headTransformLookAt;
            newSource.weight = 1.0f;
            sources.Add(newSource);
            constraint.SetSources(sources);
            constraint.enabled = true;
            constraint.constraintActive = true;

            // this will not work. you need to have them on a layer that's invisble to the camera, and then put back on.
            //particles.SetActive(false);
            //canvas.SetActive(false);

            particles.GetComponentInChildren<ParticleSystem>().Stop();
            canvas.SetActive(false);
            // turn off particles, then wait 1 sec, then set layer.
            // similarily, the glyphs should have in and out anims. Nothing crazy.
        }

        gameObject.SetActive(false);
    }

    private void OnEnable() {
        if(particles != null) {
            particles.GetComponentInChildren<ParticleSystem>().Play();
            canvas.SetActive(true);
            //play sound effect too?
            //start/resume playING passive thrumming sound.
            chainsGlowing.SetActive(true);
            chainsNormal.SetActive(false);

            OnScannerChanged(0, toolController.defaultEMF.Value);

        }
        // play expand anim
    }

    private void OnDisable() {
        if(particles != null) {
            particles.GetComponentInChildren<ParticleSystem>().Stop();
            canvas.SetActive(false);
            chainsGlowing.SetActive(false);
            chainsNormal.SetActive(true);
            //play sound effect too?
            //Pause playING passive thrumming sound.
        }
        // play shrink anim on uI
    }

    
    void OnScannerChanged(int oldValue, int newValue) {
        Debug.Log("Scanner owner: " + IsOwner);
         ScannerEffects(newValue);
        //scannerValue.Value = newValue;
    }

   // 8=4,7=4, 6=3,5=3, 4=2,3=2, 2=1,1=1, 0=0
    void ScannerEffects(int newValue) {
        if(gameObject.activeInHierarchy) {
            Debug.Log("Scanner new: " + newValue);
            int newAmount = (newValue / 2) + (newValue % 2);

            for(int i = 0; i < magicCanvasGlyphs.Length; i++) {
                magicCanvasGlyphs[i].SetActive(i < newAmount);
            }
            if(newValue != 0) {
                source.pitch = .8f;
                source.pitch += newValue / 10f;
                source.PlayOneShot(beep);
            }
        }
    }

}
