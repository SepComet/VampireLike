using CustomEvent;
using UnityGameFramework.Runtime;

namespace UI
{
    public class StartMenuForm : UGuiForm
    {
        public void RefreshUI(StartMenuFormContext context)
        {
        }

        #region Lifecycle

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (userData is StartMenuFormContext context)
            {
                RefreshUI(context);
                return;
            }

            Log.Warning("StartMenuForm requires StartMenuFormContext as userData.");
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
        }

        #endregion

        public void OnStartButtonClick()
        {
            GameEntry.Event.Fire(this, MenuStartGameEventArgs.Create());
        }

        public void OnFileButtonClick()
        {
            GameEntry.Event.Fire(this, MenuFileButtonClickEventArgs.Create());
        }

        public void OnGuideButtonClick()
        {
            GameEntry.Event.Fire(this, MenuGuideButtonClickEventArgs.Create());
        }

        public void OnSettingButtonClick()
        {
            GameEntry.Event.Fire(this, MenuSettingButtonClickEventArgs.Create());
        }

        public void OnQuitButtonClick()
        {
            GameEntry.Event.Fire(this, MenuQuitButtonClickEventArgs.Create());
        }

        public void OnAboutButtonClick()
        {
            GameEntry.Event.Fire(this, MenuAboutButtonClickEventArgs.Create());
        }
    }
}
