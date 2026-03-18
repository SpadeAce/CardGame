using UnityEngine;
using UnityEngine.SceneManagement;

public class BootScript : SceneBase
{
    public override string SceneName { get { return "BootScene"; } }

    public override void OnExitScene() { }

    public override void OnEnterScene() { }

    private void Awake()
    {
        DataManager.Instance.LoadAll();
        TextManager.Instance.Initialize();
        SceneController.Instance.ChangeScene("TitleScene");
    }

}
