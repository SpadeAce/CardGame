using UnityEngine;

namespace SA.UI
{
    public abstract class PageView : ViewCore
    {
        [SerializeField] private bool _dontHide = false;
        public bool DontHide => _dontHide;
    }
}