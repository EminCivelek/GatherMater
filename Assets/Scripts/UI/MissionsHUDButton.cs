using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MissionsHUDButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>()?.onClick.AddListener(OnClick);
    }

    private void OnClick() => DailyMissionBoardUI.Instance?.Open();
}
