using System.Collections.Generic;
using DataTable;
using Entity;
using Entity.EntityData;
using GameFramework.Event;
using Procedure;
using StarForce;
using UnityEngine;
using UnityGameFramework.Runtime;
using Random = UnityEngine.Random;

namespace CustomComponent
{
    public class EnemyManagerComponent : GameFrameworkComponent
    {
        private const float MinSpawnRateScale = 0.1f;

        private EntityComponent _entity;

        private List<EntityBase> _enemies;

        public List<EntityBase> Enemies => _enemies;

        private float _spawnEnemyTimer;
        
        private int _spawnEnemyMaxCount = 5000;
        
        private int _currentEnemyCount;
        
        private int _spawnDistanceFromPlayer = 20;
        
        private int _currentSpawnEnemyId;
        
        private int _currentLevel;

        private float[] _baseSpawnEnemyIntervals;
        private float[] _spawnEnemyIntervals;
        private int[] _spawnEnemyIds;
        private int[] _spawnEnemyCounts;
        private float _duration;
        private float _baseDuration;

        private float[] _nextSpawnTimes;
        private float _spawnRateScale = 1f;

        private Transform _player;

        private GameStateBattle _battle;

        public float SpawnRateScale => _spawnRateScale;
        public float BattleDuration => _duration;
        public float ElapsedBattleTime => _spawnEnemyTimer;

        #region FSM

        private void Start()
        {
            _entity = GameEntry.Entity;
            _enemies = new List<EntityBase>();

            GameEntry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            GameEntry.Event.Subscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
        }

        private void OnDestroy()
        {
            GameEntry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            GameEntry.Event.Unsubscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);

            _enemies = null;
            _entity = null;
        }

        public void OnInit(int level, GameStateBattle battle)
        {
            _battle = battle;

            _currentLevel = level;

            DRLevel levelData = GameEntry.DataTable.GetDataTableRow<DRLevel>(_currentLevel);
            _baseSpawnEnemyIntervals = (float[])levelData.Intervals.Clone();
            _spawnEnemyIntervals = (float[])_baseSpawnEnemyIntervals.Clone();
            _spawnEnemyIds = levelData.EntityIds;
            _spawnEnemyCounts = levelData.EntityCounts;
            _baseDuration = levelData.Duration;
            _duration = _baseDuration;

            SetSpawnRateScale(_spawnRateScale);

            _currentSpawnEnemyId = 0;
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            _spawnEnemyTimer += elapseSeconds;

            if (_spawnEnemyTimer > _duration)
            {
                _battle.LevelOver();
                return;
            }

            for (int i = 0; i < _nextSpawnTimes.Length; i++)
            {
                float nextSpawnTime = _nextSpawnTimes[i];
                if (_spawnEnemyTimer < nextSpawnTime) continue;
                for (int j = 0; j < _spawnEnemyCounts[i]; j++)
                {
                    SpawnEnemy(_spawnEnemyIds[i]);
                }

                _nextSpawnTimes[i] += _spawnEnemyIntervals[i];
            }
        }

        public void OnReset()
        {
            _currentEnemyCount = 0;

            _spawnEnemyTimer = 0;
            _currentSpawnEnemyId = 0;
            _currentLevel = 0;

            _baseSpawnEnemyIntervals = null;
            _spawnEnemyIntervals = null;
            _spawnEnemyIds = null;
            _spawnEnemyCounts = null;
            _baseDuration = 0;
            _duration = 0;

            _nextSpawnTimes = null;

            _battle = null;

            ClearEnemies();
        }

        public void SetSpawnRateScale(float scale)
        {
            float newScale = Mathf.Max(MinSpawnRateScale, scale);
            if (_baseSpawnEnemyIntervals == null || _baseSpawnEnemyIntervals.Length == 0)
            {
                _spawnRateScale = newScale;
                return;
            }

            bool hasRuntimeState = _nextSpawnTimes != null && _spawnEnemyIntervals != null &&
                                   _nextSpawnTimes.Length == _baseSpawnEnemyIntervals.Length &&
                                   _spawnEnemyIntervals.Length == _baseSpawnEnemyIntervals.Length;
            float oldScale = _spawnRateScale;
            _spawnRateScale = newScale;

            if (!hasRuntimeState)
            {
                for (int i = 0; i < _baseSpawnEnemyIntervals.Length; i++)
                {
                    _spawnEnemyIntervals[i] = GetScaledInterval(_baseSpawnEnemyIntervals[i], _spawnRateScale);
                }

                _nextSpawnTimes = (float[])_spawnEnemyIntervals.Clone();
                return;
            }

            for (int i = 0; i < _baseSpawnEnemyIntervals.Length; i++)
            {
                float oldInterval = GetScaledInterval(_baseSpawnEnemyIntervals[i], oldScale);
                float newInterval = GetScaledInterval(_baseSpawnEnemyIntervals[i], _spawnRateScale);

                float remainTime = Mathf.Max(0f, _nextSpawnTimes[i] - _spawnEnemyTimer);
                float remainRatio = oldInterval > Mathf.Epsilon ? Mathf.Clamp01(remainTime / oldInterval) : 0f;

                _spawnEnemyIntervals[i] = newInterval;
                _nextSpawnTimes[i] = _spawnEnemyTimer + newInterval * remainRatio;
            }
        }

        public void AddBattleDuration(float seconds)
        {
            if (seconds <= 0f) return;
            _duration += seconds;
        }

        private static float GetScaledInterval(float baseInterval, float scale)
        {
            float safeScale = Mathf.Max(MinSpawnRateScale, scale);
            return baseInterval / safeScale;
        }

        #endregion

        private void SpawnEnemy(int entityId)
        {
            if (_player == null) return;

            if (_currentEnemyCount >= _spawnEnemyMaxCount) return;
            int entityPoolId = _currentSpawnEnemyId % _spawnEnemyMaxCount;
            var enemyData = new EnemyData(entityPoolId, entityId, _currentLevel)
            {
                Position = GetRandomPosition()
            };
            _entity.ShowEnemy(enemyData);
            _currentSpawnEnemyId++;
        }

        private Vector3 GetRandomPosition()
        {
            float x = Random.Range(-1f, 1f);
            float z = Random.Range(-1f, 1f);
            Vector3 dir = new Vector3(x, 0, z).normalized;
            return _player.position + dir * _spawnDistanceFromPlayer;
        }

        public void ClearEnemies()
        {
            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.Available) continue;
                _entity.HideEntity(enemy);
            }

            _enemies.Clear();
        }

        #region Event Handler

        private void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            if (!(e is ShowEntitySuccessEventArgs ne)) return;

            if (ne.Entity.Logic is EnemyBase enemy)
            {
                _currentEnemyCount++;
                enemy.SetTarget(_player);
                _enemies.Add(enemy);
            }

            if (ne.EntityLogicType == typeof(Player))
            {
                _player = ne.Entity.transform;
            }
        }

        private void OnHideEntityComplete(object sender, GameEventArgs e)
        {
            if (e is HideEntityCompleteEventArgs ne)
            {
                if (ne.EntityGroup.Name == "Enemy")
                {
                    _currentEnemyCount--;
                }
            }
        }

        #endregion
    }
}
