using UnityEngine;

public class DailyMissionBuilding : MonoBehaviour, IInteractable
{
    [SerializeField] private string buildingName = "Mission Board";
    [SerializeField] private Sprite buildingIcon;

    public string InteractionLabel => buildingName;
    public Sprite InteractionIcon  => buildingIcon;
    public float  GatherDuration   => 0f;
    public bool   IsAvailable      => true;

    public void OnGatherComplete() => DailyMissionBoardUI.Instance?.Open();
}
