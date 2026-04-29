using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ReinosDelEter
{
    public static class CardDataRenamer
    {
        private struct CardConfig
        {
            public string name;
            public ElementType element;
            public int attack;
            public int defense;
            public int energy;
            public CardEffectType effect;

            public CardConfig(string name, ElementType element, int attack, int defense, int energy, CardEffectType effect)
            {
                this.name = name;
                this.element = element;
                this.attack = attack;
                this.defense = defense;
                this.energy = energy;
                this.effect = effect;
            }
        }

        private static readonly CardConfig[] Configs = new CardConfig[]
        {
            new("Arquera del Bosque",    ElementType.Earth,  7,  5, 2, CardEffectType.None),
            new("Hechicera de Tormenta", ElementType.Air,    9,  3, 3, CardEffectType.DoubleDamage),
            new("Maga Carmesí",          ElementType.Fire,   8,  4, 2, CardEffectType.None),
            new("Señor del Trueno",      ElementType.Air,   11,  6, 3, CardEffectType.DoubleDamage),
            new("Guardiana Celestial",   ElementType.Water,  6,  9, 2, CardEffectType.Shield),
            new("Sombra Púrpura",        ElementType.Center,10,  5, 3, CardEffectType.StealCard),
            new("Dúo Sagrado",           ElementType.Earth,  7,  8, 3, CardEffectType.Heal),
            new("Cazador de Sombras",    ElementType.Fire,   9,  4, 2, CardEffectType.None),
            new("Arquera Élfica",        ElementType.Air,    8,  5, 2, CardEffectType.ExtraMove),
            new("Berserker Oscuro",      ElementType.Fire,  12,  2, 3, CardEffectType.DoubleDamage),
            new("Enredadera Ancestral",  ElementType.Earth,  6, 10, 2, CardEffectType.Shield),
            new("Árbol Dorado",          ElementType.Earth,  5, 11, 3, CardEffectType.Heal),
            new("Rayo Azul",             ElementType.Water, 10,  3, 3, CardEffectType.DoubleDamage),
            new("Ojo del Abismo",        ElementType.Center, 8,  6, 2, CardEffectType.StealCard),
            new("Acróbata Oscura",       ElementType.Center, 7,  5, 2, CardEffectType.ExtraMove),
            new("Guerrera del Rayo",     ElementType.Air,    9,  7, 3, CardEffectType.None),
        };

        [MenuItem("Reinos del Éter/2 - Renombrar y Asignar Stats")]
        public static void RenameAndAssignStats()
        {
            const string folder = "Assets/Cards/Data";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"[CardDataRenamer] No existe la carpeta: {folder}");
                return;
            }

            // Buscar todos los CardData en la carpeta
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { folder });
            if (guids.Length == 0)
            {
                Debug.LogError($"[CardDataRenamer] No se encontraron assets CardData en {folder}");
                return;
            }

            // Cargar y ordenar por nombre de archivo (Card_01, Card_02, ...)
            var assets = guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .OrderBy(p => Path.GetFileNameWithoutExtension(p), System.StringComparer.Ordinal)
                .Select(p => new { Path = p, Asset = AssetDatabase.LoadAssetAtPath<CardData>(p) })
                .Where(x => x.Asset != null)
                .ToList();

            int total = Mathf.Min(assets.Count, Configs.Length);
            int updated = 0;

            for (int i = 0; i < total; i++)
            {
                var card = assets[i].Asset;
                var cfg = Configs[i];

                card.cardName     = cfg.name;
                card.element      = cfg.element;
                card.attackPower  = cfg.attack;
                card.defensePower = cfg.defense;
                card.energyCost   = cfg.energy;
                card.effectType   = cfg.effect;

                EditorUtility.SetDirty(card);
                updated++;

                Debug.Log($"[CardDataRenamer] {Path.GetFileNameWithoutExtension(assets[i].Path)} -> {cfg.name} ({cfg.element}, ATK {cfg.attack}/DEF {cfg.defense}/E {cfg.energy}, {cfg.effect})");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (assets.Count > Configs.Length)
                Debug.LogWarning($"[CardDataRenamer] Hay {assets.Count - Configs.Length} cartas adicionales sin configurar.");
            else if (assets.Count < Configs.Length)
                Debug.LogWarning($"[CardDataRenamer] Faltan {Configs.Length - assets.Count} cartas para completar la lista.");

            Debug.Log($"✓ {updated} cartas renombradas y configuradas");
        }
    }
}
