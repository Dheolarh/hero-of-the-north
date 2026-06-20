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
    [Tooltip("The number of extra jumps (multi-jumps) the player can perform in mid-air (e.g., 1 for double jump, 2 for triple jump)")]
    public int multiJumpCount = 0;

    [Header("UI Icons")]
    public Sprite levelIcon;
    public Sprite lockedIcon;

    [Header("Boss Level UI")]
    public Sprite bossUnlockedIcon;
    public Sprite bossLockedIcon;
}
