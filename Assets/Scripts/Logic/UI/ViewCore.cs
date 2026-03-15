using UnityEngine;

namespace SA.UI
{
    public abstract class ViewCore : MonoBehaviourEx
    {
        public abstract class ViewParam
        {
            
        }

        public virtual void PreOpen() { }
        public virtual void OnOpened() { }

        public virtual void PreClose() { }
        public virtual void OnClosed() { }

        protected ViewParam param;

        public void InitParam(ViewParam param)
        {
            this.param = param;
        }

        public void Close()
        {
            UIManager.Instance.CloseView(this);
        }

        public void CloseAll()
        {
            UIManager.Instance.CloseAllView();
        }
    }
}
