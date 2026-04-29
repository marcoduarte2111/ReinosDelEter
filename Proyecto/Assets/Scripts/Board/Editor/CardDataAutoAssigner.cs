using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ReinosDelEter
{
    /// <summary>
    /// Editor Script que auto-asigna arte de cartas (sprites) a los assets CardData.
    /// 
    /// Uso: En el menú de Unity, ve a "Reinos del Éter/Auto-Assign Card Art"
    /// </summary>
    public class CardDataAutoAssigner
    {
        private const string MENU_PATH = "Reinos del Éter/Auto-Assign Card Art";
        private const string CARD_DATA_SEARCH = "t:CardData";
        private const string CARDS_FOLDER = "Assets/Cards";

        [MenuItem(MENU_PATH)]
        public static void AutoAssignCardArt()
        {
            // 1. Buscar todos los CardData en Assets/
            string[] cardDataGuids = AssetDatabase.FindAssets(CARD_DATA_SEARCH);

            if (cardDataGuids.Length == 0)
            {
                Debug.LogWarning("No se encontraron assets CardData en el proyecto.");
                return;
            }

            Debug.Log($"[CardAutoAssign] Encontrados {cardDataGuids.Length} CardData");

            // 2. Obtener todos los sprites en Assets/Cards/ Y subcarpetas
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { CARDS_FOLDER });
            List<Sprite> availableSprites = new List<Sprite>();

            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    availableSprites.Add(sprite);
                    Debug.Log($"  ✓ Sprite encontrado: {sprite.name}");
                }
            }

            if (availableSprites.Count == 0)
            {
                Debug.LogWarning($"No se encontraron sprites en {CARDS_FOLDER}");
                Debug.LogWarning($"Crea una carpeta '{CARDS_FOLDER}' y coloca tus sprites PNG ahí.");
                return;
            }

            Debug.Log($"[CardAutoAssign] Encontrados {availableSprites.Count} sprites");

            // 3. Procesar cada CardData
            int successCount = 0;
            int spriteIndex = 0;

            foreach (string guid in cardDataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (cardData == null)
                    continue;

                // Si ya tiene arte, saltar
                if (cardData.HasArt)
                {
                    Debug.Log($"  ⊘ {cardData.cardName} ya tiene arte");
                    continue;
                }

                // 4. Intentar encontrar sprite por nombre (palabras clave del cardName)
                Sprite assignedSprite = FindSpriteByName(cardData.cardName, availableSprites);

                // Si no encuentra por nombre, asignar en orden (round-robin)
                if (assignedSprite == null)
                {
                    assignedSprite = availableSprites[spriteIndex % availableSprites.Count];
                    Debug.Log($"  • {cardData.cardName}: sin coincidencia exacta, usando {assignedSprite.name} (índice {spriteIndex})");
                    spriteIndex++;
                }
                else
                {
                    Debug.Log($"  ✓ {cardData.cardName}: encontrado {assignedSprite.name} por nombre");
                }

                // Asignar el sprite
                if (assignedSprite != null)
                {
                    cardData.cardArt = assignedSprite;
                    EditorUtility.SetDirty(cardData);
                    successCount++;
                }
            }

            // 5. Guardar cambios
            AssetDatabase.SaveAssets();
            Debug.Log($"\n✅ Auto-assign completado. {successCount}/{cardDataGuids.Length} cartas fueron asignadas exitosamente.");
        }

        /// <summary>
        /// Busca un sprite en la lista cuyo nombre contenga palabras clave del cardName (case-insensitive).
        /// </summary>
        private static Sprite FindSpriteByName(string cardName, List<Sprite> sprites)
        {
            if (string.IsNullOrEmpty(cardName))
                return null;

            // Dividir el cardName en palabras clave
            string[] keywords = cardName.ToLower().Split(' ');

            // Buscar el sprite que contenga más palabras clave coincidentes
            Sprite bestMatch = null;
            int bestMatchCount = 0;

            foreach (Sprite sprite in sprites)
            {
                string spriteName = sprite.name.ToLower();
                int matchCount = 0;

                foreach (string keyword in keywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && spriteName.Contains(keyword))
                    {
                        matchCount++;
                    }
                }

                // Si encontramos coincidencias y es mejor que la anterior, actualizar
                if (matchCount > bestMatchCount)
                {
                    bestMatchCount = matchCount;
                    bestMatch = sprite;
                }
            }

            return bestMatch;
        }
    }
}
