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
