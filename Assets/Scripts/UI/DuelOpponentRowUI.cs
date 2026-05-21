using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelOpponentRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text rankText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text powerText;
    [SerializeField] TMP_Text honorText;
    [SerializeField] Button   duelButton;
    [SerializeField] TMP_Text duelButtonText;

    DuelOpponent _opponent;
    Action<DuelOpponent> _onDuel;

    public void Refresh(DuelOpponent opponent, Action<DuelOpponent> onDuel)
    {
        _opponent = opponent;
        _onDuel   = onDuel;

        rankText.text  = $"#{opponent.rank}";
        nameText.text  = opponent.playerName;
        powerText.text = FormatNumber(opponent.powerScore);
        honorText.text = FormatNumber(opponent.honorPoints);

        bool used = DuelManager.IsUsed(opponent.playerId);
        duelButton.interactable = !used;
        duelButtonText.text     = used ? "Dueled" : "Duel";

        duelButton.onClick.RemoveAllListeners();
        duelButton.onClick.AddListener(() => _onDuel?.Invoke(_opponent));
    }

    static string FormatNumber(int n) =>
        n >= 1000 ? $"{n / 1000.0f:0.#}K" : n.ToString();
}
