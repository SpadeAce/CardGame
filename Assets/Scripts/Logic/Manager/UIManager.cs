using SA.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[AssetPath("Prefabs/Manager/UIManager")]
public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField]
    private EventSystem _eventSystem;

    [SerializeField]
    private GameObject _viewRoot;

    [SerializeField]
    private Canvas _canvasPage;
    [SerializeField]
    private Canvas _canvasPopup;

    [SerializeField]
    private HudController _hudController;

    public HudController Hud => _hudController;

    private Dictionary<Type, ViewCore> _dicPage = new Dictionary<Type, ViewCore>();
    private Dictionary<Type, ViewCore> _dicPopup = new Dictionary<Type, ViewCore>();
    private Stack<PageView> _pageStack = new Stack<PageView>();

    private void Awake()
    {
        _hudController?.Init();
    }

    public T OpenView<T>(ViewCore.ViewParam param = null) where T : ViewCore
    {
        if(_dicPage.ContainsKey(typeof(T)))
        {
            return (T)_dicPage[typeof(T)];
        }
        else if(_dicPopup.ContainsKey(typeof(T)))
        {
            return (T)_dicPopup[typeof(T)];
        }

        var prefab = PrefabLoader.Load<T>();

        if (prefab == null)
            return null;

        var view = Instantiate(prefab, _viewRoot.transform);

        if (view == null)
            return null;

        view.name = prefab.name;
        view.InitParam(param);

        if (view is PageView pageView)
        {
            view.transform.SetParent(_canvasPage.transform, false);
            _dicPage.Add(typeof(T), view);

            if (_pageStack.Count > 0)
            {
                PageView topPage = _pageStack.Peek();
                if (!topPage.DontHide)
                    topPage.gameObject.SetActive(false);
            }
            _pageStack.Push(pageView);
        }
        else if(view is PopupView)
        {
            view.transform.SetParent(_canvasPopup.transform, false);
            _dicPopup.Add(typeof(T), view);
        }

        view.transform.SetAsLastSibling();
        view.PreOpen();




        view.OnOpened();

        return view;
    }

    public void CloseView(ViewCore view)
    {
        view.PreClose();
        if (_dicPage.ContainsValue(view))
        {
            _dicPage.Remove(view.GetType());

            if (_pageStack.Count > 0 && _pageStack.Peek() == view)
                _pageStack.Pop();

            DestroyImmediate(view.gameObject);
            view.OnClosed();

            if (_pageStack.Count > 0)
                _pageStack.Peek().gameObject.SetActive(true);
        }
        else if (_dicPopup.ContainsValue(view))
        {
            _dicPopup.Remove(view.GetType());

            DestroyImmediate(view.gameObject);
            view.OnClosed();
        }
    }

    public void CloseAllView()
    {
        foreach(var popup in _dicPopup.Values)
        {
            popup.PreClose();
            DestroyImmediate(popup.gameObject);
        }
        foreach(var page in _dicPage.Values)
        {
            page.PreClose();
            DestroyImmediate(page.gameObject);
        }
        _dicPage.Clear();
        _dicPopup.Clear();
        _pageStack.Clear();
        _hudController?.ClearAll();
    }
}
