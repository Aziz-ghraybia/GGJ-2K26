using UnityEngine;
using System.IO;

namespace GGJ.Controllers
{
    public class PlayerDataManager
    {
        public void SavePlayerData(PlayerData data)
        {
            string json = JsonUtility.ToJson(data, true); // 'true' for pretty print during development
            string path = Path.Combine(Application.persistentDataPath, "playerSave.json");
            File.WriteAllText(path, json);
        }

        public PlayerData LoadPlayerData()
        {
            string path = Path.Combine(Application.persistentDataPath, "playerSave.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                return data;
            }
            else
            {
                Debug.LogWarning("Save file not found! Using default data.");
                PlayerData newData = CreateDefaultData(); // Return default data if no save file exists
                SavePlayerData(newData); // Save default data for future use
                return newData;
            }
        }
        PlayerData CreateDefaultData()
        {
            return new PlayerData
            {
                sprintSpeed = 1,
                prayerUnlocked = false,
                slotNumbers = 2,
                dashesNumber = 0,
                wealth = 0,
                durabilityLevel = 1,
                castingLevel = 1,
                checkpointLevel = 0
            };
        }
    }
}