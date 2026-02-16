using System.Collections.Generic;
using CustomEvent;
using Definition.Enum;
using Game.Utility;
using GameFramework.Event;
using Procedure;
using UnityGameFramework.Runtime;

namespace UI
{
    public class LevelUpFormController : UIFormControllerBase<LevelUpFormContext>
    {
        private LevelUpFormUseCase _useCase;

        private bool _pendingRefresh;

        private int? _levelUpFormSerialId;

        private LevelUpForm _levelUpForm;

        private LevelUpFormContext _context;

        private bool _isBindEvent;

        private void SubscribeEvents()
        {
            if (_isBindEvent) return;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            GameEntry.Event.Subscribe(RefreshEventArgs.EventId, OnRefresh);
            GameEntry.Event.Subscribe(LevelUpPropSelectedEventArgs.EventId, OnLevelUpPropSelected);

            _isBindEvent = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isBindEvent) return;

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            GameEntry.Event.Unsubscribe(RefreshEventArgs.EventId, OnRefresh);
            GameEntry.Event.Unsubscribe(LevelUpPropSelectedEventArgs.EventId, OnLevelUpPropSelected);

            _isBindEvent = false;
        }

        private static LevelUpFormContext BuildContext(LevelUpFormRawData rawData)
        {
            List<LevelUpRewardItemContext> props = new List<LevelUpRewardItemContext>(rawData.Rewards.Count);
            foreach (var reward in rawData.Rewards)
            {
                if (reward == null)
                {
                    continue;
                }

                props.Add(new LevelUpRewardItemContext
                {
                    Title = reward.Title,
                    Icon = null,
                    Description = ItemDescUtility.CreatePropDescription(reward.Modifiers),
                    IconAssetName = reward.IconAssetName
                });
            }

            return new LevelUpFormContext
            {
                RefreshPrice = rawData.RefreshPrice,
                Props = props
            };
        }


        #region UI Methods

        protected override int? OpenUIInternal(LevelUpFormContext context)
        {
            if (context == null)
            {
                Log.Warning("LevelUpFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_levelUpForm != null && _levelUpFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_levelUpFormSerialId.Value))
            {
                _levelUpForm.RefreshUI(_context);
                return _levelUpFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _levelUpFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.LevelUpForm, context);
            return _levelUpFormSerialId;
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is LevelUpFormContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData is LevelUpFormRawData rawDataFromUserData)
            {
                return OpenUI(rawDataFromUserData);
            }

            if (userData != null)
            {
                Log.Warning("LevelUpFormController.OpenUI() userData type is invalid.");
                return null;
            }

            if (_useCase == null)
            {
                Log.Error("LevelUpFormController.OpenUI() useCase is null.");
                return null;
            }

            LevelUpFormRawData rawData = _useCase.CreateInitialModel();
            return OpenUI(rawData);
        }

        public int? OpenUI(LevelUpFormRawData rawData)
        {
            LevelUpFormContext context = BuildContext(rawData);
            return OpenUIInternal(context);
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

        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();

            if (_levelUpFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_levelUpFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_levelUpFormSerialId.Value);
                }

                _levelUpForm = null;
                _levelUpFormSerialId = null;
                return;
            }

            if (_levelUpForm != null)
            {
                _levelUpForm.Close();
                _levelUpForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is LevelUpFormUseCase levelUpFormUseCase))
            {
                Log.Error("LevelUpForm.BindUseCase() useCase is invalid.");
                return;
            }

            _useCase = levelUpFormUseCase;
        }

        #endregion

        #region Service

        private void SelectReward(int selectedIndex)
        {
            if (_useCase == null)
            {
                Log.Error("LevelUpFormController.OpenUI() useCase is null.");
                return;
            }

            LevelUpFormRawData rawData = _useCase.SelectReward(selectedIndex);

            if (rawData == null)
            {
                return;
            }

            OpenUI(rawData);
        }

        private void RefreshRewardList(int refreshCost)
        {
            if (_useCase == null)
            {
                Log.Error("LevelUpFormController.OpenUI() useCase is null.");
                return;
            }

            LevelUpFormRawData rawData = _useCase.TryRefresh(refreshCost);
            if (rawData == null)
            {
                return;
            }

            OpenUI(rawData);
        }

        #endregion

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

        private void OnRefresh(object sender, GameEventArgs e)
        {
            if (!(sender is LevelUpForm))
            {
                return;
            }

            if (!(e is RefreshEventArgs args))
            {
                return;
            }

            RefreshRewardList(args.Cost);
        }

        private void OnLevelUpPropSelected(object sender, GameEventArgs e)
        {
            if (!(e is LevelUpPropSelectedEventArgs args))
            {
                return;
            }

            SelectReward(args.SelectedId);
        }

        #endregion
    }
}
