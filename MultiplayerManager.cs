using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

public class MultiplayerManager : MonoBehaviour {
    public static MultiplayerManager Instance { get; private set; }
    
    public string multiplayerJoinCode;

    private async void Awake() {
        if(Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);



        await InitializeServices();
    }

    private async Task InitializeServices() {
        try {
            await UnityServices.InitializeAsync();

            Debug.Log("Unity Services initialized.");

            if(!AuthenticationService.Instance.IsSignedIn) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Debug.Log(
                    $"Signed in anonymously. Player ID: {AuthenticationService.Instance.PlayerId}"
                );
            }
        }
        catch(Exception e) {
            Debug.LogError($"Failed to initialize Unity Services: {e}");
        }
    }
}