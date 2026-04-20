using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReinosDelEter
{
    /// <summary>
    /// CombatManager — resuelve combates por cartas con animación.
    ///
    /// FLUJO DE COMBATE:
    ///   1. GameManager llama StartCombat(attacker, defender, callback)
    ///   2. Ambos jugadores eligen una carta (o se elige automáticamente)
    ///   3. Se reproduce la animación de choque de cartas
    ///   4. Se calculan resultados por ATK/DEF + elemento
    ///   5. Se llama onFinished(winner, loser)
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [Header("Configuración")]
        public float cardRevealDelay = 0.8f;   // segundos entre revelar cartas
        public float combatAnimTime = 1.4f;   // duración de la animación de choque

        [Header("Ventaja elemental")]
        [Tooltip("Factor multiplicador cuando el elemento ataca al débil")]
        public float elementBonus = 1.5f;

        // Runtime
        private Piece _attacker, _defender;
        private Action<Piece, Piece> _onFinished;

        private CardData _atkCard, _defCard;
        private bool _atkCardSelected, _defCardSelected;

        // ── API pública ───────────────────────────────────────────────────────
        public void StartCombat(Piece attacker, Piece defender, Action<Piece, Piece> onFinished)
        {
            _attacker = attacker;
            _defender = defender;
            _onFinished = onFinished;
            _atkCard = _defCard = null;
            _atkCardSelected = _defCardSelected = false;

            StartCoroutine(CombatCoroutine());
        }

        /// <summary>Llamado por HUDController cuando el jugador hace click en una carta.</summary>
        public void OnCardSelectedByPlayer(CardData card, PlayerData owner)
        {
            if (owner.index == _attacker?.playerIndex && !_atkCardSelected)
            {
                _atkCard = card;
                _atkCardSelected = true;
                GameManager.Instance?.Log($"{owner.playerName} juega: {card.cardName}");
            }
            else if (owner.index == _defender?.playerIndex && !_defCardSelected)
            {
                _defCard = card;
                _defCardSelected = true;
                GameManager.Instance?.Log($"{owner.playerName} juega: {card.cardName}");
            }
        }

        // ── Coroutine de combate ──────────────────────────────────────────────
        private IEnumerator CombatCoroutine()
        {
            GameManager gm = GameManager.Instance;

            // ── Fase 1: cada jugador elige carta (máx 5 s o auto) ────────────
            float timeout = 5f;
            float elapsed = 0f;

            // Elige automáticamente la carta con mayor ATK si el jugador no hace click a tiempo
            PlayerData atkPD = gm.Players[_attacker.playerIndex];
            PlayerData defPD = gm.Players[_defender.playerIndex];

            while ((!_atkCardSelected || !_defCardSelected) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Auto-selección si no eligieron
            if (!_atkCardSelected) _atkCard = BestCard(atkPD);
            if (!_defCardSelected) _defCard = BestCard(defPD);

            if (_atkCard == null || _defCard == null)
            {
                // Sin cartas → combate por dado
                Piece winner = UnityEngine.Random.Range(0, 2) == 0 ? _attacker : _defender;
                Piece loser = winner == _attacker ? _defender : _attacker;
                _onFinished?.Invoke(winner, loser);
                yield break;
            }

            // ── Fase 2: animación de cartas ──────────────────────────────────
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            yield return StartCoroutine(PlayCombatAnimation(hud, _atkCard, _defCard));

            // ── Fase 3: resolución ───────────────────────────────────────────
            float atkScore = CalculateScore(_atkCard, _defCard, atkPD.element, defPD.element);
            float defScore = CalculateScore(_defCard, _atkCard, defPD.element, atkPD.element);

            gm.Log($"{_atkCard.cardName} ({atkScore:F0}) vs {_defCard.cardName} ({defScore:F0})");

            Piece combatWinner = atkScore >= defScore ? _attacker : _defender;
            Piece combatLoser = atkScore >= defScore ? _defender : _attacker;

            // Aplica efecto de la carta ganadora
            CardData winCard = atkScore >= defScore ? _atkCard : _defCard;
            ApplyCardEffect(winCard, gm.Players[combatWinner.playerIndex],
                                     gm.Players[combatLoser.playerIndex]);

            // Consume energía y remueve carta usada
            atkPD.SpendEnergy(_atkCard);
            defPD.SpendEnergy(_defCard);
            atkPD.RemoveCard(_atkCard);
            defPD.RemoveCard(_defCard);

            yield return new WaitForSeconds(0.5f);
            _onFinished?.Invoke(combatWinner, combatLoser);
        }

        // ── Animación ─────────────────────────────────────────────────────────
        private IEnumerator PlayCombatAnimation(HUDController hud, CardData atkCard, CardData defCard)
        {
            // Si el HUD tiene el panel de combate, anima las cartas acercándose
            if (hud == null) { yield return new WaitForSeconds(combatAnimTime); yield break; }

            // Simula "choque": las cartas se acercan, flash, retroceden
            RawImage atkImg = hud.attackerCardDisplay;
            RawImage defImg = hud.defenderCardDisplay;

            if (atkImg != null) atkImg.texture = hud.GetCardTexture(atkCard);
            if (defImg != null) defImg.texture = hud.GetCardTexture(defCard);

            Vector2 atkStart = atkImg != null ? atkImg.rectTransform.anchoredPosition : Vector2.zero;
            Vector2 defStart = defImg != null ? defImg.rectTransform.anchoredPosition : Vector2.zero;
            Vector2 center = Vector2.zero;

            // Fase acercamiento
            for (float t = 0; t < cardRevealDelay; t += Time.deltaTime)
            {
                float p = t / cardRevealDelay;
                if (atkImg != null) atkImg.rectTransform.anchoredPosition = Vector2.Lerp(atkStart, center * 0.3f + atkStart * 0.7f, p);
                if (defImg != null) defImg.rectTransform.anchoredPosition = Vector2.Lerp(defStart, center * 0.3f + defStart * 0.7f, p);
                yield return null;
            }

            // Flash de choque
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                float brightness = 1f + Mathf.Sin(t / 0.25f * Mathf.PI) * 1.5f;
                if (atkImg != null) atkImg.color = Color.white * brightness;
                if (defImg != null) defImg.color = Color.white * brightness;
                yield return null;
            }
            if (atkImg != null) atkImg.color = Color.white;
            if (defImg != null) defImg.color = Color.white;

            // Retroceso
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                float p = t / 0.4f;
                if (atkImg != null) atkImg.rectTransform.anchoredPosition = Vector2.Lerp(center * 0.3f + atkStart * 0.7f, atkStart, p);
                if (defImg != null) defImg.rectTransform.anchoredPosition = Vector2.Lerp(center * 0.3f + defStart * 0.7f, defStart, p);
                yield return null;
            }

            if (atkImg != null) atkImg.rectTransform.anchoredPosition = atkStart;
            if (defImg != null) defImg.rectTransform.anchoredPosition = defStart;
        }

        // ── Lógica ────────────────────────────────────────────────────────────
        private float CalculateScore(CardData myCard, CardData enemyCard,
                                     ElementType myElement, ElementType enemyElement)
        {
            float score = myCard.attackPower - enemyCard.defensePower * 0.5f;

            // Ventaja elemental: Water > Fire > Earth > Air > Water
            if (Beats(myElement, enemyElement)) score *= elementBonus;

            return Mathf.Max(1, score);
        }

        private bool Beats(ElementType a, ElementType b) =>
            (a == ElementType.Water && b == ElementType.Fire) ||
            (a == ElementType.Fire && b == ElementType.Earth) ||
            (a == ElementType.Earth && b == ElementType.Air) ||
            (a == ElementType.Air && b == ElementType.Water);

        private void ApplyCardEffect(CardData card, PlayerData winner, PlayerData loser)
        {
            switch (card.effectType)
            {
                case CardEffectType.Heal:
                    winner.Heal(card.effectValue);
                    GameManager.Instance?.Log($"{winner.playerName} recupera {card.effectValue} HP");
                    break;
                case CardEffectType.DoubleDamage:
                    int extra = card.attackPower;
                    loser.TakeDamage(extra);
                    GameManager.Instance?.Log($"Daño doble! {loser.playerName} pierde {extra} HP extra");
                    break;
                case CardEffectType.Shield:
                    winner.Heal(card.effectValue / 2);
                    break;
                case CardEffectType.StealCard:
                    if (loser.hand.Count > 0)
                    {
                        var stolen = loser.hand[0];
                        loser.hand.Remove(stolen);
                        winner.hand.Add(stolen);
                        GameManager.Instance?.Log($"{winner.playerName} roba {stolen.cardName}!");
                    }
                    break;
                case CardEffectType.ExtraMove:
                    // Señal al GameManager para mover extra (implementación futura)
                    GameManager.Instance?.Log($"{winner.playerName} avanza {card.effectValue} casillas extra");
                    break;
            }
        }

        private CardData BestCard(PlayerData pd)
        {
            if (pd.hand.Count == 0) return null;
            CardData best = pd.hand[0];
            foreach (var c in pd.hand)
                if (c.attackPower > best.attackPower) best = c;
            return best;
        }
    }
}