using Definition.Enum;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UI;
using UnityGameFramework.Runtime;

namespace Procedure
{
    public class GameStateLevelUp : GameStateBase
    {
        public override GameStateType GameStateType => GameStateType.LevelUp;

        private ProcedureGame _procedureGame;
        
        public bool IsCompleted { get; set; }

        #region FSM

        public override void OnInit(ProcedureGame master)
        {
            Log.Debug("GameStateLevelUp::OnInit");

            _procedureGame = master;
            
            var useCase = new LevelUpFormUseCase(_procedureGame.Player, this);
            GameEntry.UIRouter.BindUIUseCase(UIFormType.LevelUpForm, useCase);
        }

        public override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateLevelUp::OnEnter");

            IsCompleted = false;
            
            GameEntry.UIRouter.OpenUI(UIFormType.LevelUpForm);
        }

        public override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            if (IsCompleted)
            {
                _procedureGame.LevelUpToShop();
            }
        }

        public override void OnLeave(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateLevelUp::OnLeave");

            GameEntry.UIRouter.CloseUI(UIFormType.LevelUpForm);
        }

        public override void OnDestroy(IFsm<IProcedureManager> procedureOwner)
        {
            _procedureGame = null;

            Log.Debug("GameStateLevelUp::OnDestroy");
        }

        #endregion

    }
}
