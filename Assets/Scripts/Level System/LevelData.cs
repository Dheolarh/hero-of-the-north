using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "False Steps/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber;
    public string sceneName;
    public string levelName;
    public bool isBossLevel = false; // Check if this is a boss level
    
    [Header("UI")]
    public Sprite levelIcon; 
    public Sprite lockedIcon;
    public Sprite bossLevelIcon; // Special icon for boss levels (shows instead of level number)
}
