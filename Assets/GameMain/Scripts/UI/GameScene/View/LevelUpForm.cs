using CustomEvent;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class LevelUpForm : UGuiForm
    {
        [SerializeField] private TMP_Text _titleText;

        [SerializeField] private LevelUpPropItem[] _propItems;

        private LevelUpFormContext _context;

        public void RefreshUI(LevelUpFormContext context)
        {
            _context = context;

            if (_titleText != null)
            {
                _titleText.text = $"Level Up (Lv.{_context.Level})";
            }

            foreach (var propItem in _propItems)
            {
                propItem.gameObject.SetActive(false);
            }

            if (_context.Props == null) return;
            for (int i = 0; i < _propItems.Length; i++)
            {
                _propItems[i].gameObject.SetActive(true);
                _propItems[i].Init(_context.Props[i]);
            }
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

        private void OnSelectProp(int index)
        {
            GameEntry.Event.Fire(this, LevelUpPropSelectedEventArgs.Create(index));
        }
    }
}