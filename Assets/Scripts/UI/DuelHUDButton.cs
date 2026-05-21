using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DuelHUDButton : MonoBehaviour
{
    void Start() => GetComponent<Button>()?.onClick.AddListener(() => DuelUI.Instance?.Open());
}
