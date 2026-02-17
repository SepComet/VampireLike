using CustomEvent;
using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class StartMenuFormController : UIFormControllerCommonBase<StartMenuFormContext, StartMenuForm>
    {
        protected override UIFormType UIFormTypeId => UIFormType.StartMenuForm;

        protected override void RefreshUI(StartMenuForm form, StartMenuFormContext context)
        {
            form.RefreshUI(context);
        }

        protected override void SubscribeCustomEvents()
        {
            GameEntry.Event.Subscribe(MenuStartGameEventArgs.EventId, OnMenuStartGameButtonClick);
            GameEntry.Event.Subscribe(MenuFileButtonClickEventArgs.EventId, OnMenuFileButtonClick);
            GameEntry.Event.Subscribe(MenuGuideButtonClickEventArgs.EventId, OnMenuGuideButtonClick);
            GameEntry.Event.Subscribe(MenuSettingButtonClickEventArgs.EventId, OnMenuSettingButtonClick);
            GameEntry.Event.Subscribe(MenuQuitButtonClickEventArgs.EventId, OnMenuQuitButtonClick);
            GameEntry.Event.Subscribe(MenuAboutButtonClickEventArgs.EventId, OnMenuAboutButtonClick);
        }

        protected override void UnsubscribeCustomEvents()
        {
            GameEntry.Event.Unsubscribe(MenuStartGameEventArgs.EventId, OnMenuStartGameButtonClick);
            GameEntry.Event.Unsubscribe(MenuFileButtonClickEventArgs.EventId, OnMenuFileButtonClick);
            GameEntry.Event.Unsubscribe(MenuGuideButtonClickEventArgs.EventId, OnMenuGuideButtonClick);
            GameEntry.Event.Unsubscribe(MenuSettingButtonClickEventArgs.EventId, OnMenuSettingButtonClick);
            GameEntry.Event.Unsubscribe(MenuQuitButtonClickEventArgs.EventId, OnMenuQuitButtonClick);
            GameEntry.Event.Unsubscribe(MenuAboutButtonClickEventArgs.EventId, OnMenuAboutButtonClick);
        }

        private static StartMenuFormContext BuildStartMenuFormContext()
        {
            return new StartMenuFormContext();
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is StartMenuFormContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData != null)
            {
                Log.Warning("StartMenuFormController.OpenUI() userData type is invalid.");
                return null;
            }

            return OpenUIInternal(BuildStartMenuFormContext());
        }

        public int? OpenUI(StartMenuFormContext context)
        {
            return OpenUIInternal(context);
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            Log.Info("StartMenuForm doesn't need UseCase");
        }

        private void OnMenuStartGameButtonClick(object sender, GameEventArgs e)
        {
            if (!(sender is StartMenuForm) || !(e is MenuStartGameEventArgs))
            {
                return;
            }

            GameEntry.UIRouter.OpenUI(UIFormType.SelectRoleForm);
        }

        private void OnMenuFileButtonClick(object sender, GameEventArgs e)
        {
            if (!(sender is StartMenuForm) || !(e is MenuFileButtonClickEventArgs))
            {
                return;
            }

            Log.Warning("Menu file button click is not implemented.");
        }

        private void OnMenuGuideButtonClick(object sender, GameEventArgs e)
        {
            if (!(sender is StartMenuForm) || !(e is MenuGuideButtonClickEventArgs))
            {
                return;
            }

            Log.Warning("Menu guide button click is not implemented.");
        }

        private void OnMenuSettingButtonClick(object sender, GameEventArgs e)
        {
            if (!(sender is StartMenuForm) || !(e is MenuSettingButtonClickEventArgs))
            {
                return;
            }

            GameEntry.UIRouter.OpenUI(UIFormType.SettingForm);
        }

        private void OnMenuQuitButtonClick(object sender, GameEventArgs e)
        {
            if (!(sender is StartMenuForm) || !(e is MenuQuitButtonClickEventArgs))
            {
                return;
            }

            GameEntry.UIRouter.OpenUI(UIFormType.DialogForm, new DialogFormRawData
            {
                Mode = 2,
                Title = GameEntry.Localization.GetString("AskQuitGame.Title"),
                Message = GameEntry.Localization.GetString("AskQuitGame.Message"),
                OnClickConfirm = delegate(object userData)
                {
                    UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit);
                }
            });
        }

        private void OnMenuAboutButtonClick(object sender, GameEventArgs e)
        {
            if (!(sender is StartMenuForm) || !(e is MenuAboutButtonClickEventArgs))
            {
                return;
            }

            Log.Warning("Menu about button click is not implemented.");
        }
    }
}
