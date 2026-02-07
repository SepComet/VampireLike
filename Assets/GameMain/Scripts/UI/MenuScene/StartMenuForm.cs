using Definition.Enum;
using Procedure;
using UnityGameFramework.Runtime;

namespace UI
{
    public class StartMenuForm : UGuiForm
    {
        private ProcedureStartMenu _procedureStartMenu = null;

        public void OnStartButtonClick()
        {
            _procedureStartMenu.OpenSelectForm();
        }

        public void OnFileButtonClick()
        {
            throw new System.NotImplementedException();
        }
        
        public void OnGuideButtonClick()
        {
            throw new System.NotImplementedException();
        }

        public void OnSettingButtonClick()
        {
            GameEntry.UI.OpenUIForm(UIFormType.SettingForm);
        }

        public void OnQuitButtonClick()
        {
            GameEntry.UI.OpenDialog(new DialogParams()
            {
                Mode = 2,
                Title = GameEntry.Localization.GetString("AskQuitGame.Title"),
                Message = GameEntry.Localization.GetString("AskQuitGame.Message"),
                OnClickConfirm = delegate(object userData)
                {
                    UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit);
                },
            });
        }

        public void OnAboutButtonClick()
        {
            throw new System.NotImplementedException();
        }

        #region Lifecycle

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (userData is ProcedureStartMenu procedure)
            {
                _procedureStartMenu = procedure;
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            _procedureStartMenu = null;
            
            base.OnClose(isShutdown, userData);
        }

        #endregion
    }
}