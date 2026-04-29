using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ReinosDelEter
{
    /// <summary>
    /// Script que carga automáticamente los CardData creados por CardAssetCreator
    /// y los asigna al DeckManager.
    /// </summary>
    public class CardDataAssignerToDeck
    {
        [MenuItem("Reinos del Éter/2 - Asignar Cards a DeckManager")]
        public static void AssignCardsToDeck()
        {
            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("     Asignar CardData Assets a DeckManager");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // 1. Buscar DeckManager en la escena
            DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
            if (deckManager == null)
            {
                Debug.LogError("✗ DeckManager no encontrado en la escena");
                return;
            }

            // 2. Buscar todos los CardData en Assets/Cards/Data/
            string[] cardGuids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Cards/Data" });
            
            if (cardGuids.Length == 0)
            {
                Debug.LogWarning("⚠️  No se encontraron CardData en Assets/Cards/Data/");
                Debug.LogWarning("    Ejecuta: Reinos del Éter/1 - Crear CardData Assets");
                return;
            }

            Debug.Log($"✓ Encontrados {cardGuids.Length} CardData assets\n");

            // 3. Separar por elemento
            List<CardData> waterList = new List<CardData>();
            List<CardData> fireList = new List<CardData>();
            List<CardData> earthList = new List<CardData>();
            List<CardData> airList = new List<CardData>();
            List<CardData> centerList = new List<CardData>();

            foreach (string guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (card != null)
                {
                    switch (card.element)
                    {
                        case ElementType.Water:
                            waterList.Add(card);
                            break;
                        case ElementType.Fire:
                            fireList.Add(card);
                            break;
                        case ElementType.Earth:
                            earthList.Add(card);
                            break;
                        case ElementType.Air:
                            airList.Add(card);
                            break;
                        case ElementType.Center:
                            centerList.Add(card);
                            break;
                    }
                }
            }

            // 4. Asignar a DeckManager
            deckManager.waterCards = waterList.ToArray();
            deckManager.fireCards = fireList.ToArray();
            deckManager.earthCards = earthList.ToArray();
            deckManager.airCards = airList.ToArray();

            // Marcar como dirty
            EditorUtility.SetDirty(deckManager);
            EditorUtility.SetDirty(deckManager.gameObject);
            AssetDatabase.SaveAssets();

            Debug.Log($"  Water: {waterList.Count}");
            Debug.Log($"  Fire: {fireList.Count}");
            Debug.Log($"  Earth: {earthList.Count}");
            Debug.Log($"  Air: {airList.Count}");
            Debug.Log($"  Center: {centerList.Count}");

            Debug.Log($"\n✅ CardData assets asignados a DeckManager");
            Debug.Log("═══════════════════════════════════════════════════════\n");
        }
    }
}
