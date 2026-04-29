using UnityEngine;
using UnityEditor;

namespace ReinosDelEter
{
    /// <summary>
    /// Configura automáticamente los PNGs en Assets/Cards/ como Sprites
    /// y los asigna a las cartas.
    /// </summary>
    public class CardImageSetup
    {
        [MenuItem("Reinos del Éter/Setup Card Images")]
        public static void SetupCardImages()
        {
            Debug.Log("\n[CardImageSetup] ════════════════════════════════════════");
            Debug.Log("[CardImageSetup] Configurando imágenes de cartas...\n");

            // 1. Buscar todos los PNGs en Assets/Cards/
            string cardsFolder = "Assets/Cards";
            string[] pngGuids = AssetDatabase.FindAssets("*.png", new[] { cardsFolder });
            
            Debug.Log($"[CardImageSetup] Encontrados {pngGuids.Length} PNGs en {cardsFolder}");

            int configuredCount = 0;

            // 2. Configurar cada PNG como Sprite
            foreach (string guid in pngGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Configurar como Sprite
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.mipmapEnabled = false;
                        importer.filterMode = FilterMode.Bilinear;
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        Debug.Log($"✓ Configurado como Sprite: {System.IO.Path.GetFileName(path)}");
                        configuredCount++;
                    }
                }
            }

            // 3. Recargar base de datos
            AssetDatabase.Refresh();
            
            Debug.Log($"\n[CardImageSetup] {configuredCount} PNGs configurados como Sprites");

            // 4. Ahora asignar a cartas
            AssignSpritesToCards();
            
            Debug.Log("[CardImageSetup] ════════════════════════════════════════\n");
        }

        private static void AssignSpritesToCards()
        {
            Debug.Log("[CardImageSetup] Asignando sprites a cartas...\n");

            // 1. Buscar todos los CardData
            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            System.Collections.Generic.List<CardData> cards = new System.Collections.Generic.List<CardData>();

            foreach (string guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                    cards.Add(card);
            }

            cards.Sort((a, b) => a.cardName.CompareTo(b.cardName));

            // 2. Buscar todos los sprites
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Cards" });
            System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();

            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            sprites.Sort((a, b) => a.name.CompareTo(b.name));

            Debug.Log($"Encontrados: {sprites.Count} sprites para {cards.Count} cartas\n");

            if (sprites.Count == 0)
            {
                Debug.LogError("ERROR: No se encontraron sprites. Verifica que los PNGs estén en Assets/Cards/");
                return;
            }

            // 3. Asignar sprites a cartas
            int assigned = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                Sprite sprite = sprites[i % sprites.Count];

                if (card.cardArt != sprite)
                {
                    card.cardArt = sprite;
                    EditorUtility.SetDirty(card);
                    assigned++;
                    Debug.Log($"✓ {card.cardName.PadRight(20)} ← {sprite.name}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"\n✅ {assigned} cartas asignadas.");
        }
    }
}
