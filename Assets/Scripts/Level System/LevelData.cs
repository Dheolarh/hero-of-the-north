using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "False Steps/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int    levelNumber;
    public string levelName;
    public bool   isBossLevel = false;

    [Header("Level Prefab")]
    [Tooltip("The root prefab for this level (tilemap, platforms, enemies, player, goal trigger, etc.)")]
    public GameObject levelPrefab;

    [Header("Level Settings")]
    [Tooltip("Allow the player to double-jump in this level")]
    public bool allowMultiJumps = false;

    [Header("UI Icons")]
    public Sprite levelIcon;
    public Sprite lockedIcon;

    [Header("Boss Level UI")]
    public Sprite bossUnlockedIcon;
    public Sprite bossLockedIcon;
}
