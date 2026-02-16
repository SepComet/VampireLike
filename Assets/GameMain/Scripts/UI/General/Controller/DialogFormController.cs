using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class DialogFormController : UIFormControllerBase<DialogFormContext>
    {
        private DialogFormContext _context;
        
        private DialogForm _dialogForm;
        
        private int? _dialogFormSerialId;
        
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

        private static DialogFormContext BuildContext(DialogFormRawData rawData)
        {
            if (rawData == null)
            {
                return null;
            }

            return new DialogFormContext
            {
                Mode = rawData.Mode,
                Title = rawData.Title,
                Message = rawData.Message,
                PauseGame = rawData.PauseGame,
                ConfirmText = rawData.ConfirmText,
                OnClickConfirm = rawData.OnClickConfirm,
                CancelText = rawData.CancelText,
                OnClickCancel = rawData.OnClickCancel,
                OtherText = rawData.OtherText,
                OnClickOther = rawData.OnClickOther,
                UserData = rawData.UserData
            };
        }

        protected override int? OpenUIInternal(DialogFormContext context)
        {
            if (context == null)
            {
                Log.Warning("DialogFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_dialogForm != null && _dialogFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_dialogFormSerialId.Value))
            {
                _dialogForm.RefreshUI(_context);
                return _dialogFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _dialogFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.DialogForm, context);
            return _dialogFormSerialId;
        }

        public int? OpenUI(DialogFormRawData rawData)
        {
            DialogFormContext context = BuildContext(rawData);
            return OpenUIInternal(context);
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is DialogFormContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData is DialogFormRawData rawData)
            {
                return OpenUI(rawData);
            }

            if (userData is DialogFormRawData dialogParams)
            {
                return OpenUIInternal(BuildContext(dialogParams));
            }

            if (userData != null)
            {
                Log.Warning("DialogFormController.OpenUI() userData type is invalid.");
                return null;
            }

            return OpenUIInternal(_context);
        }

        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();

            if (_dialogFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_dialogFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_dialogFormSerialId.Value);
                }

                _dialogForm = null;
                _dialogFormSerialId = null;
                return;
            }

            if (_dialogForm != null)
            {
                GameEntry.UI.CloseUIForm(_dialogForm);
                _dialogForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (useCase != null)
            {
                Log.Warning("DialogFormController does not use a use case.");
            }
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_dialogForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _dialogForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_dialogFormSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _dialogFormSerialId.Value ||
                args.UserData != _context)
            {
                return;
            }

            _dialogForm = args.UIForm.Logic as DialogForm;

            if (_dialogForm == null)
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

            if (args.SerialId != _dialogFormSerialId)
            {
                return;
            }

            _dialogForm = null;
            _dialogFormSerialId = null;
        }
    }
}
