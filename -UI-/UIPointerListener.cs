using UnityEngine;
using UnityEngine.EventSystems;

public class UIPointerListener : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler {

    [SerializeField] private bool denyOrBackButton = false;

    public void OnPointerEnter(PointerEventData eventData) {
        PointerEntered();
    }

    public void OnPointerClick(PointerEventData eventData) {
        PointerClicked();
    }

    private void PointerEntered() {
        AudioManager.PlayHover();
    }

    private void PointerClicked() {
        if(!denyOrBackButton) AudioManager.PlayConfirm();
        else AudioManager.PlayDeny();
    }
}