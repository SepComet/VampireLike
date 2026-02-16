using Definition.Enum;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UI;
using UnityGameFramework.Runtime;

namespace Procedure
{
    public class GameStateShop : GameStateBase
    {
        public override GameStateType GameStateType => GameStateType.Shop;

        private ProcedureGame _procedureGame;

        public bool ShopFinish { get; set; }

        #region FSM

        public override void OnInit(ProcedureGame master)
        {
            Log.Debug("GameStateShop::OnInit");
            _procedureGame = master;
            
            var shopFormUseCase = new ShopFormUseCase(_procedureGame, this);
            GameEntry.UIRouter.BindUIUseCase(UIFormType.ShopForm, shopFormUseCase);
        }

        public override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateShop::OnEnter");

            ShopFinish = false;

            GameEntry.UIRouter.OpenUI(UIFormType.ShopForm);
        }

        public override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            if (ShopFinish)
            {
                _procedureGame.ShopToBattle();
            }
        }

        public override void OnLeave(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateShop::OnLeave");

            GameEntry.UIRouter.CloseUI(UIFormType.ShopForm);
        }

        public override void OnDestroy(IFsm<IProcedureManager> procedureOwner)
        {
            _procedureGame = null;

            Log.Debug("GameStateShop::OnDestroy");
        }

        #endregion
    }
}