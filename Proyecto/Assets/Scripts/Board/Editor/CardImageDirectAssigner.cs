using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ReinosDelEter
{
    /// <summary>
    /// Asignación simple de sprites a cartas en orden.
    /// No depende de nombres, solo asigna en orden secuencial.
    /// </summary>
    public class CardImageDirectAssigner
    {
        [MenuItem("Reinos del Éter/Assign Sprites by Order")]
        public static void AssignByOrder()
        {
            Debug.Log("[CardImageDirectAssigner] Asignando sprites por orden...\n");

            // 1. Buscar todos los CardData
            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            List<CardData> cards = new List<CardData>();

            foreach (string guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                    cards.Add(card);
            }

            cards.Sort((a, b) => a.cardName.CompareTo(b.cardName));

            // 2. Buscar todos los sprites en Assets/Cards/
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Cards" });
            List<Sprite> sprites = new List<Sprite>();

            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            sprites.Sort((a, b) => a.name.CompareTo(b.name));

            Debug.Log($"Encontradas {sprites.Count} imágenes para {cards.Count} cartas\n");

            if (sprites.Count == 0)
            {
                Debug.LogError("No hay sprites en Assets/Cards/");
                return;
            }

            // 3. Asignar sprites a cartas
            int assigned = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                Sprite sprite = sprites[i % sprites.Count]; // Round-robin

                card.cardArt = sprite;
                EditorUtility.SetDirty(card);
                assigned++;

                Debug.Log($"✓ {card.cardName.PadRight(20)} ← {sprite.name}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"\n✅ {assigned} cartas asignadas correctamente.");
        }
    }
}
