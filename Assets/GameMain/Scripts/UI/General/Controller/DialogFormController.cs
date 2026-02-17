using Definition.Enum;
using UnityGameFramework.Runtime;

namespace UI
{
    public class DialogFormController : UIFormControllerCommonBase<DialogFormContext, DialogForm>
    {
        protected override UIFormType UIFormTypeId => UIFormType.DialogForm;

        protected override void RefreshUI(DialogForm form, DialogFormContext context)
        {
            form.RefreshUI(context);
        }

        protected override void CloseLoadedFormDirect(DialogForm form)
        {
            GameEntry.UI.CloseUIForm(form);
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

            if (userData != null)
            {
                Log.Warning("DialogFormController.OpenUI() userData type is invalid.");
                return null;
            }

            return OpenUIInternal(Context);
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (useCase != null)
            {
                Log.Warning("DialogFormController does not use a use case.");
            }
        }
    }
}
