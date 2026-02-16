using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class TUIForm : UGuiForm
    {

        public void RefreshUI(UIContext context)
        {
	    
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (!(userData is UIContext context))
            {
                Log.Error("UIContext is invalid.");
                return;
            }

            RefreshUI(context);
        }
    }
}