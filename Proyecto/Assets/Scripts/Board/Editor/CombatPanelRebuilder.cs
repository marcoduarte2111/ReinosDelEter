using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ReinosDelEter;

/// <summary>
/// Regenera SOLO el panel de combate dentro de un HUDController existente.
/// Útil cuando la UI fue creada antes de los cambios y faltan referencias.
/// </summary>
public static class CombatPanelRebuilder
{
    [MenuItem("Reinos del Éter/3 - Reconstruir Panel de Combate")]
    public static void RebuildCombatPanel()
    {
        var hud = Object.FindFirstObjectByType<HUDController>();
        if (hud == null)
        {
            Debug.LogError("[CombatPanelRebuilder] No hay HUDController en la escena.");
            return;
        }

        // Buscar Canvas del HUD
        Canvas canvas = hud.GetComponentInChildren<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CombatPanelRebuilder] No hay Canvas en la escena.");
            return;
        }

        // Eliminar panel viejo si existe
        if (hud.combatPanel != null)
        {
            Object.DestroyImmediate(hud.combatPanel);
        }
        else
        {
            // Buscar por nombre por si quedó huérfano
            var existing = canvas.transform.Find("CombatPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        // ── Crear CombatPanel (FULL SCREEN para tapar el resto del HUD) ──
        var cpGo = NewAnchoredGO("CombatPanel", canvas.transform,
            Vector2.zero, Vector2.one);
        cpGo.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.08f, 0.97f);
        hud.combatPanel = cpGo;

        // VS title
        var titleGo = NewAnchoredGO("CombatResult", cpGo.transform,
            new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.95f));
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "COMBATE";
        titleTxt.fontSize = 36;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = Color.white;
        hud.combatResultLabel = titleTxt;

        // ── Attacker ─────────────────────────────────────────────────────
        var atkBg = NewAnchoredGO("AttackerCard", cpGo.transform,
            new Vector2(0.1f, 0.25f), new Vector2(0.42f, 0.78f));
        atkBg.AddComponent<Image>().color = new Color(0.2f, 0.3f, 0.5f, 1f);

        var atkRaw = NewAnchoredGO("AttackerCard_Image", atkBg.transform,
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        var atkRawImg = atkRaw.AddComponent<RawImage>();
        atkRawImg.color = Color.white;
        atkRawImg.raycastTarget = false;
        hud.attackerCardDisplay = atkRawImg;

        var atkLblGo = NewAnchoredGO("AttackerLabel", cpGo.transform,
            new Vector2(0.1f, 0.08f), new Vector2(0.42f, 0.24f));
        var atkLbl = atkLblGo.AddComponent<TextMeshProUGUI>();
        atkLbl.text = "";
        atkLbl.fontSize = 20;
        atkLbl.alignment = TextAlignmentOptions.Center;
        atkLbl.color = Color.white;
        atkLbl.richText = true;
        hud.attackerCardLabel = atkLbl;

        // ── VS divider ───────────────────────────────────────────────────
        var vsGo = NewAnchoredGO("VS", cpGo.transform,
            new Vector2(0.42f, 0.4f), new Vector2(0.58f, 0.6f));
        var vsLbl = vsGo.AddComponent<TextMeshProUGUI>();
        vsLbl.text = "VS";
        vsLbl.fontSize = 48;
        vsLbl.fontStyle = FontStyles.Bold;
        vsLbl.alignment = TextAlignmentOptions.Center;
        vsLbl.color = new Color(1f, 0.6f, 0.1f, 1f);

        // ── Defender ─────────────────────────────────────────────────────
        var defBg = NewAnchoredGO("DefenderCard", cpGo.transform,
            new Vector2(0.58f, 0.25f), new Vector2(0.9f, 0.78f));
        defBg.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);

        var defRaw = NewAnchoredGO("DefenderCard_Image", defBg.transform,
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        var defRawImg = defRaw.AddComponent<RawImage>();
        defRawImg.color = Color.white;
        defRawImg.raycastTarget = false;
        hud.defenderCardDisplay = defRawImg;

        var defLblGo = NewAnchoredGO("DefenderLabel", cpGo.transform,
            new Vector2(0.58f, 0.08f), new Vector2(0.9f, 0.24f));
        var defLbl = defLblGo.AddComponent<TextMeshProUGUI>();
        defLbl.text = "";
        defLbl.fontSize = 20;
        defLbl.alignment = TextAlignmentOptions.Center;
        defLbl.color = Color.white;
        defLbl.richText = true;
        hud.defenderCardLabel = defLbl;

        cpGo.SetActive(false);
        cpGo.transform.SetAsLastSibling();

        EditorUtility.SetDirty(hud);
        Debug.Log("✓ Panel de combate reconstruido. Referencias asignadas a HUDController.");
    }

    private static GameObject NewAnchoredGO(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }
}
