using System;
using System.Collections.Generic;
using UnityEngine;
using LushWorld.Inventory;

namespace LushWorld.Save
{
    [Serializable]
    public class GameSaveData
    {
        public PlayerSaveData              playerData;
        public InventorySaveData           inventoryData;
        public List<BlueprintSaveData>     blueprints;
        public List<BuildingPieceSaveData> buildings;
        public int                         heartsPlaced;
        public WorldSaveData               worldData;
        public List<Vector3>               harvestedSpotPositions;
        public long                        saveTimestamp;
    }

    [Serializable]
    public class PlayerSaveData
    {
        public Vector3 position;
        public float   yRotation;
        public float   health;
        public float   hunger;
    }

    [Serializable]
    public class InventorySaveData
    {
        public ItemStack[] hotbar;
        public ItemStack[] backpack;
        public int         selectedSlot;
    }

    [Serializable]
    public class BlueprintSaveData
    {
        public string              pieceId;
        public Vector3             position;
        public Quaternion          rotation;
        public List<DepositedItem> deposits;
    }

    [Serializable]
    public class BuildingPieceSaveData
    {
        public string    pieceId;
        public Vector3   position;
        public Quaternion rotation;
        public float     currentHealth;
    }

    [Serializable]
    public class WorldSaveData
    {
        public float timeOfDay;
    }

    // JsonUtility cannot serialize Dictionary<string,int>; use this list of pairs instead.
    [Serializable]
    public struct DepositedItem
    {
        public string key;
        public int    value;
    }
}
