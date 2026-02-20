using CustomEvent;
using Definition.Enum;
using GameFramework.Event;
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

        #region Event Handlers

        private void DisplayItemInfoLock(object sender, GameEventArgs e)
        {
            if (!(e is DisplayItemInfoLockEventArgs)) return;

            _locked = true;
        }

        private void DisplayItemInfoHide(object sender, GameEventArgs e)
        {
            if (!(e is DisplayItemInfoHideEventArgs args)) return;

            if (!args.Force && _locked && sender is not DisplayItemInfoForm) return;

            GameEntry.UIRouter.CloseUI(UIFormType.DisplayItemInfoForm);
            _locked = false;
        }

        #endregion
    }
}