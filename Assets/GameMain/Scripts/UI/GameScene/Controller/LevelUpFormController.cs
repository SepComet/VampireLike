using System.Collections.Generic;
using CustomEvent;
using Definition.Enum;
using CustomUtility;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class LevelUpFormController : UIFormControllerCommonBase<LevelUpFormContext, LevelUpForm>
    {
        private LevelUpFormUseCase _useCase;

        protected override UIFormType UIFormTypeId => UIFormType.LevelUpForm;

        protected override void RefreshUI(LevelUpForm form, LevelUpFormContext context)
        {
            form.RefreshUI(context);
        }

        protected override void SubscribeCustomEvents()
        {
            GameEntry.Event.Subscribe(RefreshEventArgs.EventId, OnRefresh);
            GameEntry.Event.Subscribe(LevelUpPropSelectedEventArgs.EventId, OnLevelUpPropSelected);
        }

        protected override void UnsubscribeCustomEvents()
        {
            GameEntry.Event.Unsubscribe(RefreshEventArgs.EventId, OnRefresh);
            GameEntry.Event.Unsubscribe(LevelUpPropSelectedEventArgs.EventId, OnLevelUpPropSelected);
        }

        private static LevelUpFormContext BuildContext(LevelUpFormRawData rawData)
        {
            if (rawData == null)
            {
                Log.Error("LevelUpFormController.BuildContext() rawData is null.");
                return null;
            }

            if (rawData.Rewards == null)
            {
                Log.Error("LevelUpFormController.BuildContext() rewards are null.");
                return null;
            }

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
                    ItemRarity =  reward.Rarity,
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

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is LevelUpFormUseCase levelUpFormUseCase))
            {
                Log.Error("LevelUpForm.BindUseCase() useCase is invalid.");
                return;
            }

            _useCase = levelUpFormUseCase;
        }

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

        private void OnRefresh(object sender, GameEventArgs e)
        {
            if (sender != Form)
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
            if (sender != Form)
            {
                return;
            }

            if (!(e is LevelUpPropSelectedEventArgs args))
            {
                return;
            }

            SelectReward(args.SelectedId);
        }
    }
}
