using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// CardData — ScriptableObject que define una carta.
    ///
    /// CÓMO AÑADIR TU ARTE:
    ///   1. Selecciona el asset CardData en Project
    ///   2. En el Inspector, arrastra tu imagen al campo "cardArt"
    ///   3. Listo — el HUD la muestra automáticamente
    ///
    /// Si cardArt es null, se usa el placeholder de color generado en código.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Reinos del Éter/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Identidad")]
        public string cardName = "Carta";
        public string description = "Descripción de la carta";
        public ElementType element = ElementType.Water;

        [Header("Arte — arrastra tu imagen aquí cuando la tengas")]
        public Sprite cardArt;          // null = placeholder automático
        public Sprite cardFrame;        // null = frame generado en código

        [Header("Stats de combate")]
        [Range(0, 20)] public int attackPower = 5;
        [Range(0, 20)] public int defensePower = 3;
        [Range(1, 3)] public int energyCost = 1;

        [Header("Efecto especial")]
        public CardEffectType effectType = CardEffectType.None;
        [Range(0, 10)] public int effectValue = 0;

        // ── Placeholder color por elemento ───────────────────────────────────
        public Color PlaceholderColor => element switch
        {
            ElementType.Water => new Color(0.2f, 0.5f, 1.0f),
            ElementType.Fire => new Color(1.0f, 0.3f, 0.1f),
            ElementType.Earth => new Color(0.2f, 0.7f, 0.2f),
            ElementType.Air => new Color(0.85f, 0.85f, 0.85f),
            ElementType.Center => new Color(0.5f, 0.2f, 0.9f),
            _ => Color.gray
        };

        public bool HasArt => cardArt != null;
    }

    public enum CardEffectType
    {
        None,
        Heal,           // Recupera vida
        DoubleDamage,   // Daño doble este turno
        Shield,         // Bloquea daño
        StealCard,      // Roba una carta del enemigo
        ExtraMove,      // Avanza casillas extra si ganas
    }
}