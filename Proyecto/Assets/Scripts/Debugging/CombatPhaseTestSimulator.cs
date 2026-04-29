using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using ReinosDelEter;

/// <summary>
/// Script de prueba para simular combate y verificar visualización de cartas.
/// Adjuntar a cualquier GameObject en la escena y ejecutar desde Play mode.
/// </summary>
public class CombatPhaseTestSimulator : MonoBehaviour
{
    private bool _simulationRunning = false;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Box("PRUEBA DE VISUALIZACION DE CARTAS");

        if (!_simulationRunning)
        {
            if (GUILayout.Button("Iniciar Simulacion de Combate", GUILayout.Height(40)))
            {
                StartCoroutine(SimulateCombat());
            }
        }
        else
        {
            GUILayout.Label("Simulacion en progreso...");
        }

        if (GUILayout.Button("Diagnostico Rapido", GUILayout.Height(30)))
        {
            RunQuickDiagnostic();
        }

        GUILayout.EndArea();
    }

    /// <summary>Recolecta todas las cartas asignadas a DeckManager.</summary>
    private List<CardData> GetAllDeckCards(DeckManager dm)
    {
        var all = new List<CardData>();
        if (dm.waterCards != null) all.AddRange(dm.waterCards);
        if (dm.fireCards  != null) all.AddRange(dm.fireCards);
        if (dm.earthCards != null) all.AddRange(dm.earthCards);
        if (dm.airCards   != null) all.AddRange(dm.airCards);
        return all;
    }

    private IEnumerator SimulateCombat()
    {
        _simulationRunning = true;
        Debug.Log("=== INICIANDO SIMULACION DE COMBATE ===");

        GameManager gm = GameManager.Instance;
        DeckManager dm = Object.FindFirstObjectByType<DeckManager>();
        HUDController hud = Object.FindFirstObjectByType<HUDController>();

        if (gm == null || dm == null || hud == null)
        {
            Debug.LogError("Componentes necesarios no encontrados");
            _simulationRunning = false;
            yield break;
        }

        var allCards = GetAllDeckCards(dm);
        if (allCards.Count < 2)
        {
            Debug.LogError("Se necesitan al menos 2 cartas en DeckManager");
            _simulationRunning = false;
            yield break;
        }

        if (gm.Players.Count < 2)
        {
            Debug.LogError("Se necesitan al menos 2 jugadores");
            _simulationRunning = false;
            yield break;
        }

        var attacker = gm.Players[0];
        var defender = gm.Players[1];

        Debug.Log($"Atacante: {attacker.playerName}");
        Debug.Log($"Defensor: {defender.playerName}");
        Debug.Log($"Total de cartas en DeckManager: {allCards.Count}");

        // Asegurar que cada jugador tenga cartas
        if (attacker.hand == null || attacker.hand.Count == 0)
        {
            attacker.hand = new List<CardData> { allCards[0], allCards[allCards.Count / 2] };
        }
        if (defender.hand == null || defender.hand.Count == 0)
        {
            defender.hand = new List<CardData> { allCards[1] };
        }

        Debug.Log($"Mano del atacante: {attacker.hand.Count} cartas");
        Debug.Log($"Mano del defensor: {defender.hand.Count} cartas");

        yield return new WaitForSeconds(0.5f);

        // Mostrar panel de combate
        Debug.Log("Mostrando panel de combate...");
        hud.ShowCombatPanel(attacker, defender);
        yield return new WaitForSeconds(0.8f);

        // Simular selección del atacante
        Debug.Log($"FASE 1: {attacker.playerName} selecciona carta...");
        var selectedCard = attacker.hand[0];
        Debug.Log($"  -> Seleccionada: {selectedCard.cardName}");

        hud.DisplayCombatCard(selectedCard, attacker, true);
        hud.RemoveCardFromHandDisplay(attacker, selectedCard);
        yield return new WaitForSeconds(1f);

        // Simular selección del defensor
        Debug.Log($"FASE 2: {defender.playerName} selecciona carta...");
        var defCard = defender.hand[0];
        Debug.Log($"  -> Seleccionada: {defCard.cardName}");

        hud.DisplayCombatCard(defCard, defender, false);
        hud.RemoveCardFromHandDisplay(defender, defCard);
        yield return new WaitForSeconds(1.5f);

        // Simular resolución
        Debug.Log("RESOLUCION DEL COMBATE");
        int atkPower = selectedCard.attackPower;
        int defPower = defCard.defensePower;

        Debug.Log($"  ATK: {selectedCard.cardName} = {atkPower}");
        Debug.Log($"  DEF: {defCard.cardName} = {defPower}");

        bool atkWins = atkPower >= defPower;
        Debug.Log($"  RESULTADO: {(atkWins ? attacker.playerName + " GANA" : defender.playerName + " GANA")}");

        yield return new WaitForSeconds(1f);

        // Limpiar
        Debug.Log("Limpiando...");
        hud.RestoreCardInHandDisplay(attacker, selectedCard);
        hud.RestoreCardInHandDisplay(defender, defCard);
        hud.HideCombatPanel();

        yield return new WaitForSeconds(0.5f);

        Debug.Log("=== SIMULACION COMPLETADA ===");

        _simulationRunning = false;
    }

    private void RunQuickDiagnostic()
    {
        Debug.Log("============== DIAGNOSTICO RAPIDO ==============");

        var gm = GameManager.Instance;
        var dm = Object.FindFirstObjectByType<DeckManager>();
        var hud = Object.FindFirstObjectByType<HUDController>();
        var cm = Object.FindFirstObjectByType<CombatManager>();

        // GameManager
        if (gm != null)
            Debug.Log($"GameManager: {gm.Players.Count} jugadores, turno: {gm.TurnIndex}");
        else
            Debug.LogWarning("GameManager no encontrado");

        // DeckManager
        if (dm != null)
        {
            var allCards = GetAllDeckCards(dm);
            Debug.Log($"DeckManager: {allCards.Count} cartas (W:{dm.waterCards?.Length ?? 0} F:{dm.fireCards?.Length ?? 0} E:{dm.earthCards?.Length ?? 0} A:{dm.airCards?.Length ?? 0})");
        }
        else
        {
            Debug.LogWarning("DeckManager no encontrado");
        }

        // HUDController
        if (hud != null)
        {
            Debug.Log($"HUDController: Panel combate = {(hud.combatPanel != null ? "Si" : "No")}");
            if (hud.attackerCardDisplay != null)
                Debug.Log($"  - attackerCardDisplay: {hud.attackerCardDisplay.GetType().Name}");
            if (hud.defenderCardDisplay != null)
                Debug.Log($"  - defenderCardDisplay: {hud.defenderCardDisplay.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("HUDController no encontrado");
        }

        // CombatManager
        if (cm != null)
            Debug.Log("CombatManager presente");
        else
            Debug.LogWarning("CombatManager no activo (esperar hasta combate)");

        Debug.Log("================================================");
    }
}
