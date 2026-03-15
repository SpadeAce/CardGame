using UnityEngine;

public class TitleScript : SceneBase
{
    public override string SceneName { get { return "TitleScene"; } }
    TitlePage _titlePage = null;

    public override void OnExitScene()
    {

    }

    public override void OnEnterScene()
    {
        _titlePage = UIManager.Instance.OpenView<TitlePage>();
    }
}
