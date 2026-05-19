using UnityEngine;

public class AlchemyBuilding : MonoBehaviour, IInteractable
{
    [SerializeField] private string buildingName = "Alchemy Table";
    [SerializeField] private Sprite buildingIcon;

    public string InteractionLabel => buildingName;
    public Sprite InteractionIcon  => buildingIcon;
    public float  GatherDuration   => 0f;
    public bool   IsAvailable      => true;

    public void OnGatherComplete() => AlchemyUI.Instance?.Open(this);
}
