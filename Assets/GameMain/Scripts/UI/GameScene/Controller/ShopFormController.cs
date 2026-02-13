using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class ShopFormController : IFormController<ShopFormContext>
    {
        #region Property

        private bool _pendingRefresh;

        private int? _shopFormSerialId;

        private ShopForm _shopForm;

        private ShopFormContext _context;

        #endregion

        public ShopFormController()
        {
            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }

        ~ShopFormController()
        {
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }

        public int? OpenUI(ShopFormContext context)
        {
            if (context == null)
            {
                Log.Warning("ShopFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_shopForm != null)
            {
                _shopForm.RefreshUI(_context);
                return _shopFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            _shopFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.ShopForm, context);
            return _shopFormSerialId;
        }

        public void CloseUI()
        {
            _pendingRefresh = false;

            if (_shopFormSerialId.HasValue)
            {
                GameEntry.UI.CloseUIForm(_shopFormSerialId.Value);
                return;
            }

            if (_shopForm != null)
            {
                _shopForm.Close();
            }
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_shopForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _shopForm.RefreshUI(_context);
            _pendingRefresh = false;
        }
        
        #region Event Handlers

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args)) return;

            if (!_shopFormSerialId.HasValue) return;

            if (args.UIForm == null || args.UIForm.SerialId != _shopFormSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _shopForm = args.UIForm.Logic as ShopForm;

            if (_shopForm == null)
            {
                Log.Warning("ShopFormController open success but form logic is invalid.");
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

            if (args.SerialId != _shopFormSerialId)
            {
                return;
            }

            _shopForm = null;
            _shopFormSerialId = null;
        }

        #endregion
    }
}