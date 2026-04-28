using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionButton : MonoBehaviour
{
    [SerializeField] private Button      button;
    [SerializeField] private TMP_Text    label;
    [SerializeField] private Image       icon;
    [SerializeField] private Sprite      defaultIcon;

    private PlayerInteraction playerInteraction;

    private void Start()
    {
        playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        if (playerInteraction == null) return;

        playerInteraction.OnInteractableChanged += UpdateButton;
        playerInteraction.OnGatherStarted       += OnGatherStarted;
        playerInteraction.OnGatherFinished      += OnGatherFinished;

        button.onClick.AddListener(playerInteraction.OnInteractButtonPressed);

        // Start grayed out — no interactable nearby yet
        SetNoInteractable();
    }

    private void OnDestroy()
    {
        if (playerInteraction == null) return;
        playerInteraction.OnInteractableChanged -= UpdateButton;
        playerInteraction.OnGatherStarted       -= OnGatherStarted;
        playerInteraction.OnGatherFinished      -= OnGatherFinished;
    }

    private void UpdateButton(IInteractable interactable)
    {
        if (interactable == null)
        {
            SetNoInteractable();
            return;
        }

        button.interactable = true;
        label.text  = interactable.InteractionLabel;
        icon.sprite = interactable.InteractionIcon != null
            ? interactable.InteractionIcon
            : defaultIcon;
    }

    private void OnGatherStarted()
    {
        label.text = "Cancel";
    }

    private void OnGatherFinished()
    {
        // OnInteractableChanged fires next frame from the scan — nothing to do here
    }

    private void SetNoInteractable()
    {
        button.interactable = false;
        label.text          = "Interact";
        icon.sprite         = defaultIcon;
    }
}
