using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ReinosDelEter
{
    /// <summary>
    /// Editor Script que crea automáticamente CardData assets basados en los Sprites
    /// encontrados en Assets/Cards/.
    /// 
    /// Uso: Menú → "Reinos del Éter/1 - Crear CardData Assets"
    /// </summary>
    public class CardAssetCreator
    {
        private const string MENU_PATH = "Reinos del Éter/1 - Crear CardData Assets";
        private const string SPRITES_FOLDER = "Assets/Cards";
        private const string DATA_FOLDER = "Assets/Cards/Data";

        [MenuItem(MENU_PATH)]
        public static void CreateCardDataAssets()
        {
            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("     CREAR CardData Assets desde Sprites");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // 1. Crear carpeta Assets/Cards/Data/ si no existe
            if (!System.IO.Directory.Exists(DATA_FOLDER))
            {
                string[] folderPath = DATA_FOLDER.Split('/');
                string parentFolder = folderPath[0] + "/" + folderPath[1];
                AssetDatabase.CreateFolder(parentFolder, folderPath[2]);
                Debug.Log($"✓ Carpeta creada: {DATA_FOLDER}");
            }

            // 2. Buscar todos los Sprites en Assets/Cards/
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { SPRITES_FOLDER });
            List<Sprite> sprites = new List<Sprite>();

            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning("⚠️  No se encontraron Sprites en " + SPRITES_FOLDER);
                return;
            }

            Debug.Log($"✓ Encontrados {sprites.Count} Sprites\n");

            // 3. Crear CardData para cada Sprite
            int createdCount = 0;
            ElementType[] elements = new ElementType[] 
            { 
                ElementType.Fire, 
                ElementType.Water, 
                ElementType.Earth, 
                ElementType.Air, 
                ElementType.Center 
            };

            for (int i = 0; i < sprites.Count; i++)
            {
                Sprite sprite = sprites[i];
                string cardName = SanitizeCardName(sprite.name);

                // Crear CardData
                CardData cardData = ScriptableObject.CreateInstance<CardData>();
                cardData.cardName = cardName;
                cardData.cardArt = sprite;
                cardData.element = elements[i % elements.Length];
                cardData.description = $"Carta de {cardData.cardName}";
                cardData.attackPower = Random.Range(3, 13);
                cardData.defensePower = Random.Range(2, 9);
                cardData.energyCost = Random.Range(1, 4);

                // Guardar asset
                string assetPath = $"{DATA_FOLDER}/Card_{(i + 1):D2}.asset";
                AssetDatabase.CreateAsset(cardData, assetPath);
                createdCount++;

                Debug.Log($"  [{i + 1:D2}] {cardName.PadRight(20)} [{cardData.element}] " +
                         $"ATK:{cardData.attackPower} DEF:{cardData.defensePower} Cost:{cardData.energyCost}");
            }

            // 4. Guardar cambios
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"\n✅ {createdCount} CardData assets creados en {DATA_FOLDER}");
            Debug.Log("═══════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Limpia el nombre del sprite para usarlo como cardName.
        /// </summary>
        private static string SanitizeCardName(string spriteName)
        {
            string name = spriteName;

            // Remover prefijo "Generated Image" si existe
            if (name.StartsWith("Generated Image April"))
            {
                // Extraer solo la parte final (hora)
                string[] parts = name.Split('-');
                if (parts.Length > 1)
                {
                    name = parts[parts.Length - 1].Trim();
                    // Remover "(1)" u otros números entre paréntesis
                    name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(\d+\)", "");
                }
                else
                {
                    name = "Card " + (System.DateTime.Now.Ticks % 1000);
                }
            }

            // Si sigue siendo muy genérico, usar nombre del archivo
            if (string.IsNullOrEmpty(name) || name.Length < 3)
            {
                name = "Carta " + System.Guid.NewGuid().ToString().Substring(0, 8);
            }

            return name;
        }
    }
}
