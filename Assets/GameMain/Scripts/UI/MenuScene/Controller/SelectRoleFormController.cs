using CustomEvent;
using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class SelectRoleFormController : UIFormControllerBase<SelectRoleFormContext>
    {
        private SelectRoleFormUseCase _useCase;

        private SelectRoleFormContext _context;

        private SelectRoleForm _selectRoleForm;

        private int? _selectRoleFormSerialId;

        private bool _pendingRefresh;

        private bool _isBindEvent;

        private void SubscribeEvents()
        {
            if (_isBindEvent) return;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            GameEntry.Event.Subscribe(MenuSelectRoleReturnEventArgs.EventId, OnMenuSelectRoleReturn);
            GameEntry.Event.Subscribe(MenuSelectRoleSelectedEventArgs.EventId, OnMenuSelectRoleSelected);
            GameEntry.Event.Subscribe(MenuSelectRoleConfirmEventArgs.EventId, OnMenuSelectRoleConfirm);

            _isBindEvent = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isBindEvent) return;

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            GameEntry.Event.Unsubscribe(MenuSelectRoleReturnEventArgs.EventId, OnMenuSelectRoleReturn);
            GameEntry.Event.Unsubscribe(MenuSelectRoleSelectedEventArgs.EventId, OnMenuSelectRoleSelected);
            GameEntry.Event.Unsubscribe(MenuSelectRoleConfirmEventArgs.EventId, OnMenuSelectRoleConfirm);

            _isBindEvent = false;
        }

        private static SelectRoleFormContext BuildContext(SelectRoleFormRawData rawData)
        {
            if (rawData == null)
            {
                return null;
            }

            int count = rawData.RoleIds?.Length ?? 0;
            RoleItemContext[] roleItems = new RoleItemContext[count];
            for (int i = 0; i < count; i++)
            {
                string iconName = rawData.RoleIconNames != null && i < rawData.RoleIconNames.Length
                    ? rawData.RoleIconNames[i]
                    : null;

                roleItems[i] = new RoleItemContext
                {
                    RoleId = rawData.RoleIds[i],
                    IconName = iconName
                };
            }

            RolePropertyAreaContext propertyContext = null;
            if (rawData.SelectedRoleId >= 0)
            {
                propertyContext = new RolePropertyAreaContext
                {
                    RoleName = rawData.SelectedRoleName,
                    InitialPropertyText = rawData.SelectedRoleInitialPropertyText
                };
            }

            return new SelectRoleFormContext
            {
                RoleItemContexts = roleItems,
                RolePropertyAreaContext = propertyContext,
                RoleIds = rawData.RoleIds
            };
        }

        #region UI Methods

        protected override int? OpenUIInternal(SelectRoleFormContext context)
        {
            if (context == null)
            {
                Log.Warning("SelectRoleFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_selectRoleForm != null && _selectRoleFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_selectRoleFormSerialId.Value))
            {
                _selectRoleForm.RefreshUI(_context);
                return _selectRoleFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _selectRoleFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.SelectRoleForm, context);
            return _selectRoleFormSerialId;
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is SelectRoleFormContext selectRoleFormContext)
            {
                return OpenUIInternal(selectRoleFormContext);
            }

            if (userData != null)
            {
                Log.Warning("SelectRoleFormController.OpenUI() userData type is invalid.");
                return null;
            }

            SelectRoleFormRawData rawData = _useCase.CreateModel();
            SelectRoleFormContext context = BuildContext(rawData);
            return OpenUIInternal(context);
        }

        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();

            if (_selectRoleFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_selectRoleFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_selectRoleFormSerialId.Value);
                }

                _selectRoleForm = null;
                _selectRoleFormSerialId = null;
                return;
            }

            if (_selectRoleForm != null)
            {
                _selectRoleForm.Close();
                _selectRoleForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is SelectRoleFormUseCase selectRoleUseCase))
            {
                Log.Error("SelectRoleUseCase.BindUseCase() useCase is invalid.");
                return;
            }

            _useCase = selectRoleUseCase;
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_selectRoleForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _selectRoleForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        #endregion

        #region Service

        public void UpdateShowRole(RolePropertyAreaContext rolePropertyAreaContext)
        {
            if (_context != null)
            {
                _context.RolePropertyAreaContext = rolePropertyAreaContext;
            }

            if (_selectRoleForm != null)
            {
                _selectRoleForm.UpdateShowRole(rolePropertyAreaContext);
            }
        }

        #endregion

        #region Event Handlers

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_selectRoleFormSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _selectRoleFormSerialId.Value ||
                args.UserData != _context)
            {
                return;
            }

            _selectRoleForm = args.UIForm.Logic as SelectRoleForm;

            if (_selectRoleForm == null)
            {
                Log.Warning("SelectRoleFormController open success but form logic is invalid.");
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

            if (args.SerialId != _selectRoleFormSerialId)
            {
                return;
            }

            _selectRoleForm = null;
            _selectRoleFormSerialId = null;
        }

        private void OnMenuSelectRoleReturn(object sender, GameEventArgs e)
        {
            if (!(sender is SelectRoleForm) || !(e is MenuSelectRoleReturnEventArgs))
            {
                return;
            }

            CloseUI();
        }

        private void OnMenuSelectRoleSelected(object sender, GameEventArgs e)
        {
            if (!(e is MenuSelectRoleSelectedEventArgs args))
            {
                return;
            }

            SelectRoleFormRawData rawData = _useCase != null ? _useCase.SelectRole(args.RoleId) : null;
            SelectRoleFormContext context = BuildContext(rawData);
            if (context == null)
            {
                return;
            }

            _context = context;
            UpdateShowRole(_context.RolePropertyAreaContext);
        }

        private void OnMenuSelectRoleConfirm(object sender, GameEventArgs e)
        {
            if (!(e is MenuSelectRoleConfirmEventArgs))
            {
                return;
            }

            _useCase.ConfirmSelectedRole();
        }

        #endregion
    }
}
