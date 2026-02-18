using System.Collections.Generic;
using CustomEvent;
using DataTable;
using Definition.Enum;
using Entity;
using Entity.EntityData;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using UI;

namespace Procedure
{
    public enum GameStateType
    {
        None = 0,
        Battle = 1,
        Shop = 2,
        LevelUp = 3,
    }

    public class ProcedureGame : ProcedureBase
    {
        public override bool UseNativeDialog => false;

        private HudForm _hudForm = null;
        private bool _hudInitialized = false;

        private IFsm<IProcedureManager> _procedureOwner = null;
        private PlayerData _currentPlayerData = null;
        public int CurrentLevel = 1;

        private GameStateType _currentGameState = GameStateType.None;
        private Dictionary<GameStateType, GameStateBase> _gameStates;
        public Player Player;

        private void InitGameState()
        {
            _gameStates = new Dictionary<GameStateType, GameStateBase>
            {
                { GameStateType.Battle, new GameStateBattle() },
                { GameStateType.LevelUp, new GameStateLevelUp() },
                { GameStateType.Shop, new GameStateShop() },
            };
            _gameStates[GameStateType.Battle].OnInit(this);
            _gameStates[GameStateType.LevelUp].OnInit(this);
            _gameStates[GameStateType.Shop].OnInit(this);

            _currentGameState = GameStateType.Battle;
            _gameStates[_currentGameState].OnEnter(_procedureOwner);
        }

        public void BattleToShopOrLevelUp()
        {
            CurrentLevel++;
            if (_currentGameState == GameStateType.Shop || _currentGameState == GameStateType.LevelUp) return;

            _gameStates[_currentGameState].OnLeave(_procedureOwner);
            _currentGameState = Player.PendingLevelPoints > 0 ? GameStateType.LevelUp : GameStateType.Shop;
            _gameStates[_currentGameState].OnEnter(_procedureOwner);
        }

        public void ShopToBattle()
        {
            if (_currentGameState == GameStateType.Battle) return;

            _gameStates[_currentGameState].OnLeave(_procedureOwner);
            _currentGameState = GameStateType.Battle;
            _gameStates[_currentGameState].OnEnter(_procedureOwner);
        }

        public void LevelUpToShop()
        {
            if (_currentGameState == GameStateType.Shop) return;

            _gameStates[_currentGameState].OnLeave(_procedureOwner);
            _currentGameState = GameStateType.Shop;
            _gameStates[_currentGameState].OnEnter(_procedureOwner);
        }

        #region FSM

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            _procedureOwner = procedureOwner;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, ShowEntitySuccess);

            CurrentLevel = 1;
            _currentPlayerData = new PlayerData(-1, 1001);
            GameEntry.Entity.ShowPlayer(_currentPlayerData);
            
            GameEntry.UIRouter.OpenUI(UIFormType.HudForm);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            _procedureOwner = procedureOwner;

            if (!_hudInitialized && _hudForm != null && Player != null)
            {
                int selectedRoleId = _procedureOwner.GetData<VarInt32>("SelectedRoleId").Value;
                var role = GameEntry.DataTable.GetDataTableRow<DRRole>(selectedRoleId);
                Player.InitRole(role);
                _hudInitialized = true;
                InitGameState();
            }

            if (_hudInitialized)
                _gameStates[_currentGameState].OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            foreach (var state in _gameStates.Values)
            {
                state.OnDestroy(procedureOwner);
            }

            _gameStates.Clear();

            GameEntry.UIRouter.CloseUI(UIFormType.HudForm);
            
            _hudForm = null;
            
            Player = null;
            
            _procedureOwner = null;

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, ShowEntitySuccess);

            base.OnLeave(procedureOwner, isShutdown);
        }

        #endregion

        #region Event Handler

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args)) return;

            if (args.UIForm.Logic is HudForm hudForm)
            {
                _hudForm = hudForm;
            }
        }

        private void ShowEntitySuccess(object sender, GameEventArgs e)
        {
            if (!(e is ShowEntitySuccessEventArgs args)) return;

            if (args.Entity.Logic is Player player)
            {
                Player = player;
            }
        }

        #endregion
    }
}