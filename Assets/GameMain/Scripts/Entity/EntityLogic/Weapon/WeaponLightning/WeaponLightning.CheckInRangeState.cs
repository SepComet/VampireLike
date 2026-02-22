namespace Entity.Weapon
{
    public partial class WeaponLightning
    {
        private class CheckInRangeState : WeaponStateBase
        {
            private WeaponLightning _weapon;

            public override WeaponStateType State => WeaponStateType.Check_InRange;
            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponLightning;

            public override void OnEnter()
            {
                if (_weapon._currAttackTimer >= _weapon._weaponData.Cooldown)
                {
                    _weapon.TransitionTo(WeaponStateType.Attack);
                }
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                _weapon.Check();
                _weapon.RotateToTarget(elapseSeconds);
                _weapon._currAttackTimer += elapseSeconds;

                if (_weapon._target == null || !_weapon._target.Available)
                {
                    _weapon.TransitionTo(WeaponStateType.Idle);
                    return;
                }

                if (!_weapon.IsInRange(_weapon._target, _weapon._sqrRange))
                {
                    _weapon.TransitionTo(WeaponStateType.Check_OutRange);
                    return;
                }

                if (_weapon._currAttackTimer >= _weapon._weaponData.Cooldown)
                {
                    _weapon.TransitionTo(WeaponStateType.Attack);
                }
            }

            public override void OnLeave()
            {
            }
        }
    }
}
