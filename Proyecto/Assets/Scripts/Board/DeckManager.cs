using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// DeckManager — crea y reparte 8 cartas a cada jugador según su elemento.
    ///
    /// CÓMO FUNCIONA:
    ///   • Asigna cardsByElement[0..3] con tus CardData assets en el Inspector
    ///   • Si no asignas nada, genera cartas placeholder proceduralmente
    ///   • El reparto mezcla aleatoriamente el pool del elemento del jugador
    /// </summary>
    public class DeckManager : MonoBehaviour
    {
        [Header("Pools de cartas por elemento")]
        [Tooltip("Arrastra tus CardData assets aquí. Index 0=Water 1=Fire 2=Earth 3=Air")]
        public CardData[] waterCards;
        public CardData[] fireCards;
        public CardData[] earthCards;
        public CardData[] airCards;

        public const int CardsPerPlayer = 8;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Retorna una mano de 8 cartas para el elemento dado.</summary>
        public List<CardData> DealHand(ElementType element)
        {
            CardData[] pool = GetPool(element);

            // Si no hay assets asignados → genera placeholders
            if (pool == null || pool.Length == 0)
                return GeneratePlaceholderHand(element);

            return PickRandom(pool, CardsPerPlayer);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private CardData[] GetPool(ElementType element) => element switch
        {
            ElementType.Water => waterCards,
            ElementType.Fire => fireCards,
            ElementType.Earth => earthCards,
            ElementType.Air => airCards,
            _ => waterCards
        };

        private List<CardData> PickRandom(CardData[] pool, int count)
        {
            List<CardData> shuffled = new(pool);
            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            List<CardData> hand = new();
            for (int i = 0; i < count; i++)
                hand.Add(shuffled[i % shuffled.Count]); // repite si el pool es pequeño

            return hand;
        }

        /// <summary>
        /// Genera 8 cartas placeholder si no hay assets.
        /// Cuando tengas tus sprites solo asígnalos en el Inspector
        /// y este método deja de usarse.
        /// </summary>
        private List<CardData> GeneratePlaceholderHand(ElementType element)
        {
            // Nombres y stats temáticos por elemento
            var templates = GetTemplates(element);
            List<CardData> hand = new();

            for (int i = 0; i < CardsPerPlayer; i++)
            {
                CardData cd = ScriptableObject.CreateInstance<CardData>();
                var tmpl = templates[i % templates.Length];
                cd.name = tmpl.name;
                cd.cardName = tmpl.name;
                cd.description = tmpl.desc;
                cd.element = element;
                cd.attackPower = tmpl.atk;
                cd.defensePower = tmpl.def;
                cd.energyCost = tmpl.cost;
                cd.effectType = tmpl.effect;
                cd.effectValue = tmpl.effectVal;
                // cardArt = null → placeholder de color automático en UI
                hand.Add(cd);
            }
            return hand;
        }

        // ── Plantillas temáticas por elemento ────────────────────────────────
        private class Template
        {
            public string name;
            public string desc;
            public int atk, def, cost, effectVal;
            public CardEffectType effect;
            public Template(string name, string desc, int atk, int def, int cost,
                            CardEffectType effect = CardEffectType.None, int effectVal = 0)
            {
                this.name = name; this.desc = desc; this.atk = atk;
                this.def = def; this.cost = cost; this.effect = effect;
                this.effectVal = effectVal;
            }
        }

        private Template[] GetTemplates(ElementType el) => el switch
        {
            ElementType.Water => new Template[]
            {
                new("Ola Fría",      "Congela al enemigo",       6, 4, 1),
                new("Marejada",      "Empuja al rival atrás",    8, 2, 2),
                new("Escudo Hielo",  "Bloquea el próximo golpe", 2, 9, 1, CardEffectType.Shield, 5),
                new("Tormenta",      "Daño en área",             7, 3, 2),
                new("Neblina",       "Reduce precisión rival",   3, 6, 1),
                new("Tsunami",       "Golpe devastador",        10, 1, 3),
                new("Curación",      "Recupera energía",         2, 5, 1, CardEffectType.Heal, 3),
                new("Corriente",     "Ataque rápido doble",      5, 4, 2),
            },
            ElementType.Fire => new Template[]
            {
                new("Llamarada",     "Quema al enemigo",         8, 2, 1),
                new("Explosión",     "Daño masivo",             11, 1, 3),
                new("Escudo Fuego",  "Refleja parte del daño",   3, 7, 1, CardEffectType.Shield, 3),
                new("Meteoro",       "Impacto devastador",      10, 2, 2),
                new("Ascua",         "Ataque ágil",              5, 3, 1),
                new("Inferno",       "Doble daño este turno",    6, 2, 2, CardEffectType.DoubleDamage, 0),
                new("Fénix",         "Revive con poca vida",     4, 6, 2, CardEffectType.Heal, 4),
                new("Dragón Rojo",   "Señor del fuego",          9, 4, 3),
            },
            ElementType.Earth => new Template[]
            {
                new("Terremoto",     "Sacude el terreno",        7, 5, 2),
                new("Muro Pétreo",   "Defensa máxima",           1,12, 1, CardEffectType.Shield, 8),
                new("Roca Caída",    "Aplasta al rival",         8, 3, 1),
                new("Enredadera",    "Inmoviliza un turno",      4, 6, 1),
                new("Golem",         "Gigante de roca",          7, 8, 3),
                new("Espinas",       "Contraataca al recibir",   3, 7, 1, CardEffectType.Shield, 4),
                new("Avalancha",     "Golpe masivo",            10, 2, 2),
                new("Raíz Antigua",  "Roba una carta rival",     3, 5, 2, CardEffectType.StealCard, 0),
            },
            ElementType.Air => new Template[]
            {
                new("Torbellino",    "Gira al enemigo",          6, 4, 1),
                new("Rayo",          "Velocidad máxima",         9, 1, 2),
                new("Nube Densa",    "Confunde al rival",        3, 7, 1),
                new("Vendaval",      "Empuja 2 casillas",        5, 4, 1, CardEffectType.ExtraMove, 2),
                new("Escudo Viento", "Desvia ataques",           2, 9, 2, CardEffectType.Shield, 6),
                new("Filo Aire",     "Cortante invisible",       8, 3, 2),
                new("Tifón",         "Tormenta perfecta",       10, 2, 3),
                new("Pluma Sagrada", "Curación del cielo",       2, 5, 1, CardEffectType.Heal, 5),
            },
            _ => new Template[]
            {
                new("Carta Neutra",  "Sin elemento",             5, 5, 1),
            }
        };
    }
}