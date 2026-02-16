using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class HudFormController : UIFormControllerBase<HudFormContext>
    {
        private HudFormContext _context;

        private HudForm _hudForm;

        private int? _hudFormSerialId;

        private bool _pendingRefresh;

        private bool _isBindEvent;

        private void SubscribeEvents()
        {
            if (_isBindEvent) return;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);

            _isBindEvent = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isBindEvent) return;

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);

            _isBindEvent = false;
        }

        private static HudFormContext BuildHudFormContext()
        {
            return new HudFormContext();
        }

        #region UI Methods

        protected override int? OpenUIInternal(HudFormContext context)
        {
            if (context == null)
            {
                Log.Warning("HudFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_hudForm != null && _hudFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_hudFormSerialId.Value))
            {
                _hudForm.RefreshUI(_context);
                return _hudFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _hudFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.HudForm, context);
            return _hudFormSerialId;
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is HudFormContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData != null)
            {
                Log.Warning("HudFormController.OpenUI() userData type is invalid.");
                return null;
            }

            return OpenUIInternal(BuildHudFormContext());
        }

        public int? OpenUI(HudFormContext context)
        {
            return OpenUIInternal(context);
        }

        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();

            if (_hudFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_hudFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_hudFormSerialId.Value);
                }

                _hudForm = null;
                _hudFormSerialId = null;
                return;
            }

            if (_hudForm != null)
            {
                _hudForm.Close();
                _hudForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            Log.Info("HudFormController doesn't need UseCase");
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_hudForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _hudForm.RefreshUI(_context);
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

            if (!_hudFormSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _hudFormSerialId.Value ||
                args.UserData != _context)
            {
                return;
            }

            _hudForm = args.UIForm.Logic as HudForm;

            if (_hudForm == null)
            {
                Log.Warning("HudFormController open success but form logic is invalid.");
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

            if (args.SerialId != _hudFormSerialId)
            {
                return;
            }

            _hudForm = null;
            _hudFormSerialId = null;
        }

        #endregion
    }
}
