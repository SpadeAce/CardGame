using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스테이지 씬 관리 스크립트
/// </summary>
public class StageScript : SceneBase
{
    public override string SceneName => "StageScene";

    [SerializeField] private TileMapPreset _stagePreset;

    private StagePage _stagePage;
    private Camera _mainCamera;

    public override void OnEnterScene()
    {
        _stagePage = UIManager.Instance.OpenView<StagePage>();
        _mainCamera = Camera.main ?? Camera.allCameras[0];

        StageManager.Instance.LoadStage(_stagePreset);
        DeckManager.Instance.InitStage();

        foreach (var actor in StageManager.Instance.UserActors.Values)
            if (actor?.Data is DPawn pawn)
            {
                pawn.BuildCardPool();
                pawn.InitHand();
            }

        var cameraController = _mainCamera.GetComponentInParent<CameraController>();
        if (cameraController != null)
            cameraController.SetPivot(TileManager.Instance.GetMapCenter());

        TurnManager.Instance.StartGame();
    }

    public override void OnExitScene()
    {
        TileManager.Instance?.ClearAllTiles();
        DeckManager.Instance.ResetPawnStats();
        LobbyManager.Instance.ResetForNextStage();
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (_stagePage != null && _stagePage.IsCardDragging) return;
            HandleTileClick(Mouse.current.position.ReadValue());
        }
    }

    /// <summary>
    /// 타일 클릭 처리
    /// </summary>
    private void HandleTileClick(Vector2 screenPosition)
    {
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 컬라이더의 부모에서 SquareTile 컴포넌트 검색
            SquareTile tile = hit.collider.GetComponentInParent<SquareTile>();
            
            if (tile != null)
            {
                StageManager.Instance.SelectTile(tile);
                Debug.Log($"[StageScript] Tile clicked: ID={tile.TileId}, Pos={tile.GridPosition}");
            }
        }
    }
}
