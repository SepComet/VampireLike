using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class TUIFormController : UIFormControllerBase<UIContext>
    {
        private IUIUseCase _useCase;
        private UIContext _context;
        private TUIForm _tForm;
        private int? _tFormSerialId;
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

        protected override int? OpenUIInternal(UIContext context)
        {
            if (context == null)
            {
                Log.Warning("TUIFormController open failed. context is null.");
                return null;
            }

            _context = context;

            if (_tForm != null && _tFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_tFormSerialId.Value))
            {
                _tForm.RefreshUI(_context);
                return _tFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _tFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.TUIForm, context);
            return _tFormSerialId;
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is UIContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData != null)
            {
                Log.Warning("TUIFormController.OpenUI() userData type is invalid.");
                return null;
            }

            return OpenUIInternal(_context);
        }
        
        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();

            if (_tFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_tFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_tFormSerialId.Value);
                }

                _tForm = null;
                _tFormSerialId = null;
                return;
            }

            if (_tForm != null)
            {
                _tForm.Close();
                _tForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is IUIUseCase UIFormUseCase))
            {
                Log.Error("LevelUpForm.BindUseCase() useCase is invalid.");
                return;
            }

            _useCase = UIFormUseCase;
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_tForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _tForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        #region EventHanlders

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_tFormSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _tFormSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _tForm = args.UIForm.Logic as TUIForm;
            
            if (_tForm == null)
            {
                Log.Warning("DialogFormController open success but form logic is invalid.");
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

            if (args.SerialId != _tFormSerialId)
            {
                return;
            }

            _tForm = null;
            _tFormSerialId = null;
        }

        #endregion
    }
}
