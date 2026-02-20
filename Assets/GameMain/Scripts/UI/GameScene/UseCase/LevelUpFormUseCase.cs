using System.Collections.Generic;
using System.Linq;
using DataTable;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
using CustomUtility;
using GameFramework.DataTable;

namespace UI
{
    internal class LevelUpFormUseCase : IUIUseCase
    {
        private const int BaseRefreshPrice = 2;
        private readonly GameStateLevelUp _gameStateLevelUp;
        private readonly DRLevelUpReward[] _allRewards;
        private readonly IDataTable<DRLevelRarity> _levelRarityTable;
        private readonly Dictionary<ItemRarity, List<DRLevelUpReward>> _rewardsByRarity = new();
        private readonly List<DRLevelUpReward> _validRewards = new();

        private int _refreshCount;
        private LevelUpFormRawData _currentModel;
        
        private readonly Player _player;

        public LevelUpFormUseCase(Player player, GameStateLevelUp gameStateLevelUp)
        {
            _player = player;
            _gameStateLevelUp = gameStateLevelUp;
            _allRewards = GameEntry.DataTable.GetDataTable<DRLevelUpReward>().ToArray();
            _levelRarityTable = GameEntry.DataTable.GetDataTable<DRLevelRarity>();

            BuildRewardRarityPools();
        }

        public LevelUpFormRawData CreateInitialModel(int count = 4)
        {
            _refreshCount = 0;

            if (_gameStateLevelUp == null || _player.PendingLevelPoints <= 0)
            {
                _currentModel = null;
                return null;
            }

            _currentModel = BuildModel(BaseRefreshPrice, count);
            return _currentModel;
        }

        public LevelUpFormRawData TryRefresh(int refreshCost, int count = 4)
        {
            if (_currentModel == null)
            {
                return null;
            }

            if (_player == null || _player.Coin < refreshCost)
            {
                return null;
            }

            _player.Coin -= refreshCost;

            _refreshCount++;
            _currentModel = BuildModel((_refreshCount + 1) * BaseRefreshPrice, count);
            return _currentModel;
        }

        public LevelUpFormRawData SelectReward(int selectedIndex, int count = 4)
        {
            if (_currentModel?.Rewards == null)
            {
                return null;
            }

            if (selectedIndex < 0 || selectedIndex >= _currentModel.Rewards.Count)
            {
                Log.Warning("LevelUpFormUseCase::SelectReward: Invalid index");
                return null;
            }

            ApplyReward(_currentModel.Rewards[selectedIndex]);

            if (--_player.PendingLevelPoints > 0)
            {
                _refreshCount = 0;
                _currentModel = BuildModel(BaseRefreshPrice, count);
                return _currentModel;
            }

            _gameStateLevelUp.IsCompleted = true;
            _currentModel = null;
            return null;
        }

        private LevelUpFormRawData BuildModel(int refreshPrice, int count)
        {
            if (_allRewards == null || _allRewards.Length == 0)
            {
                Log.Error("LevelUpFormUseCase::BuildModel(): _allRewards is empty");
                return null;
            }

            int finalCount = count > 0 ? System.Math.Min(count, _allRewards.Length) : _allRewards.Length;
            List<DRLevelUpReward> selections = new List<DRLevelUpReward>(finalCount);

            int currentLevel = _player != null ? _player.CurrentLevel : 1;
            for (int i = 0; i < finalCount; i++)
            {
                ItemRarity rarity = RarityUtility.SelectRarityForLevel(_levelRarityTable, currentLevel);
                DRLevelUpReward reward = PickRewardByRarity(rarity);
                if (reward == null)
                {
                    Log.Warning("LevelUpFormUseCase::BuildModel(): No available reward for selection.");
                    break;
                }

                selections.Add(reward);
            }

            return new LevelUpFormRawData
            {
                Rewards = selections,
                RefreshPrice = refreshPrice
            };
        }

        private void ApplyReward(DRLevelUpReward reward)
        {
            if (reward == null)
            {
                return;
            }

            if (_player == null)
            {
                Log.Warning("LevelUpFormUseCase::ApplyReward: Player is null");
                return;
            }

            if (reward.Modifiers == null || reward.Modifiers.Length == 0)
            {
                Log.Warning("LevelUpFormUseCase::ApplyReward: Reward modifiers are empty");
                return;
            }

            _player.AddProp(new PropItem(reward.Modifiers, reward.Rarity, reward.Title, reward.IconAssetName));
        }

        private void BuildRewardRarityPools()
        {
            _rewardsByRarity.Clear();
            _validRewards.Clear();

            if (_allRewards == null) return;

            foreach (DRLevelUpReward reward in _allRewards)
            {
                if (reward == null)
                {
                    continue;
                }

                ItemRarity rarity = reward.Rarity;
                _validRewards.Add(reward);

                if (!_rewardsByRarity.TryGetValue(rarity, out List<DRLevelUpReward> list))
                {
                    list = new List<DRLevelUpReward>();
                    _rewardsByRarity.Add(rarity, list);
                }

                list.Add(reward);
            }
        }

        private DRLevelUpReward PickRewardByRarity(ItemRarity rarity)
        {
            if (_rewardsByRarity.TryGetValue(rarity, out List<DRLevelUpReward> list) && list.Count > 0)
            {
                return list[Random.Range(0, list.Count)];
            }

            if (_validRewards.Count > 0)
            {
                return _validRewards[Random.Range(0, _validRewards.Count)];
            }

            return null;
        }
    }
}
