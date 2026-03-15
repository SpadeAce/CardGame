using UnityEngine;

public class LobbyScript : SceneBase
{
    public override string SceneName { get { return "LobbyScene"; } }
    LobbyPage _lobbyPage = null;

    public override void OnExitScene()
    {

    }

    public override void OnEnterScene()
    {
        _lobbyPage = UIManager.Instance.OpenView<LobbyPage>();
    }
}
