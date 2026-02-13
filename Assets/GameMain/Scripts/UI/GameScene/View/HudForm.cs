using Components;
using CustomEvent;
using Entity;
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
        private float _currentHpPercent = 0;
        private Coroutine _hpSliderFadeCoroutine;

        [SerializeField] private Slider _expSlider;
        private float _currentExpPercent = 0;
        private Coroutine _expSliderFadeCoroutine;

        [SerializeField] private TMP_Text _coinText;
        private int _currentCoin = 0;

        #region FSM

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            GameEntry.Event.Subscribe(PlayerHealthChangeEventArgs.EventId, OnPlayerHpChange);
            GameEntry.Event.Subscribe(PlayerExpChangeEventArgs.EventId, OnPlayerExpChange);
            GameEntry.Event.Subscribe(PlayerCoinChangeEventArgs.EventId, OnPlayerCoinChange);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            GameEntry.Event.Unsubscribe(PlayerHealthChangeEventArgs.EventId, OnPlayerHpChange);
            GameEntry.Event.Unsubscribe(PlayerExpChangeEventArgs.EventId, OnPlayerExpChange);
            GameEntry.Event.Unsubscribe(PlayerCoinChangeEventArgs.EventId, OnPlayerCoinChange);

            base.OnClose(isShutdown, userData);
        }

        #endregion

        #region Event Handlers

        private void OnPlayerHpChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerHealthChangeEventArgs args)) return;
            float percent = (float)args.CurrentHealth / args.MaxHealth;
            if (Mathf.Approximately(_currentHpPercent, percent)) return;

            _currentHpPercent = percent;

            if (_hpSliderFadeCoroutine != null) StopCoroutine(_hpSliderFadeCoroutine);
            _hpSliderFadeCoroutine = StartCoroutine(_hpSlider.SmoothValue(percent, _sliderFadeDuration));
        }

        private void OnPlayerExpChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerExpChangeEventArgs args)) return;
            float percent = (float)args.CurrentExp / args.MaxExp;
            if (Mathf.Approximately(_currentExpPercent, percent)) return;

            _currentExpPercent = percent;

            if (_expSliderFadeCoroutine != null) StopCoroutine(_expSliderFadeCoroutine);
            _expSliderFadeCoroutine = StartCoroutine(_expSlider.SmoothValue(percent, _sliderFadeDuration));
        }

        private void OnPlayerCoinChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerCoinChangeEventArgs args)) return;
            if (_currentCoin == args.CoinCount) return;

            _coinText.text = args.CoinCount.ToString();
            _currentCoin = args.CoinCount;
        }

        #endregion

        #region Test

        private Player _player;

        public void CauseDamageToPlayer()
        {
            if (_player == null)
            {
                _player = FindObjectOfType<Player>();
            }

            if (_player == null) return;

            _player.GetComponent<HealthComponent>().TakeDamage(2);
        }

        #endregion
    }
}