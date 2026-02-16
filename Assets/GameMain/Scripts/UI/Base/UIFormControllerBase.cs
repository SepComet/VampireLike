namespace UI
{
    public abstract class UIFormControllerBase<TContext> : IUIFormController where TContext : UIContext
    {
        protected abstract int? OpenUIInternal(TContext context);
        public abstract int? OpenUI(object userData = null);
        public abstract void CloseUI();
        public abstract void BindUseCase(IUIUseCase useCase);
    }
}
