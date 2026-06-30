using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A lightweight, serializable Vector2 replacement for clean JSON output
/// and compatibility across Unity and web-based backends.
/// </summary>
[Serializable]
public struct Vector2S
{
    public float x;
    public float y;

    public Vector2S(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2S(Vector2 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
    }

    public Vector2 ToVector2() => new Vector2(x, y);
}

/// <summary>
/// Represents placed terrain block element in custom levels with position, scale, and rotation.
/// </summary>
[Serializable]
public class CustomTileData
{
    public string type;
    public Vector2S position;
    public Vector2S scale = new Vector2S(1f, 1f);
    public float rotation = 0f;
}

/// <summary>
/// Represents placed hazard/trap elements with position, scale, rotation, and mechanics properties.
/// </summary>
[Serializable]
public class CustomTrapData
{
    public string type;
    public Vector2S spawnPos;
    public Vector2S scale = new Vector2S(1f, 1f);
    public float rotation = 0f;
    
    // Physics/Trigger settings
    public string moveDir = "Down";
    public float speed = 3f;
    public float delay = 1f;

    // Trigger wiring (connection target coordinates)
    public bool hasTarget = false;
    public Vector2S targetPos;
}

/// <summary>
/// Root data structure representing a complete custom level configuration.
/// Can be fully serialized to a JSON string via JsonUtility.ToJson.
/// </summary>
[Serializable]
public class CustomLevelData
{
    public string levelName;
    public string creator;
    
    public int gridWidth = 32;
    public int gridHeight = 18;

    public Vector2S playerStartPos;
    public Vector2S goalPos;

    public List<CustomTileData> tiles = new List<CustomTileData>();
    public List<CustomTrapData> traps = new List<CustomTrapData>();
}
