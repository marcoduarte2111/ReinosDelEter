using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReinosDelEter
{
    /// <summary>
    /// CombatManager — resuelve combates por cartas.
    /// Soporta combate grupal: N atacantes combinan fuerza vs 1 defensor.
    ///
    /// FLUJO:
    ///   1. StartGroupCombat(attackers, defender, callback)
    ///   2. Cada jugador elige una carta (5s timeout → auto-selecciona la mejor)
    ///   3. Atacantes combinan ATK total; defensor usa su carta
    ///   4. Animación de choque
    ///   5. Llama callback(attackerWon)
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [Header("Config")]
        public float cardSelectTimeout = 5f;
        public float elementBonus = 1.4f;

        // Runtime
        private List<Piece> _attackers;
        private Piece _defender;
        private Action<bool> _onResult;

        private CardData _defCard;
        private List<CardData> _atkCards = new();
        private bool _defSelected;
        private int _atkSelected;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Combate 1v1 — wraps StartGroupCombat.</summary>
        public void StartCombat(Piece attacker, Piece defender, Action<Piece, Piece> onFinished)
        {
            StartGroupCombat(
                new List<Piece> { attacker },
                defender,
                won => onFinished?.Invoke(won ? attacker : defender,
                                          won ? defender : attacker));
        }

        /// <summary>Combate grupal: N fichas atacantes vs 1 defensor.</summary>
        public void StartGroupCombat(List<Piece> attackers, Piece defender, Action<bool> onResult)
        {
            _attackers = attackers;
            _defender = defender;
            _onResult = onResult;
            _atkCards.Clear();
            _defCard = null;
            _defSelected = false;
            _atkSelected = 0;

            StartCoroutine(CombatCoroutine());
        }

        /// <summary>Llamado por HUD cuando el jugador hace click en una carta.</summary>
        public void OnCardSelectedByPlayer(CardData card, PlayerData owner)
        {
            if (_defender != null && owner.index == _defender.playerIndex && !_defSelected)
            {
                _defCard = card;
                _defSelected = true;
                GameManager.Instance?.Log($"{owner.playerName} juega: {card.cardName}");
                
                // Mostrar carta en el panel de combate
                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                hud?.DisplayCombatCard(_defCard, owner, false);
                
                // Remover carta de la mano temporalmente (se remueve definitivamente al terminar combate)
                hud?.RemoveCardFromHandDisplay(owner, _defCard);
                return;
            }

            if (_attackers != null)
            {
                for (int i = 0; i < _attackers.Count; i++)
                {
                    Piece a = _attackers[i];
                    if (owner.index == a.playerIndex && _atkCards.Count == i)
                    {
                        _atkCards.Add(card);
                        _atkSelected++;
                        GameManager.Instance?.Log($"{owner.playerName} juega: {card.cardName}");
                        
                        // Mostrar carta en el panel de combate
                        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                        hud?.DisplayCombatCard(card, owner, true);
                        
                        // Remover carta de la mano temporalmente
                        hud?.RemoveCardFromHandDisplay(owner, card);
                        return;
                    }
                }
            }
        }

        // ── Combat coroutine ─────────────────────────────────────────────────

        private IEnumerator CombatCoroutine()
        {
            GameManager gm = GameManager.Instance;
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();

            // ── Mostrar panel de combate ──────────────────────────────────────
            PlayerData atkPD = gm.Players[_attackers[0].playerIndex];
            PlayerData defPD = gm.Players[_defender.playerIndex];
            hud?.ShowCombatPanel(atkPD, defPD);
            yield return new WaitForSeconds(0.5f);

            // ── Fase 1: cada atacante elige su carta manualmente ──────────────
            for (int i = 0; i < _attackers.Count; i++)
            {
                if (i < _atkCards.Count) continue; // ya seleccionó
                var pd = gm.Players[_attackers[i].playerIndex];
                hud?.ShowCardSelectionForPlayer(pd, "ATACANTE");
                // Esperar hasta que el jugador haga click en una carta
                while (_atkCards.Count <= i) yield return null;
                hud?.HideCardSelection();
                yield return new WaitForSeconds(0.3f);
            }

            // ── Fase 2: defensor elige su carta manualmente ───────────────────
            if (!_defSelected)
            {
                hud?.ShowCardSelectionForPlayer(defPD, "DEFENSOR");
                while (!_defSelected) yield return null;
                hud?.HideCardSelection();
                yield return new WaitForSeconds(0.3f);
            }

            // ── Fase 2: animación ─────────────────────────────────────────────
            yield return StartCoroutine(PlayCombatAnimation(hud));
            yield return new WaitForSeconds(0.5f);

            // ── Fase 3: cálculo ───────────────────────────────────────────────
            float atkTotal = 0f;
            for (int i = 0; i < _attackers.Count; i++)
            {
                CardData card = _atkCards[i];
                ElementType ae = _attackers[i].element;
                ElementType de = _defender.element;
                float score = card.attackPower - (_defCard?.defensePower ?? 0) * 0.3f;
                if (Beats(ae, de)) score *= elementBonus;
                atkTotal += Mathf.Max(1f, score);
            }

            float defTotal = 0f;
            if (_defCard != null)
            {
                float score = _defCard.attackPower
                            - _atkCards[0].defensePower * 0.3f * _attackers.Count;
                ElementType de = _defender.element;
                // Defensor gana bonus si su elemento vence al primero de los atacantes
                if (Beats(de, _attackers[0].element)) score *= elementBonus;
                defTotal = Mathf.Max(1f, score);
            }

            bool attackerWon = atkTotal >= defTotal;
            gm.Log($"ATK total: {atkTotal:F0} vs DEF: {defTotal:F0} → {(attackerWon ? "Atacantes ganan" : "Defensor gana")}");

            // Aplica efectos de cartas
            if (attackerWon)
            {
                var winPD = gm.Players[_attackers[0].playerIndex];
                var losePD = gm.Players[_defender.playerIndex];
                int dmg = Mathf.RoundToInt(atkTotal * 0.5f);
                losePD.TakeDamage(dmg);
                winPD.score += 10;
                ApplyCardEffect(_atkCards[0], winPD, losePD);
            }
            else
            {
                var winPD = gm.Players[_defender.playerIndex];
                var losePD = gm.Players[_attackers[0].playerIndex];
                int dmg = Mathf.RoundToInt(defTotal * 0.5f);
                losePD.TakeDamage(dmg);
                winPD.score += 10;
                if (_defCard != null) ApplyCardEffect(_defCard, winPD, losePD);
            }

            // Consume cartas
            foreach (Piece a in _attackers)
            {
                int idx = _attackers.IndexOf(a);
                if (idx < _atkCards.Count && _atkCards[idx] != null)
                {
                    var pd = gm.Players[a.playerIndex];
                    pd.SpendEnergy(_atkCards[idx]);
                    pd.RemoveCard(_atkCards[idx]);
                }
            }
            if (_defCard != null)
            {
                var defenderPD = gm.Players[_defender.playerIndex];
                defenderPD.SpendEnergy(_defCard);
                defenderPD.RemoveCard(_defCard);
            }

            yield return new WaitForSeconds(0.4f);
            _onResult?.Invoke(attackerWon);
        }

        // ── Animación ─────────────────────────────────────────────────────────

        private IEnumerator PlayCombatAnimation(HUDController hud)
        {
            if (hud == null) { yield return new WaitForSeconds(1f); yield break; }

            RawImage atkImg = hud.attackerCardDisplay;
            RawImage defImg = hud.defenderCardDisplay;

            if (atkImg != null && _atkCards.Count > 0)
                atkImg.texture = hud.GetCardTexture(_atkCards[0]);
            if (defImg != null)
                defImg.texture = hud.GetCardTexture(_defCard);

            // Acercamiento
            Vector2 atkStart = atkImg?.rectTransform.anchoredPosition ?? Vector2.zero;
            Vector2 defStart = defImg?.rectTransform.anchoredPosition ?? Vector2.zero;

            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                float p = t / 0.5f;
                if (atkImg != null) atkImg.rectTransform.anchoredPosition =
                    Vector2.Lerp(atkStart, atkStart * 0.5f, p);
                if (defImg != null) defImg.rectTransform.anchoredPosition =
                    Vector2.Lerp(defStart, defStart * 0.5f, p);
                yield return null;
            }

            // Flash de choque
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                float brightness = 1f + Mathf.Sin(t / 0.25f * Mathf.PI) * 2f;
                if (atkImg != null) atkImg.color = Color.white * brightness;
                if (defImg != null) defImg.color = Color.white * brightness;
                yield return null;
            }
            if (atkImg != null) atkImg.color = Color.white;
            if (defImg != null) defImg.color = Color.white;

            // Retroceso
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float p = t / 0.3f;
                if (atkImg != null) atkImg.rectTransform.anchoredPosition =
                    Vector2.Lerp(atkStart * 0.5f, atkStart, p);
                if (defImg != null) defImg.rectTransform.anchoredPosition =
                    Vector2.Lerp(defStart * 0.5f, defStart, p);
                yield return null;
            }

            if (atkImg != null) atkImg.rectTransform.anchoredPosition = atkStart;
            if (defImg != null) defImg.rectTransform.anchoredPosition = defStart;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool Beats(ElementType a, ElementType b) =>
            (a == ElementType.Water && b == ElementType.Fire) ||
            (a == ElementType.Fire && b == ElementType.Earth) ||
            (a == ElementType.Earth && b == ElementType.Air) ||
            (a == ElementType.Air && b == ElementType.Water);

        private CardData BestCard(PlayerData pd)
        {
            if (pd.hand == null || pd.hand.Count == 0) return null;
            CardData best = pd.hand[0];
            foreach (var c in pd.hand)
                if (c.attackPower > best.attackPower) best = c;
            return best;
        }

        private void ApplyCardEffect(CardData card, PlayerData winner, PlayerData loser)
        {
            if (card == null) return;
            var gm = GameManager.Instance;
            switch (card.effectType)
            {
                case CardEffectType.Heal:
                    winner.Heal(card.effectValue);
                    gm?.Log($"{winner.playerName} recupera {card.effectValue} HP");
                    break;
                case CardEffectType.DoubleDamage:
                    loser.TakeDamage(card.attackPower);
                    gm?.Log($"Daño doble! {loser.playerName} -{card.attackPower} HP extra");
                    break;
                case CardEffectType.Shield:
                    winner.Heal(card.effectValue / 2);
                    break;
                case CardEffectType.StealCard:
                    if (loser.hand?.Count > 0)
                    {
                        var stolen = loser.hand[0];
                        loser.RemoveCard(stolen);
                        winner.hand?.Add(stolen);
                        gm?.Log($"{winner.playerName} roba {stolen.cardName}!");
                    }
                    break;
            }
        }
    }
}