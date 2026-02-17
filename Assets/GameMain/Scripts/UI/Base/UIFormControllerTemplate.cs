using Definition.Enum;
using UnityGameFramework.Runtime;

namespace UI
{
    public class TUIFormController : UIFormControllerCommonBase<UIContext, TUIForm>
    {
        private IUIUseCase _useCase;

        protected override UIFormType UIFormTypeId => UIFormType.TUIForm;

        protected override void RefreshUI(TUIForm form, UIContext context)
        {
            form.RefreshUI(context);
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

            return OpenUIInternal(Context);
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is IUIUseCase uiFormUseCase))
            {
                Log.Error("LevelUpForm.BindUseCase() useCase is invalid.");
                return;
            }

            _useCase = uiFormUseCase;
        }
    }
}
