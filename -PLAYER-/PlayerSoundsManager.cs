using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;


public class PlayerSoundsManager : NetworkBehaviour {

    [SerializeField] private AudioSource voiceAudioSource, midBodyAudioSource;

    // Voiced. Will swap based on gender later.
    [SerializeField] private AudioClip[] jumpGrunt, landGrunt, runningBreath, outOfBreathStamina, wallJumpGrunt, hurtOrDamaged, jumpscareScream, jumpscareWarpedScream, coughing;
    // Not voiced
    [SerializeField] private AudioClip[] wallGrabSFX, ledgeGrabSFX, ledgeClimbSFX, crouchSFX, jumpSFX, ghostJumpSFX, slideSFX;

    public enum PlayerSound {
        JumpGrunt,
        LandGrunt,
        RunningBreath,
        OutOfBreathStamina,
        WallJumpGrunt,
        HurtOrDamaged,
        JumpscareScream,
        JumpscareWarpedScream,
        Coughing,
        WallGrabSFX,
        LedgeGrabSFX,
        LedgeClimbSFX,
        CrouchSFX,
        JumpSFX,
        GhostJumpSFX,
        SlideSFX
    }

    // Called by gameplay scripts
    public void PlaySound(PlayerSound sound) {
        PlaySoundServerRpc(sound);
    }

    [ServerRpc]
    private void PlaySoundServerRpc(PlayerSound sound) {
        AudioClip[] clips = sound switch {
            PlayerSound.JumpGrunt => jumpGrunt,
            PlayerSound.LandGrunt => landGrunt,
            PlayerSound.RunningBreath => runningBreath,
            PlayerSound.OutOfBreathStamina => outOfBreathStamina,
            PlayerSound.WallJumpGrunt => wallJumpGrunt,
            PlayerSound.HurtOrDamaged => hurtOrDamaged,
            PlayerSound.JumpscareScream => jumpscareScream,
            PlayerSound.JumpscareWarpedScream => jumpscareWarpedScream,
            PlayerSound.Coughing => coughing,
            PlayerSound.WallGrabSFX => wallGrabSFX,
            PlayerSound.LedgeGrabSFX => ledgeGrabSFX,
            PlayerSound.LedgeClimbSFX => ledgeClimbSFX,
            PlayerSound.CrouchSFX => crouchSFX,
            PlayerSound.JumpSFX => jumpSFX,
            PlayerSound.GhostJumpSFX => ghostJumpSFX,
            PlayerSound.SlideSFX => slideSFX,
            _ => null
        };

        if(clips.Length == 0 || clips == null)
            return;

        int index = Random.Range(0, clips.Length);
      
        PlayNewSoundClientRpc(sound, index);
    }

    // The big client Rpc. This rpc is called by everyone to actually play the sound.
    [ClientRpc]
    private void PlayNewSoundClientRpc(PlayerSound sound, int clipIndex) {
        AudioClip clip = sound switch {
            PlayerSound.JumpGrunt => jumpGrunt[clipIndex],
            PlayerSound.LandGrunt => landGrunt[clipIndex],
            PlayerSound.RunningBreath => runningBreath[clipIndex],
            PlayerSound.OutOfBreathStamina => outOfBreathStamina[clipIndex],
            PlayerSound.WallJumpGrunt => wallJumpGrunt[clipIndex],
            PlayerSound.HurtOrDamaged => hurtOrDamaged[clipIndex],
            PlayerSound.JumpscareScream => jumpscareScream[clipIndex],
            PlayerSound.JumpscareWarpedScream => jumpscareWarpedScream[clipIndex],
            PlayerSound.Coughing => coughing[clipIndex],
            PlayerSound.WallGrabSFX => wallGrabSFX[clipIndex],
            PlayerSound.LedgeGrabSFX => ledgeGrabSFX[clipIndex],
            PlayerSound.LedgeClimbSFX => ledgeClimbSFX[clipIndex],
            PlayerSound.CrouchSFX => crouchSFX[clipIndex],
            PlayerSound.JumpSFX => jumpSFX[clipIndex],
            PlayerSound.GhostJumpSFX => ghostJumpSFX[clipIndex],
            PlayerSound.SlideSFX => slideSFX[clipIndex],
            _ => null
        };

        if(clip == null) 
            return;

        if(sound == PlayerSound.WallGrabSFX || sound == PlayerSound.LedgeGrabSFX || sound == PlayerSound.LedgeClimbSFX
            || sound == PlayerSound.CrouchSFX || sound == PlayerSound.JumpSFX
            || sound == PlayerSound.GhostJumpSFX || sound == PlayerSound.SlideSFX) midBodyAudioSource.PlayOneShot(clip);
        else voiceAudioSource.PlayOneShot(clip);
        

    }

}
