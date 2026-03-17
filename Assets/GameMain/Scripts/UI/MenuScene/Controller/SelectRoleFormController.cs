using CustomEvent;
using Definition.Enum;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class SelectRoleFormController : UIFormControllerCommonBase<SelectRoleFormContext, SelectRoleForm>
    {
        private SelectRoleFormUseCase _useCase;

        protected override UIFormType UIFormTypeId => UIFormType.SelectRoleForm;

        protected override void RefreshUI(SelectRoleForm form, SelectRoleFormContext context)
        {
            form.RefreshUI(context);
        }

        protected override void SubscribeCustomEvents()
        {
            GameEntry.Event.Subscribe(MenuSelectRoleReturnEventArgs.EventId, OnMenuSelectRoleReturn);
            GameEntry.Event.Subscribe(MenuSelectRoleSelectedEventArgs.EventId, OnMenuSelectRoleSelected);
            GameEntry.Event.Subscribe(MenuSelectRoleConfirmEventArgs.EventId, OnMenuSelectRoleConfirm);
        }

        protected override void UnsubscribeCustomEvents()
        {
            GameEntry.Event.Unsubscribe(MenuSelectRoleReturnEventArgs.EventId, OnMenuSelectRoleReturn);
            GameEntry.Event.Unsubscribe(MenuSelectRoleSelectedEventArgs.EventId, OnMenuSelectRoleSelected);
            GameEntry.Event.Unsubscribe(MenuSelectRoleConfirmEventArgs.EventId, OnMenuSelectRoleConfirm);
        }

        private static SelectRoleFormContext BuildContext(SelectRoleFormRawData rawData)
        {
            if (rawData == null)
            {
                Log.Error("SelectRoleFormController.BuildContext() rawData is null.");
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

            if (_useCase == null)
            {
                Log.Error("SelectRoleFormController.OpenUI() useCase is null.");
                return null;
            }

            SelectRoleFormRawData rawData = _useCase.CreateModel();
            SelectRoleFormContext context = BuildContext(rawData);
            return OpenUIInternal(context);
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

        public void UpdateShowRole(RolePropertyAreaContext rolePropertyAreaContext)
        {
            if (Context == null)
            {
                Log.Error("SelectRoleFormController.UpdateShowRole() Context is null.");
                return;
            }

            Context.RolePropertyAreaContext = rolePropertyAreaContext;

            Form?.UpdateShowRole(rolePropertyAreaContext);
        }

        private bool IsCurrentFormEventSender(object sender)
        {
            if (sender is SelectRoleForm selectRoleForm)
            {
                return selectRoleForm == Form;
            }

            if (sender is Component component && Form != null)
            {
                return component.transform.IsChildOf(Form.transform);
            }

            return false;
        }

        private void OnMenuSelectRoleReturn(object sender, GameEventArgs e)
        {
            if ((SelectRoleForm)sender != Form || !(e is MenuSelectRoleReturnEventArgs))
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

            if (!IsCurrentFormEventSender(sender))
            {
                return;
            }

            if (_useCase == null)
            {
                Log.Error("SelectRoleFormController.OnMenuSelectRoleSelected() useCase is null.");
                return;
            }

            SelectRoleFormRawData rawData = _useCase.SelectRole(args.RoleId);
            SelectRoleFormContext context = BuildContext(rawData);
            if (context == null)
            {
                Log.Error("SelectRoleFormController.OnMenuSelectRoleSelected() context build failed.");
                return;
            }

            SetContext(context);
            UpdateShowRole(context.RolePropertyAreaContext);
        }

        private void OnMenuSelectRoleConfirm(object sender, GameEventArgs e)
        {
            if (!(e is MenuSelectRoleConfirmEventArgs))
            {
                return;
            }

            if (!IsCurrentFormEventSender(sender))
            {
                return;
            }

            if (_useCase == null)
            {
                Log.Error("SelectRoleFormController.OnMenuSelectRoleConfirm() useCase is null.");
                return;
            }

            _useCase.ConfirmSelectedRole();
        }
    }
}