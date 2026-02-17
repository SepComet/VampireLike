using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public abstract class UIFormControllerCommonBase<TContext, TForm> : UIFormControllerBase<TContext>
        where TContext : UIContext
        where TForm : UGuiForm
    {
        private TContext _context;
        private TForm _form;
        private int? _formSerialId;
        private bool _pendingRefresh;
        private bool _isBindEvent;

        protected TContext Context => _context;

        protected TForm Form => _form;

        protected int? FormSerialId => _formSerialId;

        protected abstract UIFormType UIFormTypeId { get; }

        protected abstract void RefreshUI(TForm form, TContext context);

        protected virtual void SubscribeCustomEvents()
        {
        }

        protected virtual void UnsubscribeCustomEvents()
        {
        }

        protected virtual void CloseLoadedFormDirect(TForm form)
        {
            form.Close();
        }

        protected void SetContext(TContext context)
        {
            _context = context;
        }

        protected void RefreshCurrentUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_form == null)
            {
                _pendingRefresh = true;
                return;
            }

            RefreshUI(_form, _context);
            _pendingRefresh = false;
        }

        protected override int? OpenUIInternal(TContext context)
        {
            if (context == null)
            {
                Log.Warning("{0}.OpenUI() context is null.", GetType().Name);
                return null;
            }

            _context = context;

            if (_form != null && _formSerialId.HasValue && GameEntry.UI.HasUIForm(_formSerialId.Value))
            {
                RefreshUI(_form, _context);
                return _formSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _formSerialId = GameEntry.UI.OpenUIForm(UIFormTypeId, _context);
            return _formSerialId;
        }

        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();

            if (_formSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_formSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_formSerialId.Value);
                }

                _form = null;
                _formSerialId = null;
                return;
            }

            if (_form != null)
            {
                CloseLoadedFormDirect(_form);
                _form = null;
            }
        }

        private void SubscribeEvents()
        {
            if (_isBindEvent)
            {
                return;
            }

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            SubscribeCustomEvents();

            _isBindEvent = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isBindEvent)
            {
                return;
            }

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            UnsubscribeCustomEvents();

            _isBindEvent = false;
        }

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_formSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _formSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _form = args.UIForm.Logic as TForm;
            if (_form == null)
            {
                Log.Warning("{0} open success but form logic is invalid.", GetType().Name);
                return;
            }

            if (_pendingRefresh)
            {
                RefreshCurrentUI();
            }
        }

        private void CloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args))
            {
                return;
            }

            if (args.SerialId != _formSerialId)
            {
                return;
            }

            _form = null;
            _formSerialId = null;
        }
    }
}
