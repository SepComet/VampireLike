using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class DisplayItemInfoFormController : UIFormControllerBase<DisplayItemInfoFormContext>
    {
        private DisplayItemInfoFormContext _context;

        private DisplayItemInfoForm _itemInfoForm;

        private int? _itemInfoFormSerialId;

        private bool _pendingRefresh;

        private bool _isBindEvent = false;

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

        private static DisplayItemInfoFormContext BuildContext(DisplayItemInfoFormRawData rawData)
        {
            if (rawData == null)
            {
                return null;
            }

            return new DisplayItemInfoFormContext
            {
                IconAssetName = rawData.IconAssetName,
                Title = rawData.Title,
                Rarity = rawData.Rarity,
                TypeText = rawData.TypeText,
                Description = rawData.Description,
                Price = rawData.Price,
                IsWeapon = rawData.IsWeapon,
                TargetPos = rawData.TargetPos
            };
        }

        #region UI Methods

        protected override int? OpenUIInternal(DisplayItemInfoFormContext context)
        {
            if (context == null)
            {
                Log.Warning("ItemInfoFormController open failed. context is null.");
                return null;
            }

            _context = context;

            if (_itemInfoForm != null && _itemInfoFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_itemInfoFormSerialId.Value))
            {
                _itemInfoForm.RefreshUI(_context);
                return _itemInfoFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _itemInfoFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.DisplayItemInfoForm, context);
            return _itemInfoFormSerialId;
        }

        public int? OpenUI(DisplayItemInfoFormRawData rawData)
        {
            DisplayItemInfoFormContext context = BuildContext(rawData);
            return OpenUIInternal(context);
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is DisplayItemInfoFormContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData is DisplayItemInfoFormRawData rawData)
            {
                return OpenUI(rawData);
            }

            if (userData != null)
            {
                Log.Warning("DisplayItemInfoFormController.OpenUI() userData type is invalid.");
                return null;
            }
            
            return OpenUIInternal(_context);
        }

        public override void CloseUI()
        {
            _pendingRefresh = false;

            UnsubscribeEvents();

            if (_itemInfoFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_itemInfoFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_itemInfoFormSerialId.Value);
                }

                _itemInfoForm = null;
                _itemInfoFormSerialId = null;
                return;
            }

            if (_itemInfoForm != null)
            {
                GameEntry.UI.CloseUIForm(_itemInfoForm);
                _itemInfoForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is DisplayItemInfoFormUseCase))
            {
                Log.Error("DisplayItemInfoForm.BindUseCase() useCase is invalid.");
                return;
            }
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_itemInfoForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _itemInfoForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        #endregion

        #region EventHanlders

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_itemInfoFormSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _itemInfoFormSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _itemInfoForm = args.UIForm.Logic as DisplayItemInfoForm;

            if (_itemInfoForm == null)
            {
                Log.Warning("DisplayItemInfoFormController open success but form logic is invalid.");
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

            if (args.SerialId != _itemInfoFormSerialId)
            {
                return;
            }

            _itemInfoForm = null;
            _itemInfoFormSerialId = null;
        }

        #endregion
    }
}
