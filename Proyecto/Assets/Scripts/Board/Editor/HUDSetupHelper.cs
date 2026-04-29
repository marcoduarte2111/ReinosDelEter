using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace ReinosDelEter
{
    /// <summary>
    /// Editor helper para configurar rápidamente HUDController.
    /// Crea el Canvas UI y asigna las referencias automáticamente.
    /// </summary>
    public class HUDSetupHelper
    {
        [MenuItem("Reinos del Éter/Setup HUD UI")]
        public static void SetupHUD()
        {
            Debug.Log("\n[HUDSetup] Iniciando setup de HUD...");

            // 1. Buscar o crear Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                var graphic = canvasGO.AddComponent<GraphicRaycaster>();
                Debug.Log("[HUDSetup] ✓ Canvas creado");
            }

            // 2. Buscar o crear HUDController
            HUDController hud = canvas.GetComponent<HUDController>();
            if (hud == null)
            {
                hud = canvas.gameObject.AddComponent<HUDController>();
                Debug.Log("[HUDSetup] ✓ HUDController añadido");
            }

            // 3. Crear estructura UI
            SetupCanvasStructure(canvas, hud);

            // 4. Marcar como dirty
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(canvas.gameObject);
            AssetDatabase.SaveAssets();

            Debug.Log("[HUDSetup] ✅ Setup completado.\n");
        }

        private static void SetupCanvasStructure(Canvas canvas, HUDController hud)
        {
            // BottomBar con HandContainer (más importante)
            Transform bottomBar = canvas.transform.Find("BottomBar");
            if (bottomBar == null)
            {
                GameObject bottomBarGO = new GameObject("BottomBar");
                bottomBar = bottomBarGO.transform;
                bottomBar.SetParent(canvas.transform, false);
                var rt = bottomBarGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0.15f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                Debug.Log("[HUDSetup] ✓ BottomBar creado");
            }

            // HandContainer
            Transform handContainer = bottomBar.Find("HandContainer");
            if (handContainer == null)
            {
                GameObject handContainerGO = new GameObject("HandContainer");
                handContainer = handContainerGO.transform;
                handContainer.SetParent(bottomBar, false);
                
                var hcrt = handContainerGO.AddComponent<RectTransform>();
                hcrt.anchorMin = Vector2.zero;
                hcrt.anchorMax = Vector2.one;
                hcrt.offsetMin = Vector2.zero;
                hcrt.offsetMax = Vector2.zero;
                hcrt.anchoredPosition = Vector2.zero;
                
                var hlg = handContainerGO.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 5;
                hlg.padding = new RectOffset(10, 10, 5, 5);
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;

                Debug.Log("[HUDSetup] ✓ HandContainer creado");
            }

            // Asignar handContainer a HUDController
            if (hud.handContainer == null)
            {
                hud.handContainer = handContainer;
                Debug.Log("[HUDSetup] ✓ handContainer asignado a HUDController");
            }

            // TopBar (opcional)
            Transform topBar = canvas.transform.Find("TopBar");
            if (topBar == null)
            {
                GameObject topBarGO = new GameObject("TopBar");
                topBar = topBarGO.transform;
                topBar.SetParent(canvas.transform, false);
                var rt = topBarGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.9f);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Debug.Log("[HUDSetup] ✓ TopBar creado");
            }
        }
    }
}
