using UnityEngine;

public abstract class SceneBase : MonoBehaviour
{
    public abstract string SceneName { get; }
    
    public abstract void OnExitScene();
    public abstract void OnEnterScene();
}