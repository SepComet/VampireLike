namespace UI
{
    public interface IFormController<TContext>
    {
        int? OpenUI(TContext context);
        void CloseUI();
    }
}
