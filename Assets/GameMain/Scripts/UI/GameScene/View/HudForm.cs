using CustomComponent;
using CustomEvent;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HudForm : UGuiForm
    {
        [SerializeField] private float _sliderFadeDuration = 0.5f;

        [SerializeField] private Slider _hpSlider;

        [SerializeField] private TMP_Text _hpText;

        private float _currentHpPercent = 0;
        private Coroutine _hpSliderFadeCoroutine;

        [SerializeField] private Slider _expSlider;

        private float _currentExpPercent = 0;
        private Coroutine _expSliderFadeCoroutine;

        [SerializeField] private TMP_Text _expText;

        private int _playerCurrentLevel = 1;

        [SerializeField] private TMP_Text _coinText;
        private int _currentCoin = 0;

        [SerializeField] private TMP_Text _levelTimeLeftText;
        private int _currentTimeLeft = 0;

        [SerializeField] private TMP_Text _enemyCountText;
        private EnemyManagerComponent _enemy;
        
        public void RefreshUI(HudFormContext hudFormContext)
        {
        }

        #region FSM

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            GameEntry.Event.Subscribe(PlayerHealthChangeEventArgs.EventId, PlayerHpChange);
            GameEntry.Event.Subscribe(PlayerExpChangeEventArgs.EventId, PlayerExpChange);
            GameEntry.Event.Subscribe(PlayerCoinChangeEventArgs.EventId, PlayerCoinChange);
            GameEntry.Event.Subscribe(PlayerLevelUpEventArgs.EventId, PlayerLevelUp);
            GameEntry.Event.Subscribe(LevelProcessEventArgs.EventId, LevelProcess);

            _enemy = GameEntry.EnemyManager;
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            GameEntry.Event.Unsubscribe(PlayerLevelUpEventArgs.EventId, PlayerLevelUp);
            GameEntry.Event.Unsubscribe(PlayerHealthChangeEventArgs.EventId, PlayerHpChange);
            GameEntry.Event.Unsubscribe(PlayerExpChangeEventArgs.EventId, PlayerExpChange);
            GameEntry.Event.Unsubscribe(PlayerCoinChangeEventArgs.EventId, PlayerCoinChange);
            GameEntry.Event.Unsubscribe(PlayerLevelUpEventArgs.EventId, PlayerLevelUp);

            base.OnClose(isShutdown, userData);
        }
        
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            if (_enemy == null)
            {
                _enemy = GameEntry.EnemyManager;
            }

            if (_enemy != null)
            {
                _enemyCountText.text = $"Enemy: {_enemy.CurrentEnemyCount}";
            }
        }

        #endregion

        #region Event Handlers

        private void PlayerHpChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerHealthChangeEventArgs args)) return;

            _hpText.text = $"{args.CurrentHealth}/{args.MaxHealth}";

            float percent = (float)args.CurrentHealth / args.MaxHealth;
            if (Mathf.Approximately(_currentHpPercent, percent)) return;

            _currentHpPercent = percent;

            if (_hpSliderFadeCoroutine != null) StopCoroutine(_hpSliderFadeCoroutine);
            _hpSliderFadeCoroutine = StartCoroutine(_hpSlider.SmoothValue(percent, _sliderFadeDuration));
        }

        private void PlayerExpChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerExpChangeEventArgs args)) return;
            float percent = (float)args.CurrentExp / args.MaxExp;
            if (Mathf.Approximately(_currentExpPercent, percent)) return;

            _currentExpPercent = percent;

            if (_expSliderFadeCoroutine != null) StopCoroutine(_expSliderFadeCoroutine);
            _expSliderFadeCoroutine = StartCoroutine(_expSlider.SmoothValue(percent, _sliderFadeDuration));
        }

        private void PlayerLevelUp(object sender, GameEventArgs e)
        {
            if (!(e is PlayerLevelUpEventArgs)) return;

            _playerCurrentLevel++;
            _expText.text = $"LV.{_playerCurrentLevel}";
        }

        private void PlayerCoinChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerCoinChangeEventArgs args)) return;
            if (_currentCoin == args.CoinCount) return;

            _coinText.text = $"<sprite name=\"coin\" index=0> {args.CoinCount}";
            _currentCoin = args.CoinCount;
        }

        private void LevelProcess(object sender, GameEventArgs e)
        {
            if (!(e is LevelProcessEventArgs args)) return;
            if (_currentTimeLeft == args.LevelTimeLeft) return;

            _currentTimeLeft = args.LevelTimeLeft;
            _levelTimeLeftText.text = _currentTimeLeft.ToString();
        }

        #endregion
    }
}