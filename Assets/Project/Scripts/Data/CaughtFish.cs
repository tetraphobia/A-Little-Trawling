using System;
using UnityEngine;

namespace LittleTrawling.Data
{
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
        public string timestamp;

        public CaughtFish(Fish species, float sizeCm, float weightKg, int sellPrice)
        {
            this.species = species;
            this.sizeCm = sizeCm;
            this.weightKg = weightKg;
            this.sellPrice = sellPrice;
            this.timestamp = DateTime.Now.ToString("g");
        }
    }
}
