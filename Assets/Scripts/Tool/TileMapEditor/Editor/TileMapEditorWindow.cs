using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TileMapEditorWindow : EditorWindow
{
    private enum EditMode { TileType, SpawnPoint, Entity }
    private enum HitType { None, Tile, EastEdge, NorthEdge }

    // Grid settings
    private int _width = 10;
    private int _height = 10;
    private float _tileSize = 1.05f;

    // Grid data
    private TilePresetData[,] _gridData;
    private bool _gridInitialized;

    // Edge entity data (0 = 없음)
    private Dictionary<EdgeTileKey, int> _edgeEntityData = new();

    // Edit state
    private EditMode _editMode = EditMode.TileType;
    private TileType _selectedTileType = TileType.Ground;
    private SpawnPointType _selectedSpawnPoint = SpawnPointType.Player;
    private int _selectedSpawnId = 1;
    private int _selectedEntityId;
    private bool _isPainting;

    // Scroll
    private Vector2 _scrollPosition;

    // Visual constants
    private const float CELL_SIZE   = 32f;
    private const float EDGE_SIZE   = 12f;
    private const float CELL_STRIDE = CELL_SIZE + EDGE_SIZE;
    private const float TOOLBAR_HEIGHT = 24f;

    // Default preset path
    private const string PRESET_FOLDER = "Assets/Resources/Tile/Preset";

    // Entity catalog cache
    private EntityCatalog _catalog;
    private int[] _entityIds;       // catalog entry id
    private string[] _entityLabels; // "[Category] DisplayName" 형태 (표시용)

    // Loaded preset tracking (.json이면 절대경로, .asset이면 Assets 상대경로)
    private string _loadedPresetPath = "";

    [MenuItem("Tools/Tile/TileMap Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<TileMapEditorWindow>("TileMap Editor");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        RefreshEntityCatalog();
    }

    private void OnGUI()
    {
        DrawSettingsPanel();
        EditorGUILayout.Space(4);
        DrawEditModeToolbar();
        EditorGUILayout.Space(4);
        DrawGridView();
        EditorGUILayout.Space(4);
        DrawToolPanel();
        EditorGUILayout.Space(4);
        DrawExportImportPanel();
    }

    #region Settings Panel

    private void DrawSettingsPanel()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _width = EditorGUILayout.IntField("Width", _width);
        _height = EditorGUILayout.IntField("Height", _height);
        EditorGUILayout.EndHorizontal();

        _tileSize = EditorGUILayout.FloatField("Tile Size", _tileSize);

        if (GUILayout.Button("Generate", GUILayout.Height(24)))
            GenerateGrid();
    }

    #endregion

    #region Edit Mode Toolbar

    private void DrawEditModeToolbar()
    {
        EditorGUILayout.LabelField("Edit Mode", EditorStyles.boldLabel);
        _editMode = (EditMode)GUILayout.Toolbar((int)_editMode,
            new[] { "TileType Paint", "Spawn Point", "Entity Placement" });
    }

    #endregion

    #region Grid View

    private void DrawGridView()
    {
        if (!_gridInitialized || _gridData == null)
        {
            EditorGUILayout.HelpBox("Click 'Generate' to create a grid.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

        float gridWidth  = _width  * CELL_SIZE + (_width  - 1) * EDGE_SIZE;
        float gridHeight = _height * CELL_SIZE + (_height - 1) * EDGE_SIZE;

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
            GUILayout.ExpandHeight(true), GUILayout.MinHeight(350));

        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

        Event e = Event.current;
        if (e != null && gridRect.Contains(e.mousePosition))
        {
            Vector2 localPos = e.mousePosition - gridRect.position;
            HitType hitType = GetHitType(localPos, out int hitX, out int hitY, out EdgeTileKey edgeKey);

            if (hitType != HitType.None)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    _isPainting = true;
                    if (hitType == HitType.Tile) ApplyEditToCell(hitX, hitY);
                    else ApplyEditToEdge(edgeKey);
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && _isPainting)
                {
                    if (hitType == HitType.Tile) ApplyEditToCell(hitX, hitY);
                    else ApplyEditToEdge(edgeKey);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    _isPainting = false;
                    e.Use();
                }
            }
        }

        if (e != null && e.type == EventType.MouseUp)
            _isPainting = false;

        // Draw tile cells
        for (int displayRow = 0; displayRow < _height; displayRow++)
        {
            int dataY = _height - 1 - displayRow;
            for (int col = 0; col < _width; col++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + col * CELL_STRIDE,
                    gridRect.y + displayRow * CELL_STRIDE,
                    CELL_SIZE, CELL_SIZE);

                var data = _gridData[col, dataY];
                EditorGUI.DrawRect(cellRect, GetTileColor(data.tileType));
                DrawCellBorder(cellRect);

                if (data.spawnPoint != SpawnPointType.None)
                {
                    string spawnLabel = data.spawnPoint == SpawnPointType.Player
                        ? $"P:{data.spawnId}"
                        : $"E:{data.spawnId}";
                    Color spawnColor = data.spawnPoint == SpawnPointType.Player ? Color.cyan : Color.magenta;
                    GUI.Label(cellRect, spawnLabel, new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.UpperRight,
                        fontSize = 10,
                        normal = { textColor = spawnColor }
                    });
                }

                if (data.entityId > 0)
                {
                    GUI.Label(cellRect, GetEntityShortName(data.entityId), new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.LowerCenter,
                        fontSize = 8,
                        normal = { textColor = Color.white },
                        wordWrap = true
                    });
                }
            }
        }

        // Draw East edge slots (x → x+1, 수직 스트립)
        for (int displayRow = 0; displayRow < _height; displayRow++)
        {
            int dataY = _height - 1 - displayRow;
            for (int col = 0; col < _width - 1; col++)
            {
                var edgeKey = new EdgeTileKey(new Vector2Int(col, dataY), new Vector2Int(col + 1, dataY));
                bool hasEntity = _edgeEntityData.TryGetValue(edgeKey, out int entityId) && entityId > 0;

                Rect edgeRect = new Rect(
                    gridRect.x + col * CELL_STRIDE + CELL_SIZE,
                    gridRect.y + displayRow * CELL_STRIDE,
                    EDGE_SIZE, CELL_SIZE);

                EditorGUI.DrawRect(edgeRect, hasEntity ? new Color(1f, 0.6f, 0f) : new Color(0.15f, 0.15f, 0.15f));
                if (hasEntity)
                {
                    GUI.Label(edgeRect, GetEntityShortName(entityId), new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 7,
                        normal = { textColor = Color.black }
                    });
                }
            }
        }

        // Draw North edge slots (y → y+1, 수평 스트립)
        for (int displayRow = 0; displayRow < _height - 1; displayRow++)
        {
            int dataY_lower = _height - 2 - displayRow;
            for (int col = 0; col < _width; col++)
            {
                var edgeKey = new EdgeTileKey(
                    new Vector2Int(col, dataY_lower),
                    new Vector2Int(col, dataY_lower + 1));
                bool hasEntity = _edgeEntityData.TryGetValue(edgeKey, out int entityId) && entityId > 0;

                Rect edgeRect = new Rect(
                    gridRect.x + col * CELL_STRIDE,
                    gridRect.y + displayRow * CELL_STRIDE + CELL_SIZE,
                    CELL_SIZE, EDGE_SIZE);

                EditorGUI.DrawRect(edgeRect, hasEntity ? new Color(1f, 0.6f, 0f) : new Color(0.15f, 0.15f, 0.15f));
                if (hasEntity)
                {
                    GUI.Label(edgeRect, GetEntityShortName(entityId), new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 7,
                        normal = { textColor = Color.black }
                    });
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private HitType GetHitType(Vector2 localPos, out int hitX, out int hitY, out EdgeTileKey edgeKey)
    {
        hitX = 0; hitY = 0;
        edgeKey = default;

        int col = Mathf.FloorToInt(localPos.x / CELL_STRIDE);
        int row = Mathf.FloorToInt(localPos.y / CELL_STRIDE);
        float colPart = localPos.x % CELL_STRIDE;
        float rowPart = localPos.y % CELL_STRIDE;

        bool inTileCol = colPart < CELL_SIZE;
        bool inTileRow = rowPart < CELL_SIZE;

        if (col < 0 || row < 0) return HitType.None;

        if (inTileCol && inTileRow)
        {
            if (col >= _width || row >= _height) return HitType.None;
            hitX = col;
            hitY = _height - 1 - row;
            return HitType.Tile;
        }
        else if (inTileCol && !inTileRow)
        {
            if (col >= _width || row >= _height - 1) return HitType.None;
            int dataY_lower = _height - 2 - row;
            if (dataY_lower < 0) return HitType.None;
            edgeKey = new EdgeTileKey(
                new Vector2Int(col, dataY_lower),
                new Vector2Int(col, dataY_lower + 1));
            return HitType.NorthEdge;
        }
        else if (!inTileCol && inTileRow)
        {
            if (col >= _width - 1 || row >= _height) return HitType.None;
            int dataY = _height - 1 - row;
            edgeKey = new EdgeTileKey(
                new Vector2Int(col, dataY),
                new Vector2Int(col + 1, dataY));
            return HitType.EastEdge;
        }

        return HitType.None;
    }

    private Color GetTileColor(TileType type)
    {
        return type switch
        {
            TileType.Empty   => new Color(0.5f, 0.5f, 0.5f),
            TileType.Ground  => new Color(0.3f, 0.7f, 0.3f),
            TileType.Dirt    => new Color(0.6f, 0.4f, 0.2f),
            TileType.Water   => new Color(0.2f, 0.4f, 0.8f),
            TileType.Blocked => new Color(0.8f, 0.2f, 0.2f),
            _ => Color.gray
        };
    }

    private void DrawCellBorder(Rect rect)
    {
        Color borderColor = new Color(0, 0, 0, 0.3f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), borderColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), borderColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), borderColor);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), borderColor);
    }

    private string GetEntityShortName(int entityId)
    {
        if (entityId <= 0) return "";
        if (_catalog != null)
        {
            var entry = _catalog.GetEntry(entityId);
            if (entry != null)
            {
                string name = entry.displayName;
                return name.Length > 6 ? name[..6] : name;
            }
        }
        return entityId.ToString();
    }

    #endregion

    #region Edit Actions

    private void ApplyEditToCell(int x, int y)
    {
        var data = _gridData[x, y];

        switch (_editMode)
        {
            case EditMode.TileType:
                data.tileType = _selectedTileType;
                break;

            case EditMode.SpawnPoint:
                if (!_isPainting || Event.current.type == EventType.MouseDown)
                {
                    if (data.spawnPoint == SpawnPointType.None)
                    {
                        data.spawnPoint = _selectedSpawnPoint;
                        data.spawnId = _selectedSpawnId;
                    }
                    else
                    {
                        data.spawnPoint = SpawnPointType.None;
                        data.spawnId = 0;
                    }
                }
                else
                {
                    data.spawnPoint = _selectedSpawnPoint;
                    data.spawnId = _selectedSpawnId;
                }
                break;

            case EditMode.Entity:
                if (!_isPainting || Event.current.type == EventType.MouseDown)
                    ShowEntityPopup(x, y);
                break;
        }

        Repaint();
    }

    private void ApplyEditToEdge(EdgeTileKey edgeKey)
    {
        if (_editMode == EditMode.Entity)
        {
            if (!_isPainting || Event.current.type == EventType.MouseDown)
                ShowEdgeEntityPopup(edgeKey);
        }
    }

    private void ShowEntityPopup(int x, int y)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("None"), false, () =>
        {
            _gridData[x, y].entityId = 0;
            Repaint();
        });

        if (_catalog != null)
        {
            foreach (var entry in _catalog.GetAllEntries())
            {
                int id = entry.id;
                string menuPath = $"{entry.category}/{entry.displayName}";
                menu.AddItem(new GUIContent(menuPath), false, () =>
                {
                    _gridData[x, y].entityId = id;
                    Repaint();
                });
            }
        }

        menu.ShowAsContext();
    }

    private void ShowEdgeEntityPopup(EdgeTileKey edgeKey)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("None"), false, () =>
        {
            _edgeEntityData.Remove(edgeKey);
            Repaint();
        });

        if (_catalog != null)
        {
            foreach (var entry in _catalog.GetAllEntries())
            {
                int id = entry.id;
                string menuPath = $"{entry.category}/{entry.displayName}";
                menu.AddItem(new GUIContent(menuPath), false, () =>
                {
                    _edgeEntityData[edgeKey] = id;
                    Repaint();
                });
            }
        }

        menu.ShowAsContext();
    }

    #endregion

    #region Tool Panel

    private void DrawToolPanel()
    {
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        switch (_editMode)
        {
            case EditMode.TileType:  DrawTileTypeTools();   break;
            case EditMode.SpawnPoint: DrawSpawnPointTools(); break;
            case EditMode.Entity:    DrawEntityTools();     break;
        }

        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill All")) FillAll();
        if (GUILayout.Button("Clear All")) ClearAll();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTileTypeTools()
    {
        EditorGUILayout.BeginHorizontal();
        var tileTypes = System.Enum.GetValues(typeof(TileType));
        foreach (TileType type in tileTypes)
        {
            bool isSelected = _selectedTileType == type;
            var style = isSelected ? new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold } : GUI.skin.button;

            Color oldBg = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = GetTileColor(type);
            if (GUILayout.Button(type.ToString(), style)) _selectedTileType = type;
            GUI.backgroundColor = oldBg;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpawnPointTools()
    {
        EditorGUILayout.BeginHorizontal();
        var spawnTypes = System.Enum.GetValues(typeof(SpawnPointType));
        foreach (SpawnPointType type in spawnTypes)
        {
            bool isSelected = _selectedSpawnPoint == type;
            var style = isSelected ? new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold } : GUI.skin.button;
            if (GUILayout.Button(type.ToString(), style)) _selectedSpawnPoint = type;
        }
        EditorGUILayout.EndHorizontal();
        _selectedSpawnId = EditorGUILayout.IntField("Spawn ID (Pawn/Monster)", _selectedSpawnId);
        EditorGUILayout.HelpBox("Click: toggle spawn (None ↔ selected type+ID). Drag: paint selected type+ID.", MessageType.Info);
    }

    private void DrawEntityTools()
    {
        if (_catalog == null)
        {
            EditorGUILayout.HelpBox(
                "EntityCatalog not found.\n" +
                "Create via: Assets > Create > Game > Entity Catalog\n" +
                "Place at: Assets/Resources/Data/EntityCatalog.asset",
                MessageType.Warning);
            if (GUILayout.Button("Refresh Catalog")) RefreshEntityCatalog();
            return;
        }

        if (_entityIds == null || _entityIds.Length == 0)
        {
            EditorGUILayout.HelpBox("EntityCatalog is empty. Add entries in the Inspector.", MessageType.Warning);
            if (GUILayout.Button("Refresh Catalog")) RefreshEntityCatalog();
            return;
        }

        EditorGUILayout.LabelField("Selected Entity:");
        int selectedIdx = _entityIds != null ? System.Array.IndexOf(_entityIds, _selectedEntityId) : -1;
        int newIdx = EditorGUILayout.Popup(selectedIdx, _entityLabels);
        if (newIdx >= 0 && newIdx < _entityIds.Length)
            _selectedEntityId = _entityIds[newIdx];

        if (GUILayout.Button("Refresh Catalog")) RefreshEntityCatalog();

        EditorGUILayout.HelpBox(
            "Click a tile or edge slot (dark strips) to open entity popup.\n" +
            "Orange = entity placed. Popup shows Category/Name submenus.",
            MessageType.Info);
    }

    #endregion

    #region Export / Import

    private void DrawExportImportPanel()
    {
        EditorGUILayout.LabelField("Export / Import", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Export ScriptableObject")) ExportAsScriptableObject();
        if (GUILayout.Button("Export JSON")) ExportAsJson();
        if (GUILayout.Button("Load Preset")) LoadPreset();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        bool hasLoaded = !string.IsNullOrEmpty(_loadedPresetPath);
        EditorGUILayout.LabelField(hasLoaded ? Path.GetFileName(_loadedPresetPath) : "No preset loaded", EditorStyles.miniLabel);
        GUI.enabled = hasLoaded && _gridInitialized;
        if (GUILayout.Button("Save (Overwrite)", GUILayout.Width(130))) OverwritePreset();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private void OverwritePreset()
    {
        if (string.IsNullOrEmpty(_loadedPresetPath)) return;

        if (_loadedPresetPath.EndsWith(".json"))
        {
            var preset = CreateInstance<TileMapPreset>();
            FillPresetFromGrid(preset);
            File.WriteAllText(_loadedPresetPath, preset.ToJson());
            DestroyImmediate(preset);
            EditorUtility.DisplayDialog("Saved", $"Overwritten:\n{_loadedPresetPath}", "OK");
        }
        else if (_loadedPresetPath.EndsWith(".asset"))
        {
            var preset = AssetDatabase.LoadAssetAtPath<TileMapPreset>(_loadedPresetPath);
            if (preset == null)
            {
                EditorUtility.DisplayDialog("Error", $"Asset not found:\n{_loadedPresetPath}", "OK");
                return;
            }
            FillPresetFromGrid(preset);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Saved", $"Overwritten:\n{_loadedPresetPath}", "OK");
        }
    }

    private void EnsurePresetFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(PRESET_FOLDER))
        {
            string[] parts = PRESET_FOLDER.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    private void ExportAsScriptableObject()
    {
        if (!_gridInitialized)
        {
            EditorUtility.DisplayDialog("Error", "No grid data to export. Generate a grid first.", "OK");
            return;
        }
        EnsurePresetFolderExists();
        string path = EditorUtility.SaveFilePanelInProject("Save TileMap Preset", "TileMapPreset", "asset",
            "Save the tilemap preset as a ScriptableObject", PRESET_FOLDER);
        if (string.IsNullOrEmpty(path)) return;

        var preset = CreateInstance<TileMapPreset>();
        FillPresetFromGrid(preset);
        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Preset saved to:\n{path}", "OK");
    }

    private void ExportAsJson()
    {
        if (!_gridInitialized)
        {
            EditorUtility.DisplayDialog("Error", "No grid data to export. Generate a grid first.", "OK");
            return;
        }
        EnsurePresetFolderExists();
        string defaultDir = Path.Combine(Application.dataPath, "Resources/Tile/Preset");
        string path = EditorUtility.SaveFilePanel("Save TileMap JSON", defaultDir, "TileMapPreset", "json");
        if (string.IsNullOrEmpty(path)) return;

        var preset = CreateInstance<TileMapPreset>();
        FillPresetFromGrid(preset);
        File.WriteAllText(path, preset.ToJson());
        DestroyImmediate(preset);
        EditorUtility.DisplayDialog("Success", $"JSON saved to:\n{path}", "OK");
    }

    private void LoadPreset()
    {
        string defaultDir = Path.Combine(Application.dataPath, "Resources/Tile/Preset");
        string path = EditorUtility.OpenFilePanel("Load TileMap Preset", defaultDir, "asset,json");
        if (string.IsNullOrEmpty(path)) return;

        if (path.EndsWith(".json")) LoadFromJson(path);
        else if (path.EndsWith(".asset"))
        {
            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
            LoadFromAsset(relativePath);
        }
    }

    private void LoadFromJson(string path)
    {
        string json = File.ReadAllText(path);
        var preset = CreateInstance<TileMapPreset>();
        preset.FromJson(json);
        ApplyPresetToGrid(preset);
        DestroyImmediate(preset);
        _loadedPresetPath = path;
        Repaint();
    }

    private void LoadFromAsset(string assetPath)
    {
        var preset = AssetDatabase.LoadAssetAtPath<TileMapPreset>(assetPath);
        if (preset == null)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to load preset at:\n{assetPath}", "OK");
            return;
        }
        ApplyPresetToGrid(preset);
        _loadedPresetPath = assetPath;
        Repaint();
    }

    #endregion

    #region Grid Operations

    private void GenerateGrid()
    {
        if (_width <= 0 || _height <= 0)
        {
            EditorUtility.DisplayDialog("Error", "Width and Height must be greater than 0.", "OK");
            return;
        }

        _gridData = new TilePresetData[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _gridData[x, y] = new TilePresetData
                {
                    position = new Vector2Int(x, y),
                    tileType = TileType.Ground,
                    spawnPoint = SpawnPointType.None,
                    entityId = 0
                };
            }
        }

        _edgeEntityData = new Dictionary<EdgeTileKey, int>();
        _gridInitialized = true;
        Repaint();
    }

    private void FillAll()
    {
        if (!_gridInitialized) return;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                switch (_editMode)
                {
                    case EditMode.TileType:   _gridData[x, y].tileType = _selectedTileType;   break;
                    case EditMode.SpawnPoint: _gridData[x, y].spawnPoint = _selectedSpawnPoint; break;
                    case EditMode.Entity:     _gridData[x, y].entityId = _selectedEntityId;   break;
                }
            }
        }
        Repaint();
    }

    private void ClearAll()
    {
        if (!_gridInitialized) return;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _gridData[x, y].tileType = TileType.Ground;
                _gridData[x, y].spawnPoint = SpawnPointType.None;
                _gridData[x, y].entityId = 0;
            }
        }
        _edgeEntityData.Clear();
        Repaint();
    }

    private void FillPresetFromGrid(TileMapPreset preset)
    {
        preset.width = _width;
        preset.height = _height;
        preset.tileSize = _tileSize;
        preset.tiles.Clear();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                preset.tiles.Add(new TilePresetData
                {
                    position = _gridData[x, y].position,
                    tileType = _gridData[x, y].tileType,
                    spawnPoint = _gridData[x, y].spawnPoint,
                    spawnId = _gridData[x, y].spawnId,
                    entityId = _gridData[x, y].entityId
                });
            }
        }

        preset.edges.Clear();
        foreach (var kvp in _edgeEntityData)
        {
            if (kvp.Value <= 0) continue;
            preset.edges.Add(new EdgePresetData
            {
                posA = kvp.Key.PosA,
                posB = kvp.Key.PosB,
                entityId = kvp.Value
            });
        }
    }

    private void ApplyPresetToGrid(TileMapPreset preset)
    {
        _width = preset.width;
        _height = preset.height;
        _tileSize = preset.tileSize;

        _gridData = new TilePresetData[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _gridData[x, y] = new TilePresetData
                {
                    position = new Vector2Int(x, y),
                    tileType = TileType.Ground,
                    spawnPoint = SpawnPointType.None,
                    entityId = 0
                };
            }
        }

        foreach (var tile in preset.tiles)
        {
            int x = tile.position.x;
            int y = tile.position.y;
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _gridData[x, y] = new TilePresetData
                {
                    position = tile.position,
                    tileType = tile.tileType,
                    spawnPoint = tile.spawnPoint,
                    spawnId = tile.spawnId,
                    entityId = tile.entityId
                };
            }
        }

        _edgeEntityData = new Dictionary<EdgeTileKey, int>();
        if (preset.edges != null)
        {
            foreach (var edge in preset.edges)
            {
                if (edge.entityId > 0)
                    _edgeEntityData[new EdgeTileKey(edge.posA, edge.posB)] = edge.entityId;
            }
        }

        _gridInitialized = true;
    }

    #endregion

    #region Utility

    private void RefreshEntityCatalog()
    {
        _catalog = null;
        _entityIds = System.Array.Empty<int>();
        _entityLabels = System.Array.Empty<string>();

        string[] guids = AssetDatabase.FindAssets("t:EntityCatalog");
        if (guids.Length == 0) return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        _catalog = AssetDatabase.LoadAssetAtPath<EntityCatalog>(path);
        if (_catalog == null) return;

        _catalog.RebuildLookup();

        var entries = _catalog.entries;
        _entityIds    = entries.Select(e => e.id).ToArray();
        _entityLabels = entries.Select(e => $"[{e.category}] {e.displayName}").ToArray();
    }

    #endregion
}
