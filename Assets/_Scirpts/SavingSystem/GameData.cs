using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // --- Player Data ---
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public float playerVerticalLookRotation; 

    // --- Enemy Data ---
    public Vector3 enemyPosition;
    
    // --- Pet (Fox) Data ---      
    public Vector3 foxPosition;

    // --- Inventory / Quest Data ---
    public int itemsDelivered;
    public string currentHeldItemID; // We save the string ID, not the ScriptableObject
    public List<string> collectedItemIDs; // Track IDs of items removed from the world

    // Constructor sets default values for a New Game
    public GameData()
    {
        itemsDelivered = 0;
        currentHeldItemID = "";
        collectedItemIDs = new List<string>();
        playerPosition = new Vector3(0, 1, 0); // Default spawn
        playerVerticalLookRotation = 0f; // New default
        enemyPosition = new Vector3(10, 1, 10); // Default enemy spawn
        foxPosition = new Vector3(1f, 1f, 0f);
    }
}