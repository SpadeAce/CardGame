using System.Collections.Generic;
using SA.UI;
using UnityEngine;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/StageSelect/StageSelectPage")]
public class StageSelectPage : PageView
{
    #region Linker
    [Linker("Root/StageGroup")]
    public GameObject _goStageGroup;
    [Linker("Root/StageGroup/Spawn_Stage_1",
    "Root/StageGroup/Spawn_Stage_2",
    "Root/StageGroup/Spawn_Stage_3")]
    public List<Spawn> _spawnStageList = new List<Spawn>();
    [Linker("Root/Button_Close")]
    public Button _buttonClose;
    #endregion Linker


}
