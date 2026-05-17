using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    public class PlayerData
    {
        public int index;
        public string playerName;
        public ElementType element;
        public Color color;
        public List<CardData> hand = new();
        public List<Piece> pieces = new();

        public int health = 20;
        public int maxHealth = 20;
        public int energy = 3;
        public int maxEnergy = 3;
        public int score = 0;

        public string ElementName => element switch
        {
            ElementType.Water => "Agua",
            ElementType.Fire => "Fuego",
            ElementType.Earth => "Tierra",
            ElementType.Air => "Aire",
            _ => "?"
        };

        public Color ElementColor => element switch
        {
            ElementType.Water => new Color(0.2f, 0.5f, 1.0f),
            ElementType.Fire => new Color(1.0f, 0.3f, 0.1f),
            ElementType.Earth => new Color(0.2f, 0.7f, 0.2f),
            ElementType.Air => new Color(0.85f, 0.85f, 0.85f),
            _ => Color.white
        };

        public bool IsAlive => health > 0;

        public void TakeDamage(int dmg) => health = Mathf.Max(0, health - dmg);
        public void Heal(int amount) => health = Mathf.Min(maxHealth, health + amount);
        public void RestoreEnergy() => energy = maxEnergy;

        public bool CanPlayCard(CardData card) => energy >= card.energyCost;
        public void SpendEnergy(CardData card) => energy = Mathf.Max(0, energy - card.energyCost);
        public void RemoveCard(CardData card) => hand.Remove(card);
    }
}