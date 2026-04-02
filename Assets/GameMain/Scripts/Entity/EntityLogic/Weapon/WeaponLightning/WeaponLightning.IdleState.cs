namespace Entity.Weapon
{
    public partial class WeaponLightning
    {
        private class IdleState : WeaponStateBase
        {
            private WeaponLightning _weapon;

            public override WeaponStateType State => WeaponStateType.Idle;
            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponLightning;

            public override void OnEnter()
            {
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                _weapon.Check();
                _weapon.RotateToOrigin(elapseSeconds);
                _weapon._currAttackTimer += elapseSeconds;

                if (_weapon._target != null && _weapon._target.Available)
                {
                    _weapon.TransitionTo(WeaponStateType.Check_OutRange);
                }
            }

            public override void OnLeave()
            {
            }
        }
    }
}
