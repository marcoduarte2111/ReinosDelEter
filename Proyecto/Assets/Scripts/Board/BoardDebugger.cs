using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// Adjunta a cualquier GameObject. En Play, presiona D para
    /// imprimir todas las conexiones del tablero en consola.
    /// </summary>
    public class BoardDebugger : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
                DebugBoard();
        }

        private void DebugBoard()
        {
            var board = Object.FindFirstObjectByType<BoardGenerator>();
            if (board == null) { Debug.LogError("No BoardGenerator"); return; }

            Tile center = board.centerTile;
            Debug.Log($"=== CENTER: nextTile={center?.nextTile?.name ?? "NULL"} connectedTiles={center?.connectedTiles?.Length ?? 0} ===");
            if (center?.connectedTiles != null)
                foreach (var t in center.connectedTiles)
                    Debug.Log($"  center → {t?.name ?? "NULL"}");

            for (int p = 0; p < 4; p++)
            {
                if (board.paths?[p] == null) continue;
                var arm = board.paths[p];
                Tile last = arm[arm.Count - 1];
                Tile corner = last.nextTile;

                Debug.Log($"=== CORNER[{p}] = {corner?.name ?? "NULL"} connectedTiles={corner?.connectedTiles?.Length ?? 0} ===");
                if (corner?.connectedTiles != null)
                    foreach (var t in corner.connectedTiles)
                        Debug.Log($"  corner[{p}] → {t?.name ?? "NULL"}");

                Debug.Log($"  StartTile[{p}]={arm[0].name} nextTile={arm[0].nextTile?.name ?? "NULL"} connectedTiles={arm[0].connectedTiles?.Length ?? 0}");
                Debug.Log($"  ArmLast[{p}]={last.name} nextTile={last.nextTile?.name ?? "NULL"} connectedTiles={last.connectedTiles?.Length ?? 0}");
            }
        }
    }
}