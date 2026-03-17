using CustomEvent;
using Definition.Enum;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class DisplayItemInfoFormController : UIFormControllerCommonBase<
        DisplayItemInfoFormContext, DisplayItemInfoForm>
    {
        protected override UIFormType UIFormTypeId => UIFormType.DisplayItemInfoForm;

        private bool _locked = false;

        protected override void SubscribeCustomEvents()
        {
            GameEntry.Event.Subscribe(DisplayItemInfoLockEventArgs.EventId, DisplayItemInfoLock);
            GameEntry.Event.Subscribe(DisplayItemInfoHideEventArgs.EventId, DisplayItemInfoHide);
        }

        protected override void UnsubscribeCustomEvents()
        {
            GameEntry.Event.Unsubscribe(DisplayItemInfoLockEventArgs.EventId, DisplayItemInfoLock);
            GameEntry.Event.Unsubscribe(DisplayItemInfoHideEventArgs.EventId, DisplayItemInfoHide);
        }

        protected override void RefreshUI(DisplayItemInfoForm form, DisplayItemInfoFormContext context)
        {
            form.RefreshUI(context);
        }

        protected override void CloseLoadedFormDirect(DisplayItemInfoForm form)
        {
            GameEntry.UI.CloseUIForm(form);
        }

        private static DisplayItemInfoFormContext BuildContext(DisplayItemInfoFormRawData rawData)
        {
            if (rawData == null)
            {
                Log.Error("DisplayItemInfoFormController.BuildContext() rawData is null.");
                return null;
            }

            return new DisplayItemInfoFormContext
            {
                Index = rawData.Index,
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

        public int? OpenUI(DisplayItemInfoFormRawData rawData)
        {
            _locked = false;
            DisplayItemInfoFormContext context = BuildContext(rawData);
            return OpenUIInternal(context);
        }

        public override int? OpenUI(object userData = null)
        {
            _locked = false;
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

            return OpenUIInternal(Context);
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is DisplayItemInfoFormUseCase))
            {
                Log.Error("DisplayItemInfoForm.BindUseCase() useCase is invalid.");
            }
        }

        private bool IsCurrentFormSender(object sender)
        {
            if (sender is DisplayItemInfoForm displayItemInfoForm)
            {
                return displayItemInfoForm == Form;
            }

            if (sender is Component component && Form != null)
            {
                return component.transform.IsChildOf(Form.transform);
            }

            return false;
        }

        #region Event Handlers

        private void DisplayItemInfoLock(object sender, GameEventArgs e)
        {
            if (!(e is DisplayItemInfoLockEventArgs))
            {
                return;
            }

            if (Context == null)
            {
                Log.Error("DisplayItemInfoFormController.DisplayItemInfoLock() Context is null.");
                return;
            }

            if (Form == null)
            {
                Log.Error("DisplayItemInfoFormController.DisplayItemInfoLock() Form is null.");
                return;
            }

            _locked = true;
        }

        private void DisplayItemInfoHide(object sender, GameEventArgs e)
        {
            if (!(e is DisplayItemInfoHideEventArgs args))
            {
                return;
            }

            if (Context == null)
            {
                Log.Error("DisplayItemInfoFormController.DisplayItemInfoHide() Context is null.");
                return;
            }

            if (Form == null)
            {
                Log.Error("DisplayItemInfoFormController.DisplayItemInfoHide() Form is null.");
                return;
            }

            if (args.Force)
            {
                GameEntry.UIRouter.CloseUI(UIFormType.DisplayItemInfoForm);
                _locked = false;
                return;
            }

            if (_locked && !IsCurrentFormSender(sender) && sender is not DisplayItem)
            {
                return;
            }

            GameEntry.UIRouter.CloseUI(UIFormType.DisplayItemInfoForm);
            _locked = false;
        }

        #endregion
    }
}
