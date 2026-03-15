using UnityEngine;
using UnityEngine.UI;

public class Localize : MonoBehaviour
{
    public string textAlias = string.Empty;
    
    void Start()
    {
        if(!string.IsNullOrEmpty(textAlias))
            GetComponent<Text>().text = TextManager.Instance.Get(textAlias);
    }
}
