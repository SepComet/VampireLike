using System;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using UnityEngine;

namespace Components
{
    public class AbsorbComponent : MonoBehaviour
    {
        [SerializeField] private float _absorbRangeBase = 3f;
        [SerializeField] private float _absorbSpeed = 15f;
        [SerializeField] private float _collectDistance = 0.35f;
        [SerializeField] private int _maxDetectCount = 64;
        [SerializeField] private LayerMask _detectLayerMask = ~0;

        private Player _player;
        private Transform _target;
        private StatComponent _statComponent;
        private Collider[] _buffer;

        private StatProperty _absorbRangeStat;
        private Action<StatModifier, bool> _absorbRangeStatCallback;

        private float AbsorbRange => Mathf.Max(0f, (_absorbRangeBase + _absorbRangeStat.Value) * _absorbRangeStat.Percent);

        public void OnInit(Player player, StatComponent statComponent = null)
        {
            _player = player;
            _target = player != null ? player.CachedTransform : null;
            _statComponent = statComponent;
            _buffer = new Collider[Mathf.Max(1, _maxDetectCount)];

            if (_statComponent != null)
            {
                _absorbRangeStat = _statComponent.GetStat(StatType.AbsorbRange);
                _absorbRangeStatCallback = (modifier, isApply) =>
                    _statComponent.UpdateStat(_absorbRangeStat, modifier, isApply);
                _statComponent.Subscribe(StatType.AbsorbRange, _absorbRangeStatCallback);
            }
            else
            {
                _absorbRangeStat = new StatProperty();
            }
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_player == null || !_player.Available || _target == null || _buffer == null) return;

            int hitCount = Physics.OverlapSphereNonAlloc(
                _target.position,
                AbsorbRange,
                _buffer,
                _detectLayerMask,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _buffer[i];
                if (hit == null) continue;

                CoinEntity coin = hit.GetComponent<CoinEntity>();
                if (coin != null && coin.Available)
                {
                    AbsorbDrop(coin.CachedTransform, elapseSeconds);
                    coin.TryCollect(_player, _collectDistance);
                    continue;
                }

                ExpEntity exp = hit.GetComponent<ExpEntity>();
                if (exp != null && exp.Available)
                {
                    AbsorbDrop(exp.CachedTransform, elapseSeconds);
                    exp.TryCollect(_player, _collectDistance);
                }
            }
        }

        public void OnReset()
        {
            if (_statComponent != null)
            {
                _statComponent.Unsubscribe(StatType.AbsorbRange, _absorbRangeStatCallback);
                _absorbRangeStatCallback = null;
            }

            _statComponent = null;
            _target = null;
            _player = null;
            _buffer = null;
        }

        private void AbsorbDrop(Transform drop, float deltaTime)
        {
            if (drop == null || _target == null) return;
            drop.position = Vector3.MoveTowards(drop.position, _target.position, _absorbSpeed * deltaTime);
        }
    }
}
