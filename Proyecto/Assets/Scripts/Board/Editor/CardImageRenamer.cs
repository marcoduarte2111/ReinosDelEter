using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace ReinosDelEter
{
    /// <summary>
    /// Editor Script para renombrar automáticamente los PNGs de cartas.
    /// Asigna nombres basados en los nombres de CardData.
    /// </summary>
    public class CardImageRenamer
    {
        [MenuItem("Reinos del Éter/Rename Card Images")]
        public static void RenameCardImages()
        {
            Debug.Log("[CardImageRenamer] Iniciando renombrado de imágenes...\n");

            // 1. Buscar todos los CardData
            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            List<string> cardNames = new List<string>();

            foreach (string guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                    cardNames.Add(card.cardName);
            }

            // 2. Buscar todos los PNGs en Assets/Cards/
            string[] imageGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Cards" });
            List<string> imagePaths = new List<string>();

            foreach (string guid in imageGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".png"))
                    imagePaths.Add(path);
            }

            Debug.Log($"Encontradas {imagePaths.Count} imágenes y {cardNames.Count} cartas");

            // 3. Renombrar imágenes con nombres de cartas
            for (int i = 0; i < imagePaths.Count && i < cardNames.Count; i++)
            {
                string oldPath = imagePaths[i];
                string fileName = Path.GetFileNameWithoutExtension(oldPath);
                string directory = Path.GetDirectoryName(oldPath);
                string extension = Path.GetExtension(oldPath);

                // Crear nombre seguro para archivo
                string newFileName = SanitizeFileName(cardNames[i]) + extension;
                string newPath = Path.Combine(directory, newFileName).Replace("\\", "/");

                // Evitar sobrescribir
                if (newPath != oldPath && !File.Exists(newPath))
                {
                    AssetDatabase.RenameAsset(oldPath, newFileName.Replace(".png", ""));
                    Debug.Log($"✓ Renombrado: {fileName} → {newFileName}");
                }
                else if (newPath == oldPath)
                {
                    Debug.Log($"⊘ {fileName} ya tiene el nombre correcto");
                }
                else
                {
                    Debug.LogWarning($"! {newFileName} ya existe, saltando");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("\n✅ Renombrado completado. Ahora ejecuta 'Auto-Assign Card Art'");
        }

        private static string SanitizeFileName(string name)
        {
            // Reemplazar caracteres no seguros
            string safe = name
                .Replace(" ", "_")
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("-", "_")
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U");

            // Remover caracteres especiales
            var sb = new System.Text.StringBuilder();
            foreach (char c in safe)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
