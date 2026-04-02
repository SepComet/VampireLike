namespace Entity.Weapon
{
    public partial class WeaponLance
    {
        private class AttackState : WeaponStateBase
        {
            private WeaponLance _weapon;

            public override WeaponStateType State => WeaponStateType.Attack;
            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponLance;

            public override void OnEnter()
            {
                _weapon._currAttackTimer = 0f;
                _weapon.Attack();
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (!_weapon._isAttacking)
                {
                    _weapon.TransitionTo(WeaponStateType.Check_InRange);
                }
            }

            public override void OnLeave()
            {
            }
        }
    }
}
