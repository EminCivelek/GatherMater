using UnityEngine;

/// <summary>
/// Registry of all ItemData assets. Used to resolve items by ID after loading from PlayerPrefs.
/// SETUP: Create one instance via right-click → GatherMater → Item Database,
///        place it at Assets/Resources/ItemDatabase, then drag all ItemData assets into the list.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "GatherMater/Item Database")]
public class ItemDatabase : ScriptableObject
{
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
            return _instance;
        }
    }

    [SerializeField] private ItemData[] items;

    public ItemData FindById(string id) =>
        System.Array.Find(items, i => i != null && i.name == id);
}
