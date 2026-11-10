using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One settings-menu row: labels a GameAction and lets the player change its key.</summary>
public sealed class KeyBindingRow : MonoBehaviour
{
    [SerializeField] private GameAction action;
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private Button rebindButton;
    [SerializeField] private TMP_Text currentKeyText;
    [SerializeField] private TMP_Text promptText;
    private bool listening;

    private void OnEnable()
    {
        if (rebindButton) rebindButton.onClick.AddListener(BeginRebind);
        KeyBindingManager.BindingChanged += OnBindingChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (rebindButton) rebindButton.onClick.RemoveListener(BeginRebind);
        KeyBindingManager.BindingChanged -= OnBindingChanged;
    }

    private void Update()
    {
        if (!listening) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { listening = false; Refresh(); return; }
        if (!Input.anyKeyDown) return;
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (key == KeyCode.None || key == KeyCode.Escape || !Input.GetKeyDown(key)) continue;
            if (!KeyBindingManager.Set(action, key) && promptText) promptText.text = "Already in use";
            listening = false;
            Refresh();
            return;
        }
    }

    private void BeginRebind()
    {
        listening = true;
        if (promptText) promptText.text = "Press a key (Esc cancels)";
    }

    private void OnBindingChanged(GameAction changed)
    {
        if (changed == action || changed == default) Refresh();
    }

    private void Refresh()
    {
        if (actionNameText) actionNameText.text = action.ToString();
        if (currentKeyText) currentKeyText.text = KeyBindingManager.Get(action).ToString();
        if (promptText && !listening) promptText.text = "";
    }
}

