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
        if (PawnManager.Instance.Pawns.Count == 0)
            DeckManager.Instance.InitTestData();

        _lobbyPage = UIManager.Instance.OpenView<LobbyPage>();
    }
}
