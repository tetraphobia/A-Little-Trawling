using System;
using UnityEngine;

namespace LittleTrawling.Data
{
    public enum LunkerStatus
    {
        Normal,
        Lunker,
        MegaLunker
    }

    /// <summary>
    /// Represents an individual caught fish item stored in player inventory.
    /// </summary>
    [Serializable]
    public class CaughtFish
    {
        public Fish species;
        public float sizeCm;
        public float weightKg;
        public int sellPrice;
        public LunkerStatus lunkerStatus;
        public string timestamp;

        public CaughtFish(Fish species, float sizeCm, float weightKg, int sellPrice, LunkerStatus lunkerStatus = LunkerStatus.Normal)
        {
            this.species = species;
            this.sizeCm = sizeCm;
            this.weightKg = weightKg;
            this.sellPrice = sellPrice;
            this.lunkerStatus = lunkerStatus;
            this.timestamp = DateTime.Now.ToString("g");
        }
    }
}
