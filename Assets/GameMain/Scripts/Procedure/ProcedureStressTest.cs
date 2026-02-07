using CustomComponent;
using Entity.EntityData;
using GameFramework.Fsm;
using GameFramework.Procedure;
using StarForce;
using UnityGameFramework.Runtime;

namespace Procedure
{
    public class ProcedureStressTest : ProcedureBase
    {
        private EnemyManagerComponent _enemyManager;
        
        public override bool UseNativeDialog => false;

        protected override void OnInit(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnInit(procedureOwner);
        }
        
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            int level = procedureOwner.GetData<VarInt16>("Level").Value;
            
            // _enemyManager = GameEntry.EnemyManager;
            // _enemyManager.OnInit(level, this);
            //
            // var playerData = new PlayerData(-1, 101, 100, 4);
            // GameEntry.Entity.ShowPlayer(playerData);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            
            _enemyManager.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            _enemyManager = null;
            
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnDestroy(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }
    }
}
