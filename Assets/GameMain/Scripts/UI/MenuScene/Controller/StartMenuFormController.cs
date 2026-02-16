using CustomEvent;
using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class StartMenuFormController : UIFormControllerBase<StartMenuFormContext>
    {
        private StartMenuFormContext _context;

        private StartMenuForm _startMenuForm;

        private int? _startMenuFormSerialId;

        private bool _pendingRefresh;

        private bool _isBindEvent;

        private void SubscribeEvents()
        {
            if (_isBindEvent) return;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);

            GameEntry.Event.Subscribe(MenuStartGameEventArgs.EventId, OnMenuStartGameButtonClick);
            GameEntry.Event.Subscribe(MenuFileButtonClickEventArgs.EventId, OnMenuFileButtonClick);
            GameEntry.Event.Subscribe(MenuGuideButtonClickEventArgs.EventId, OnMenuGuideButtonClick);
            GameEntry.Event.Subscribe(MenuSettingButtonClickEventArgs.EventId, OnMenuSettingButtonClick);
            GameEntry.Event.Subscribe(MenuQuitButtonClickEventArgs.EventId, OnMenuQuitButtonClick);
            GameEntry.Event.Subscribe(MenuAboutButtonClickEventArgs.EventId, OnMenuAboutButtonClick);

            _isBindEvent = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isBindEvent) return;

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);

            GameEntry.Event.Unsubscribe(MenuStartGameEventArgs.EventId, OnMenuStartGameButtonClick);
            GameEntry.Event.Unsubscribe(MenuFileButtonClickEventArgs.EventId, OnMenuFileButtonClick);
            GameEntry.Event.Unsubscribe(MenuGuideButtonClickEventArgs.EventId, OnMenuGuideButtonClick);
            GameEntry.Event.Unsubscribe(MenuSettingButtonClickEventArgs.EventId, OnMenuSettingButtonClick);
            GameEntry.Event.Unsubscribe(MenuQuitButtonClickEventArgs.EventId, OnMenuQuitButtonClick);
            GameEntry.Event.Unsubscribe(MenuAboutButtonClickEventArgs.EventId, OnMenuAboutButtonClick);

            _isBindEvent = false;
        }

        private static StartMenuFormContext BuildStartMenuFormContext()
        {
            return new StartMenuFormContext();
        }
        
        #region UI Methods

        protected override int? OpenUIInternal(StartMenuFormContext context)
        {
            if (context == null)
            {
                Log.Warning("StartMenuFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_startMenuForm != null && _startMenuFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_startMenuFormSerialId.Value))
            {
                _startMenuForm.RefreshUI(_context);
                return _startMenuFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _startMenuFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.StartMenuForm, context);
            return _startMenuFormSerialId;
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

        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();

            if (_startMenuFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_startMenuFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_startMenuFormSerialId.Value);
                }

                _startMenuForm = null;
                _startMenuFormSerialId = null;
                return;
            }

            if (_startMenuForm != null)
            {
                _startMenuForm.Close();
                _startMenuForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            Log.Info("StartMenuForm doesn't need UseCase");
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_startMenuForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _startMenuForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        #endregion


        #region Event Handlers

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_startMenuFormSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _startMenuFormSerialId.Value ||
                args.UserData != _context)
            {
                return;
            }

            _startMenuForm = args.UIForm.Logic as StartMenuForm;

            if (_startMenuForm == null)
            {
                Log.Warning("StartMenuFormController open success but form logic is invalid.");
                return;
            }

            if (_pendingRefresh)
            {
                TryRefreshUI();
            }
        }

        private void CloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args))
            {
                return;
            }

            if (args.SerialId != _startMenuFormSerialId)
            {
                return;
            }

            _startMenuForm = null;
            _startMenuFormSerialId = null;
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

        #endregion
    }
}
