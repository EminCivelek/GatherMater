using UnityEngine;

public class DuelArena : MonoBehaviour, IInteractable
{
    [SerializeField] private string arenaName = "Duel Arena";
    [SerializeField] private Sprite arenaIcon;

    public string InteractionLabel => arenaName;
    public Sprite InteractionIcon  => arenaIcon;
    public float  GatherDuration   => 0f;
    public bool   IsAvailable      => true;

    public void OnGatherComplete() => DuelUI.Instance?.Open();
}
