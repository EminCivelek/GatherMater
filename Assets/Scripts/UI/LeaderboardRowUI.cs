using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text rankText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] Image    background;
    [SerializeField] Color    defaultColor   = new Color(1f, 1f, 1f, 0.05f);
    [SerializeField] Color    highlightColor = new Color(1f, 0.85f, 0f, 0.25f);

    public void SetData(int rank, string playerName, double score, bool isLocalPlayer)
    {
        rankText.text  = rank > 0 ? $"#{rank}" : "--";
        nameText.text  = playerName;
        scoreText.text = ((long)score).ToString("N0");

        if (background != null)
            background.color = isLocalPlayer ? highlightColor : defaultColor;

        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
