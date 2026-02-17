using Definition.Enum;
using UnityGameFramework.Runtime;

namespace UI
{
    public class HudFormController : UIFormControllerCommonBase<HudFormContext, HudForm>
    {
        protected override UIFormType UIFormTypeId => UIFormType.HudForm;

        protected override void RefreshUI(HudForm form, HudFormContext context)
        {
            form.RefreshUI(context);
        }

        private static HudFormContext BuildHudFormContext()
        {
            return new HudFormContext();
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is HudFormContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData != null)
            {
                Log.Warning("HudFormController.OpenUI() userData type is invalid.");
                return null;
            }

            return OpenUIInternal(BuildHudFormContext());
        }

        public int? OpenUI(HudFormContext context)
        {
            return OpenUIInternal(context);
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            Log.Info("HudFormController doesn't need UseCase");
        }
    }
}
