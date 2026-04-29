using UnityEngine;
using UnityEngine.UI;

namespace ReinosDelEter
{
    /// <summary>
    /// Diagnostics — revisa el estado del sistema de cartas.
    /// Adjunta a cualquier GameObject y chequea automáticamente en Start().
    /// </summary>
    public class CardSystemDiagnostics : MonoBehaviour
    {
        private void Start()
        {
            CheckCardSystem();
        }

        [ContextMenu("Check Card System")]
        public void CheckCardSystem()
        {
            Debug.Log("\n═══════════════════════════════════════════════════════");
            Debug.Log("     COMPLETE CARD SYSTEM DIAGNOSTICS");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            // 1. GameManager
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            Debug.Log($"[1] GameManager: {(gm != null ? "✓ Found" : "✗ NOT FOUND")}");
            if (gm != null)
            {
                Debug.Log($"     Players: {gm.Players?.Count ?? 0}");
                if (gm.Players?.Count > 0)
                {
                    PlayerData pd = gm.CurrentPlayer;
                    Debug.Log($"\n     Current Player: {pd.playerName} ({pd.ElementName})");
                    Debug.Log($"     Hand size: {pd.hand?.Count ?? 0}");
                    
                    if (pd.hand != null && pd.hand.Count > 0)
                    {
                        Debug.Log($"\n     Cards in hand:");
                        for (int i = 0; i < Mathf.Min(5, pd.hand.Count); i++)
                        {
                            CardData card = pd.hand[i];
                            string artStatus = card.HasArt 
                                ? $"✓ {card.cardArt.name}" 
                                : "✗ NO ART";
                            Debug.Log($"       [{i}] {card.cardName} - {artStatus}");
                        }
                    }
                    else
                    {
                        Debug.LogError("     ✗✗✗ CRITICAL: Hand is empty or null! ✗✗✗");
                    }
                }
            }

            // 2. DeckManager
            Debug.Log($"\n[2] DeckManager:");
            DeckManager dm = Object.FindFirstObjectByType<DeckManager>();
            if (dm != null)
            {
                Debug.Log($"     • waterCards: {dm.waterCards?.Length ?? 0}");
                Debug.Log($"     • fireCards: {dm.fireCards?.Length ?? 0}");
                Debug.Log($"     • earthCards: {dm.earthCards?.Length ?? 0}");
                Debug.Log($"     • airCards: {dm.airCards?.Length ?? 0}");
            }
            else
            {
                Debug.LogWarning("     ✗ DeckManager NOT FOUND");
            }

            // 3. HUDController
            Debug.Log($"\n[3] HUDController:");
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                Debug.Log($"     • handContainer: {(hud.handContainer != null ? "✓" : "✗")}");
                Debug.Log($"     • cardSlotPrefab: {(hud.cardSlotPrefab != null ? "✓" : "✗")}");
                
                if (hud.handContainer != null)
                {
                    int childCount = hud.handContainer.childCount;
                    Debug.Log($"     • Slots created: {childCount}");
                    
                    if (childCount > 0)
                    {
                        for (int i = 0; i < Mathf.Min(3, childCount); i++)
                        {
                            Transform child = hud.handContainer.GetChild(i);
                            var cardSlot = child.GetComponent<CardSlotUI>();
                            var img = child.GetComponent<Image>();
                            Debug.Log($"       [{i}] {child.name}");
                            Debug.Log($"           - CardSlotUI: {(cardSlot != null ? "✓" : "✗")}");
                            Debug.Log($"           - Image: {(img != null ? "✓" : "✗")}");
                        }
                    }
                    else
                    {
                        Debug.LogError("     ✗✗✗ NO SLOTS CREATED ✗✗✗");
                    }
                }
                else
                {
                    Debug.LogError("     ✗✗✗ handContainer is NULL ✗✗✗");
                }
            }
            else
            {
                Debug.LogError("     ✗ HUDController NOT FOUND");
            }

            // 4. Canvas
            Debug.Log($"\n[4] Canvas:");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"     ✓ Found at: {canvas.gameObject.name}");
                Transform bb = canvas.transform.Find("BottomBar");
                if (bb != null)
                {
                    Debug.Log($"     ✓ BottomBar found");
                    Transform hc = bb.Find("HandContainer");
                    if (hc != null)
                    {
                        Debug.Log($"     ✓ HandContainer found");
                    }
                    else
                    {
                        Debug.LogError("     ✗ HandContainer NOT found");
                    }
                }
                else
                {
                    Debug.LogError("     ✗ BottomBar NOT found");
                }
            }
            else
            {
                Debug.LogError("     ✗✗✗ Canvas NOT FOUND ✗✗✗");
            }

            Debug.Log("\n═══════════════════════════════════════════════════════\n");
        }
    }
}
