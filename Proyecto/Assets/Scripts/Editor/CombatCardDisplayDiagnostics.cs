using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using ReinosDelEter;

public static class CombatCardDisplayDiagnostics
{
    /// <summary>Recolecta todas las cartas de DeckManager.</summary>
    private static List<CardData> GetAllDeckCards(DeckManager dm)
    {
        var all = new List<CardData>();
        if (dm.waterCards != null) all.AddRange(dm.waterCards);
        if (dm.fireCards  != null) all.AddRange(dm.fireCards);
        if (dm.earthCards != null) all.AddRange(dm.earthCards);
        if (dm.airCards   != null) all.AddRange(dm.airCards);
        return all;
    }

    [MenuItem("Tools/Debug/Verificar Sistema de Cartas en Combate")]
    public static void CheckCombatCardDisplay()
    {
        Debug.Log("==========================================================");
        Debug.Log("DIAGNOSTICO: Sistema de Visualizacion de Cartas en Combate");
        Debug.Log("==========================================================");

        // 1. HUDController
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud == null)
        {
            Debug.LogError("HUDController NO ENCONTRADO en la escena");
            return;
        }
        Debug.Log("OK HUDController encontrado");

        // 2. combatPanel
        if (hud.combatPanel == null)
        {
            Debug.LogError("combatPanel es NULL");
            return;
        }
        Debug.Log($"OK combatPanel existe: {hud.combatPanel.name}");
        Debug.Log($"   - Activo: {hud.combatPanel.activeInHierarchy}");

        // 3. attackerCardDisplay
        if (hud.attackerCardDisplay == null)
        {
            Debug.LogError("attackerCardDisplay es NULL");
        }
        else
        {
            Debug.Log($"OK attackerCardDisplay: {hud.attackerCardDisplay.GetType().Name} en '{hud.attackerCardDisplay.gameObject.name}'");
        }

        // 4. defenderCardDisplay
        if (hud.defenderCardDisplay == null)
        {
            Debug.LogError("defenderCardDisplay es NULL");
        }
        else
        {
            Debug.Log($"OK defenderCardDisplay: {hud.defenderCardDisplay.GetType().Name} en '{hud.defenderCardDisplay.gameObject.name}'");
        }

        // 5. DeckManager
        DeckManager dm = Object.FindFirstObjectByType<DeckManager>();
        if (dm == null)
        {
            Debug.LogError("DeckManager NO ENCONTRADO");
        }
        else
        {
            var allCards = GetAllDeckCards(dm);
            Debug.Log($"OK DeckManager encontrado con {allCards.Count} cartas");

            int cardsWithSprites = 0;
            foreach (var card in allCards)
            {
                if (card != null && card.HasArt && card.cardArt != null)
                    cardsWithSprites++;
            }
            Debug.Log($"   - Cartas con sprites: {cardsWithSprites}/{allCards.Count}");

            if (cardsWithSprites == 0)
                Debug.LogWarning("NINGUNA carta tiene sprite asignado");
        }

        // 6. GameManager
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("GameManager.Instance es NULL (normal si no esta jugando)");
        }
        else
        {
            Debug.Log($"OK GameManager activo con {gm.Players.Count} jugadores");

            for (int i = 0; i < gm.Players.Count; i++)
            {
                var player = gm.Players[i];
                int handCount = player.hand?.Count ?? 0;
                Debug.Log($"   - {player.playerName}: {handCount} cartas en mano");

                if (player.hand != null)
                {
                    int withSprites = 0;
                    foreach (var card in player.hand)
                    {
                        if (card != null && card.HasArt && card.cardArt != null)
                            withSprites++;
                    }
                    Debug.Log($"     -> Con sprites: {withSprites}/{handCount}");
                }
            }
        }

        // 7. CombatManager
        CombatManager cm = Object.FindFirstObjectByType<CombatManager>();
        if (cm == null)
            Debug.LogWarning("CombatManager NO ENCONTRADO (normal si no hay combate activo)");
        else
            Debug.Log("OK CombatManager existe");

        Debug.Log("==========================================================");
    }

    [MenuItem("Tools/Debug/Mostrar Cartas del DeckManager")]
    public static void ListDeckManagerCards()
    {
        Debug.Log("=========== CARTAS EN DECKMANAGER ===========");

        DeckManager dm = Object.FindFirstObjectByType<DeckManager>();
        if (dm == null)
        {
            Debug.LogWarning("DeckManager no existe");
            return;
        }

        var allCards = GetAllDeckCards(dm);
        if (allCards.Count == 0)
        {
            Debug.LogWarning("DeckManager vacio");
            return;
        }

        for (int i = 0; i < allCards.Count; i++)
        {
            var card = allCards[i];
            if (card == null) { Debug.Log($"  {i+1:00}. NULL"); continue; }
            string sprite = card.HasArt && card.cardArt != null ? "[V]" : "[X]";
            Debug.Log($"  {i+1:00}. {sprite} {card.cardName,-20} | ATK:{card.attackPower:00} DEF:{card.defensePower:00} | {card.element}");
        }

        Debug.Log($"Total: {allCards.Count} cartas");

        int withSprites = 0;
        var elements = new Dictionary<ElementType, int>();

        foreach (var card in allCards)
        {
            if (card == null) continue;
            if (card.HasArt && card.cardArt != null)
                withSprites++;

            if (!elements.ContainsKey(card.element))
                elements[card.element] = 0;
            elements[card.element]++;
        }

        Debug.Log($"Con sprites: {withSprites}/{allCards.Count}");
        Debug.Log("Por elemento:");
        foreach (var kvp in elements)
            Debug.Log($"  - {kvp.Key}: {kvp.Value}");
    }

    [MenuItem("Tools/Debug/Prueba: Mostrar Carta en Panel")]
    public static void TestDisplayCard()
    {
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        DeckManager dm = Object.FindFirstObjectByType<DeckManager>();

        if (hud == null || dm == null)
        {
            Debug.LogError("HUDController o DeckManager no existen");
            return;
        }

        var allCards = GetAllDeckCards(dm);
        if (allCards.Count == 0)
        {
            Debug.LogError("DeckManager vacio");
            return;
        }

        // Buscar primera carta con sprite
        CardData testCard = null;
        foreach (var card in allCards)
        {
            if (card != null && card.HasArt && card.cardArt != null)
            {
                testCard = card;
                break;
            }
        }

        if (testCard == null)
        {
            Debug.LogWarning("No hay cartas con sprites para probar - usando primera carta");
            testCard = allCards[0];
            if (testCard == null) return;
        }

        var tempPlayer = new PlayerData { playerName = "Test" };

        Debug.Log($"Probando DisplayCombatCard con: {testCard.cardName}");
        hud.ShowCombatPanel(tempPlayer, tempPlayer);
        hud.DisplayCombatCard(testCard, tempPlayer, true);
        hud.DisplayCombatCard(testCard, tempPlayer, false);

        Debug.Log("Prueba completada - revisa los paneles de ataque/defensa");
    }
}
