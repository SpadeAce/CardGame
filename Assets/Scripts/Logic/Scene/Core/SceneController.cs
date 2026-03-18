using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    private SceneBase _currentScene;

    public SceneController()
    {
        SceneManager.activeSceneChanged -= OnChangeScene;
        SceneManager.activeSceneChanged += OnChangeScene;
    }

    public void ChangeScene(string sceneName)
    {
        if (_currentScene != null && _currentScene.SceneName == sceneName)
            return;

        _currentScene?.OnExitScene();

        UIManager.Instance.CloseAllView();

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// 타이틀로 복귀. 모든 매니저의 런타임 데이터를 초기화한 후 TitleScene으로 전환한다.
    /// </summary>
    public void ReturnToTitle()
    {
        _currentScene?.OnExitScene();
        UIManager.Instance.CloseAllView();
        ResetAllManagers();
        SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);
    }

    private void ResetAllManagers()
    {
        var root = GameObject.Find("ManagerRoot");
        if (root == null) return;

        var resettables = root.GetComponentsInChildren<IResettable>();
        foreach (var r in resettables)
            r.ResetAll();
    }

    private void OnChangeScene(Scene old, Scene current)
    {
        var roots = current.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            var scene = roots[i].GetComponent<SceneBase>();

            if (scene != null)
            {
                _currentScene = scene;
                _currentScene.OnEnterScene();
                break;
            }
        }
    }
}
