using Unity.Netcode;
using UnityEngine;
using System;

public class ServerManager : NetworkBehaviour {
    [SerializeField] private GameObject timerPrefab, curseGamePrefab;
    public bool spawnGhost = true, passiveGhost = false, dontMoveGhost = false;
    public int timerAmount = 600;

    public CursedObject.CurseType freebieCurse, enviroCurse, auraCurse;
    public bool randomizeCurse = true;

    public override void OnNetworkSpawn() {
        if(!IsServer) return;

        StartGame();
    }

    void StartGame() {
        var timer = Instantiate(timerPrefab);
        timer.GetComponent<NetworkObject>().Spawn();
        timer.GetComponent<GameTimer>().timeLeft.Value = timerAmount;

        var cursegame = Instantiate(curseGamePrefab);
        cursegame.GetComponent<CurseGameManager>().spawnGhost = spawnGhost;
        cursegame.GetComponent<CurseGameManager>().dontMoveGhost = dontMoveGhost;

        if(!randomizeCurse) {
            cursegame.GetComponent<CurseGameManager>().freebieCurse = freebieCurse;
            cursegame.GetComponent<CurseGameManager>().enviroCurse = enviroCurse;
            cursegame.GetComponent<CurseGameManager>().auraCurse = auraCurse;

            cursegame.GetComponent<CurseGameManager>().randomizeCurse = false;
        }
        cursegame.GetComponent<NetworkObject>().Spawn();
    }

}