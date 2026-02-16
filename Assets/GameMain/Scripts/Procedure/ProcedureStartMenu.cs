using Definition.Enum;
using Scene;
using UI;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Procedure
{
    public class ProcedureStartMenu : ProcedureBase
    {
        public override bool UseNativeDialog => false;

        private bool _startGame = false;

        private int _selectedRoleId = 0;

        public void StartGame(int selectedRoleId)
        {
            _selectedRoleId = selectedRoleId;
            _startGame = true;
        }

        #region FSM

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            GameEntry.UIRouter.OpenUI(UIFormType.StartMenuForm);

            var useCase2 = new SelectRoleFormUseCase(this);
            GameEntry.UIRouter.BindUIUseCase(UIFormType.SelectRoleForm, useCase2);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
            GameEntry.UIRouter.CloseUI(UIFormType.StartMenuForm);
            GameEntry.UIRouter.CloseUI(UIFormType.SelectRoleForm);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (_startGame)
            {
                procedureOwner.SetData<VarInt32>("NextSceneId", (int)SceneId.Game);
                procedureOwner.SetData<VarInt32>("SelectedRoleId", _selectedRoleId);
                ChangeState<ProcedureChangeScene>(procedureOwner);
            }
        }

        #endregion
    }
}