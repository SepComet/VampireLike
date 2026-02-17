using CustomEvent;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class LevelUpForm : UGuiForm
    {
        [SerializeField] private LevelUpRewardItem[] _propItems;

        [SerializeField] private TMP_Text _refreshButtonText;

        private LevelUpFormContext _context;

        public void RefreshUI(LevelUpFormContext context)
        {
            _context = context;
            
            foreach (var propItem in _propItems)
            {
                propItem.gameObject.SetActive(false);
            }

            if (_context.Props == null) return;
            int count = Mathf.Min(_propItems.Length, _context.Props.Count);
            for (int i = 0; i < count; i++)
            {
                _propItems[i].gameObject.SetActive(true);
                _propItems[i].Init(_context.Props[i]);
            }
            
            //刷新 -20 <sprite name="coin" index=0>
            _refreshButtonText.text = $"刷新 -{context.RefreshPrice} <sprite name=\"coin\" index=0>";
        }

        #region FSM

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (userData is LevelUpFormContext context)
            {
                RefreshUI(context);
                return;
            }

            Log.Warning("LevelUpForm requires LevelUpFormContext as userData.");
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            _context = null;
            base.OnClose(isShutdown, userData);
        }

        #endregion

        public void OnSelectButtonClick(int index)
        {
            GameEntry.Event.Fire(this, LevelUpPropSelectedEventArgs.Create(index));
        }

        public void OnRefreshButtonClick()
        {
            GameEntry.Event.Fire(this, RefreshEventArgs.Create(_context.RefreshPrice));
        }
    }
}
