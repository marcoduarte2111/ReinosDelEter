using UnityEngine;
using System.Collections.Generic;

namespace ReinosDelEter
{
    /// <summary>
    /// Verifica el estado de sprites en CardData.
    /// Muestra qué cartas tienen arte y cuáles no.
    /// </summary>
    public class CardArtVerifier : MonoBehaviour
    {
        [ContextMenu("Verify Card Art")]
        public void VerifyCardArt()
        {
            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("     Card Art Verification");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // Buscar todos los CardData
            string[] cardGuids = UnityEditor.AssetDatabase.FindAssets("t:CardData");
            Dictionary<string, int> withArt = new(), withoutArt = new();

            foreach (string guid in cardGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (card == null) continue;

                if (card.HasArt)
                {
                    Debug.Log($"✓ {card.cardName.PadRight(20)} → {card.cardArt.name}");
                    if (!withArt.ContainsKey(card.element.ToString()))
                        withArt[card.element.ToString()] = 0;
                    withArt[card.element.ToString()]++;
                }
                else
                {
                    Debug.LogWarning($"✗ {card.cardName.PadRight(20)} → SIN ARTE");
                    if (!withoutArt.ContainsKey(card.element.ToString()))
                        withoutArt[card.element.ToString()] = 0;
                    withoutArt[card.element.ToString()]++;
                }
            }

            Debug.Log($"\n📊 RESUMEN:");
            Debug.Log($"   Total CardData: {cardGuids.Length}");
            Debug.Log($"   Con arte: {cardGuids.Length - withoutArt.Count}");
            Debug.Log($"   Sin arte: {withoutArt.Count}");

            if (withoutArt.Count > 0)
            {
                Debug.LogWarning($"\n⚠️  {withoutArt.Count} cartas sin arte. Ejecuta:");
                Debug.LogWarning("   1. Reinos del Éter/Auto-Assign Card Art");
                Debug.LogWarning("   2. O asigna manualmente en los ScriptableObjects");
            }

            Debug.Log("\n═══════════════════════════════════════════════════════\n");
        }
    }
}
