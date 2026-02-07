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
        private EntityComponent _entity;

        private List<EntityBase> _enemies;

        public List<EntityBase> Enemies => _enemies;

        private float _spawnEnemyTimer;
        private int _spawnEnemyMaxCount = 5000;
        private int _currentEnemyCount;
        private int _spawnDistanceFromPlayer = 20;
        private int _currentSpawnEnemyId;
        private int _currentLevel;

        private float[] _spawnEnemyIntervals;
        private int[] _spawnEnemyIds;
        private int[] _spawnEnemyCounts;
        private float _duration;

        private float[] _nextSpawnTimes;

        private Transform _player;

        private GameStateBattle _battle;

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
            _spawnEnemyIntervals = levelData.Intervals;
            _spawnEnemyIds = levelData.EntityIds;
            _spawnEnemyCounts = levelData.EntityCounts;
            _duration = levelData.Duration;

            _nextSpawnTimes = (float[])_spawnEnemyIntervals.Clone();

            _currentSpawnEnemyId = 0;
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            _spawnEnemyTimer += elapseSeconds;
            // if (_spawnEnemyTimer < _spawnEnemyInterval) return;
            // SpawnEnemy();
            // _spawnEnemyTimer = 0;
            // _spawnEnemyInterval = Mathf.Max(0.1f, _spawnEnemyInterval - _spawnEnemyAccelerate);
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

            _spawnEnemyIntervals = null;
            _spawnEnemyIds = null;
            _spawnEnemyCounts = null;
            _duration = 0;

            _nextSpawnTimes = null;

            _battle = null;

            ClearEnemies();
        }

        #endregion

        private void SpawnEnemy(int entityId)
        {
            if (_player == null) return;

            if (_currentEnemyCount >= _spawnEnemyMaxCount) return;
            int entityPoolId = _currentSpawnEnemyId % _spawnEnemyMaxCount;
            var enemyData = new EnemyData(entityPoolId, entityId)
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