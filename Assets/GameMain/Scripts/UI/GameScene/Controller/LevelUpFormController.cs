using System.Collections.Generic;
using CustomEvent;
using DataTable;
using Definition.Enum;
using GameFramework.DataTable;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class LevelUpFormController : IFormController<LevelUpFormContext>
    {
        private int _currentLevel = 1;
        private int _buildCount = 4;

        private bool _pendingRefresh;

        private int? _levelUpFormSerialId;
        private LevelUpForm _levelUpForm;
        private LevelUpFormContext _context;

        public LevelUpFormController()
        {
            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }

        ~LevelUpFormController()
        {
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }

        public void ConfigureBuild(int level, int count = 4)
        {
            _currentLevel = Mathf.Max(1, level);
            _buildCount = Mathf.Max(0, count);
        }

        public int? OpenUI(LevelUpFormContext context)
        {
            if (context == null)
            {
                Log.Warning("LevelUpFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_levelUpForm != null)
            {
                _levelUpForm.RefreshUI(_context);
                return _levelUpFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            _levelUpFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.LevelUpForm, context);
            return _levelUpFormSerialId;
        }

        public void CloseUI()
        {
            _pendingRefresh = false;

            if (_levelUpFormSerialId.HasValue)
            {
                GameEntry.UI.CloseUIForm(_levelUpFormSerialId.Value);
                return;
            }

            if (_levelUpForm != null)
            {
                _levelUpForm.Close();
            }
        }

        public void Reset()
        {
            _context = null;
            _levelUpForm = null;
            _levelUpFormSerialId = null;
            _pendingRefresh = false;
        }

        public void OnSelectProp(int propId)
        {
            
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_levelUpForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _levelUpForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        #region Event Handlers

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args)) return;

            if (!_levelUpFormSerialId.HasValue) return;

            if (args.UIForm == null || args.UIForm.SerialId != _levelUpFormSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _levelUpForm = args.UIForm.Logic as LevelUpForm;

            if (_levelUpForm == null)
            {
                Log.Warning("LevelUpFormController open success but form logic is invalid.");
                return;
            }

            if (_pendingRefresh)
            {
                TryRefreshUI();
            }
        }

        private void CloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args))
            {
                return;
            }

            if (args.SerialId != _levelUpFormSerialId)
            {
                return;
            }

            _levelUpForm = null;
            _levelUpFormSerialId = null;
        }

        #endregion
    }
}