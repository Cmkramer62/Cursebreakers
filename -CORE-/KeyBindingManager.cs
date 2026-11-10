using System;
using UnityEngine;

public enum GameAction
{
    MoveForward, MoveBackward, MoveLeft, MoveRight, Jump, Sprint, Crouch,
    Interact, UseAbility, Pause, Ping, Spell1, Spell2, Spell3, Spell4, Spell5, Spell6, Spell7, Spell8
}

/// <summary>Legacy Input Manager key map used by gameplay and the rebinding UI.</summary>
public static class KeyBindingManager
{
    public static event Action<GameAction> BindingChanged;

    public static KeyCode Get(GameAction action)
    {
        return (KeyCode)PlayerPrefs.GetInt("KeyBinding_" + action, (int)DefaultFor(action));
    }

    public static bool GetKey(GameAction action) => Input.GetKey(Get(action));
    public static bool GetKeyDown(GameAction action) => Input.GetKeyDown(Get(action));
    public static bool GetKeyUp(GameAction action) => Input.GetKeyUp(Get(action));

    public static bool Set(GameAction action, KeyCode key)
    {
        if (key == KeyCode.None) return false;
        foreach (GameAction other in Enum.GetValues(typeof(GameAction)))
            if (other != action && Get(other) == key) return false;
        PlayerPrefs.SetInt("KeyBinding_" + action, (int)key);
        PlayerPrefs.Save();
        BindingChanged?.Invoke(action);
        return true;
    }

    public static void ResetAll()
    {
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
            PlayerPrefs.DeleteKey("KeyBinding_" + action);
        PlayerPrefs.Save();
        BindingChanged?.Invoke(default);
    }

    public static KeyCode DefaultFor(GameAction action)
    {
        switch (action)
        {
            case GameAction.MoveForward: return KeyCode.W;
            case GameAction.MoveBackward: return KeyCode.S;
            case GameAction.MoveLeft: return KeyCode.A;
            case GameAction.MoveRight: return KeyCode.D;
            case GameAction.Jump: return KeyCode.Space;
            case GameAction.Sprint: return KeyCode.LeftShift;
            case GameAction.Crouch: return KeyCode.LeftControl;
            case GameAction.Interact: return KeyCode.E;
            case GameAction.UseAbility: return KeyCode.F;
            case GameAction.Pause: return KeyCode.Tab;
            case GameAction.Ping: return KeyCode.Mouse2;
            case GameAction.Spell1: return KeyCode.Alpha1;
            case GameAction.Spell2: return KeyCode.Alpha2;
            case GameAction.Spell3: return KeyCode.Alpha3;
            case GameAction.Spell4: return KeyCode.Alpha4;
            case GameAction.Spell5: return KeyCode.Alpha5;
            case GameAction.Spell6: return KeyCode.Alpha6;
            case GameAction.Spell7: return KeyCode.Alpha7;
            case GameAction.Spell8: return KeyCode.Alpha8;
            default: return KeyCode.None;
        }
    }
}

