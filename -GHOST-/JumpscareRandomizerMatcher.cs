using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This script is meant to be client side only. Only the client will see the jumpscare animation.
 * So this script waits until it can find the ghostRandomizer Script (there will only be 1) and sets the
 * player's jumpscare ghost to be the real ghost's random values based on the code.
 */
public class JumpscareRandomizerMatcher : MonoBehaviour {

    public GameObject[] ghostBodies;

    #region MeshLists
    public SkinnedMeshRenderer[] gownLongHair, gownShortHair, nakedLongHair, nakedShortHair, nakedBald, robe, veil, victorian;
    private Material[] skinMats, eyeMats, teethMats, hairMats; // shared amongst all ghosts, if they have hair. Naked only uses these.
    private Material[] gownMats; // only for the gown ghost.
    private Material[] hoodMats, robeMats, shoulderclothMats; // only for the robe ghost. One index needed for all, except shoulders.
    private Material[] veilMats; // only one for the veil ghost.
    private Material[] headclothMats, dressMats; // only for the victorian ghost.
    private Material glowingEyesMat;
    #endregion
   // public GameObject[] horns;

    private GhostRandomizer ghostRandomizerScript;

    private IEnumerator Start() {
        while(ghostRandomizerScript == null) {
            ghostRandomizerScript = FindObjectOfType<GhostRandomizer>();
            yield return null;
        }
        MimicMaterialLists();
        ApplyRandomization(ghostRandomizerScript.generatedRanString.Value);
        ghostRandomizerScript.generatedRanString.OnValueChanged += OnGenChanged;

    }

    // Copies the references for assets of materials from the real ghost.
    private void MimicMaterialLists() {
        skinMats = ghostRandomizerScript.skinMats;
        eyeMats = ghostRandomizerScript.eyeMats;
        teethMats = ghostRandomizerScript.teethMats;
        hairMats = ghostRandomizerScript.hairMats;
        gownMats = ghostRandomizerScript.gownMats;
        hoodMats = ghostRandomizerScript.hoodMats;
        robeMats = ghostRandomizerScript.robeMats;
        shoulderclothMats = ghostRandomizerScript.shoulderclothMats;
        veilMats = ghostRandomizerScript.veilMats;
        headclothMats = ghostRandomizerScript.headclothMats;
        dressMats = ghostRandomizerScript.dressMats;
        glowingEyesMat = ghostRandomizerScript.glowingEyesMat;
    }

    private void OnGenChanged(GhostAppearance old, GhostAppearance newval) {
        ApplyRandomization(newval);
    }

    // Called by EITHER. Runs when code changes, or on network spawn.
    public void ApplyRandomization(GhostAppearance generatedCode) {
        //ghostBodies[generatedCode.body].SetActive(true);
        GetComponentInParent<Death>().jumpscareGhostBodyIndex = generatedCode.body;

        if(generatedCode.body == 0) SetGownLongHair(generatedCode);
        else if(generatedCode.body == 1) SetGownShortHair(generatedCode);
        else if(generatedCode.body == 2) SetNakedLongHair(generatedCode);
        else if(generatedCode.body == 3) SetNakedShortHair(generatedCode);
        else if(generatedCode.body == 4) SetNakedBald(generatedCode);
        else if(generatedCode.body == 5) SetRobe(generatedCode);
        else if(generatedCode.body == 6) SetVeil(generatedCode);
        else SetVictorian(generatedCode);
    }

    #region Setting Body Parts
    private void SetGownLongHair(GhostAppearance generatedCode) {
        var bodyMats = gownLongHair[0].materials;
        bodyMats[0] = skinMats[generatedCode.skin];
        bodyMats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) bodyMats[1] = glowingEyesMat;
        bodyMats[2] = teethMats[generatedCode.teeth];
        gownLongHair[0].materials = bodyMats;

        var hairMats = gownLongHair[1].materials;
        hairMats[0] = this.hairMats[generatedCode.hair];
        gownLongHair[1].materials = hairMats;

        var gownMats = gownLongHair[2].materials;
        gownMats[0] = gownMats[0]; // generatedcode.gown seems to keep throwing out of bounds.
        gownLongHair[2].materials = gownMats;
    }

    private void SetGownShortHair(GhostAppearance generatedCode) {
        var mats = gownShortHair[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        gownShortHair[0].materials = mats;

        var mats2 = gownShortHair[1].materials;
        mats2[0] = gownMats[generatedCode.gown];
        gownShortHair[1].materials = mats2;

        var mats3 = gownShortHair[2].materials;
        mats3[0] = hairMats[generatedCode.hair];
        gownShortHair[2].materials = mats3;
    }

    private void SetNakedLongHair(GhostAppearance generatedCode) {
        var mats = nakedLongHair[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        nakedLongHair[0].materials = mats;

        var mats2 = nakedLongHair[1].materials;
        mats2[0] = hairMats[generatedCode.hair];
        nakedLongHair[1].materials = mats2;
    }

    private void SetNakedShortHair(GhostAppearance generatedCode) {
        var mats = nakedShortHair[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        nakedShortHair[0].materials = mats;

        var mats2 = nakedShortHair[1].materials;
        mats2[0] = hairMats[generatedCode.hair];
        nakedShortHair[1].materials = mats2;
    }

    private void SetNakedBald(GhostAppearance generatedCode) {
        var mats = nakedBald[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        nakedBald[0].materials = mats;
    }

    private void SetRobe(GhostAppearance generatedCode) {
        var mats = robe[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        robe[0].materials = mats;

        var mats2 = robe[1].materials;
        mats2[0] = hoodMats[generatedCode.hood];
        robe[1].materials = mats2;

        var mats3 = robe[2].materials;
        mats3[0] = robeMats[generatedCode.robe];
        robe[2].materials = mats3;

        var mats4 = robe[3].materials;
        mats4[0] = shoulderclothMats[generatedCode.shoulder];
        robe[3].materials = mats4;
    }

    private void SetVeil(GhostAppearance generatedCode) {
        var mats = veil[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        veil[0].materials = mats;

        var mats2 = veil[1].materials;
        mats2[0] = veilMats[generatedCode.veil];
        veil[1].materials = mats2;
    }

    private void SetVictorian(GhostAppearance generatedCode) {
        var mats = victorian[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(ghostRandomizerScript.overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        victorian[0].materials = mats;

        var mats2 = victorian[1].materials;
        mats2[0] = headclothMats[generatedCode.headcloth];
        victorian[1].materials = mats2;

        var mats3 = victorian[2].materials;
        mats3[0] = dressMats[generatedCode.dress];
        victorian[2].materials = mats3;
    }
    #endregion
}
