using GameFramework;

namespace UI
{
    public class DialogFormContext : UIContext
    {
        public int Mode;
        public string Title;
        public string Message;
        public bool PauseGame;
        public string ConfirmText;
        public GameFrameworkAction<object> OnClickConfirm;
        public string CancelText;
        public GameFrameworkAction<object> OnClickCancel;
        public string OtherText;
        public GameFrameworkAction<object> OnClickOther;
        public object UserData;
    }
}
