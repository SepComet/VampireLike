using CustomComponent;
using Entity;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

namespace Procedure
{
    public class GameStateBattle : GameStateBase
    {
        public override GameStateType GameStateType => GameStateType.Battle;

        private EnemyManagerComponent _enemyManager = null;
        private int _currentLevel = 0;
        private bool _levelOver;

        private ProcedureGame _procedureGame = null;

        public void LevelOver()
        {
            if (_levelOver) return;
            _levelOver = true;
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
            _levelOver = false;
            _enemyManager.OnInit(_currentLevel, this);
            Player player = _procedureGame.Player;
            if (player != null) player.Enable = false;
        }

        public override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            _enemyManager.OnUpdate(elapseSeconds, realElapseSeconds);

            if (_levelOver)
            {
                procedureOwner.SetData<VarByte>("CurrentLevel", (byte)(_currentLevel + 1));
                _procedureGame.BattleToShop();
            }
        }

        public override void OnLeave(IFsm<IProcedureManager> procedureOwner)
        {
            // 隐藏所有敌人实体
            _enemyManager.OnReset();

            // 停止玩家逻辑
            Player player = _procedureGame.Player;
            player.Enable = false;

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