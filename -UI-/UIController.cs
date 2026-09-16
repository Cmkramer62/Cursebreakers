using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

    public PlayerHandler target;

    public Image sprintBarBG, sprintBar;
    private Color stamBarUIColor;
    public bool useSprintBar = true, hideBarWhenFull = true;
    public CanvasGroup sprintBarCG;

    // private PlayerStats target;

    public void Initialize(PlayerHandler player) {
        target = player;

        target.stamina.OnValueChanged += OnStaminaChanged;
       // target.lives.OnValueChanged += OnLivesChanged;

        // Initialize immediately
        OnStaminaChanged(0, target.stamina.Value);
        stamBarUIColor = sprintBar.color;

        // OnLivesChanged(0, target.lives.Value);
    }

    private void OnStaminaChanged(float oldValue, float newValue) {
        // Update stamina bar
        if(hideBarWhenFull && useSprintBar) sprintBarCG.alpha += 5 * Time.deltaTime;

        if(useSprintBar) sprintBar.rectTransform.sizeDelta = new Vector2(newValue / target.GetComponent<PlayerMovement>().staminaDuration * 175, sprintBar.rectTransform.sizeDelta.y);

        if(newValue <= 0) {
            sprintBar.color = Color.red;
        }
        if(newValue == target.GetComponent<PlayerMovement>().staminaDuration) {
            if(hideBarWhenFull && useSprintBar) sprintBarCG.alpha -= 3 * Time.deltaTime;
            if(useSprintBar) sprintBar.color = stamBarUIColor;
        }

    }

    private void OnLivesChanged(int oldValue, int newValue) {
        // Update lives UI
    }
}
