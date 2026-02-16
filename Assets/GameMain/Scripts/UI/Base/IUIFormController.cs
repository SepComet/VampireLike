namespace UI
{
    public interface IUIFormController
    {
        int? OpenUI(object userData = null);
        void CloseUI();
        void BindUseCase(IUIUseCase useCase);
    }
}
