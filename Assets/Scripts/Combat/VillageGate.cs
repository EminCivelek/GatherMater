using UnityEngine;
using UnityEngine.SceneManagement;

public class VillageGate : MonoBehaviour
{
    [SerializeField] string combatSceneName = "CombatScene";
    [SerializeField] float activationDelay = 1f;

    bool _ready;

    void OnEnable()
    {
        _ready = false;
        Invoke(nameof(SetReady), activationDelay);
    }

    void SetReady() => _ready = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_ready && other.CompareTag("Player"))
            SceneManager.LoadScene(combatSceneName);
    }
}
