//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Definition.Enum;
using GameFramework.Event;
using Scene;
using UI;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Procedure
{
    public class ProcedureStartMenu : ProcedureBase
    {
        private bool _isStartGame = false;
        private StartMenuForm _startMenuForm = null;
        private SelectRoleForm _selectRoleForm = null;

        public override bool UseNativeDialog => false;

        private int _selectedRoleId = 0;

        public void StartGame(int roleId)
        {
            _isStartGame = true;
            _selectedRoleId = roleId;
        }

        private void OnStartGame(ProcedureOwner procedureOwner)
        {
            procedureOwner.SetData<VarInt32>("NextSceneId", (int)SceneId.Game);
            procedureOwner.SetData<VarInt32>("SelectedRoleId", _selectedRoleId);
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        public void OpenSelectForm()
        {
            _startMenuForm.Close();
            GameEntry.UI.OpenUIForm(UIFormType.SelectRoleForm, this);
        }

        #region FSM

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            // 1. 初始化变量与事件
            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            _isStartGame = false;

            // 2. 打开 StartMenuForm
            GameEntry.UI.OpenUIForm(UIFormType.StartMenuForm, this);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);

            if (_startMenuForm != null)
            {
                _startMenuForm.Close(true);
                _startMenuForm = null;
            }

            if (_selectRoleForm != null)
            {
                _selectRoleForm.Close(true);
                _selectRoleForm = null;
            }
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (_isStartGame)
            {
                OnStartGame(procedureOwner);
            }
        }

        #endregion

        #region Event Handlers

        private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (e is OpenUIFormSuccessEventArgs ne)
            {
                if (ne.UIForm.Logic is StartMenuForm startMenuForm) _startMenuForm = startMenuForm;
                if (ne.UIForm.Logic is SelectRoleForm selectRoleForm) _selectRoleForm = selectRoleForm;
            }
        }

        #endregion
    }
}