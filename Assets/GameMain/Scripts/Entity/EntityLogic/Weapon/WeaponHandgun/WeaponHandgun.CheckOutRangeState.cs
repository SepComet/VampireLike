namespace Entity.Weapon
{
    public partial class WeaponHandgun
    {
        private class CheckOutRangeState : WeaponStateBase
        {
            private WeaponHandgun _weapon;
            public override WeaponStateType State => WeaponStateType.Check_OutRange;
            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponHandgun;
            public override void OnEnter() { }

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

                if (_weapon.IsInRange(_weapon._target, _weapon._sqrRange))
                {
                    _weapon.TransitionTo(WeaponStateType.Check_InRange);
                }
            }

            public override void OnLeave() { }
        }
    }
}