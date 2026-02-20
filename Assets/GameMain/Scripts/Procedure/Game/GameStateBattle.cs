using System;
using CustomComponent;
using CustomEvent;
using DataTable;
using Entity;
using GameFramework.Fsm;
using GameFramework.Procedure;
using Simulation;
using UnityEngine;

namespace Procedure
{
    public class GameStateBattle : GameStateBase
    {
        public override GameStateType GameStateType => GameStateType.Battle;

        private EnemyManagerComponent _enemyManager;

        private int _currentLevel;

        private float _levelTimeLeft;

        private Player Player => _procedureGame.Player;

        private ProcedureGame _procedureGame;

        public void AddBattleDuration(float seconds)
        {
            if (seconds <= 0f) return;
            _levelTimeLeft += seconds;
        }

        #region FSM

        public override void OnInit(ProcedureGame master)
        {
            _enemyManager = GameEntry.EnemyManager;
            _procedureGame = master;
        }

        public override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            _currentLevel = _procedureGame.CurrentLevel;

            var drLevel = GameEntry.DataTable.GetDataTableRow<DRLevel>(_currentLevel);

            if (drLevel == null)
            {
                throw new Exception($"GameStateBattle.OnEnter: {_currentLevel} is not found.");
            }

            _levelTimeLeft = drLevel.Duration;
            _enemyManager.OnInit(drLevel);

            if (Player != null) Player.Enable = true;
        }

        public override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            if (_levelTimeLeft < 0)
            {
                _procedureGame.BattleToShopOrLevelUp();
                return;
            }

            _enemyManager.OnUpdate(elapseSeconds, realElapseSeconds);

            SimulationWorld simulationWorld = GameEntry.SimulationWorld;
            if (simulationWorld != null)
            {
                Vector3 playerPosition = Player != null ? Player.CachedTransform.position : Vector3.zero;
                simulationWorld.Tick(new SimulationTickContext(elapseSeconds, realElapseSeconds, playerPosition));
            }

            _levelTimeLeft -= elapseSeconds;
            GameEntry.Event.Fire(this, LevelProcessEventArgs.Create((int)_levelTimeLeft));
        }

        public override void OnLeave(IFsm<IProcedureManager> procedureOwner)
        {
            // 隐藏所有敌人实体
            _enemyManager.OnReset();

            // 停止玩家逻辑
            Player.Enable = false;

            // 隐藏所有掉落物实体
            var entities = GameEntry.Entity.GetEntityGroup("Drop").GetAllEntities();
            foreach (var entity in entities)
            {
                GameEntry.Entity.HideEntity(entity.Id);
            }
        }

        public override void OnDestroy(IFsm<IProcedureManager> procedureOwner)
        {
            _enemyManager = null;
            _procedureGame = null;
        }

        #endregion
    }
}