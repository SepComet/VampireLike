namespace Entity.Weapon
{
    public partial class WeaponHandgun
    {
        private class AttackState : WeaponStateBase
        {
            private WeaponHandgun _weapon;
            public override WeaponStateType State => WeaponStateType.Attack;
            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponHandgun;

            public override void OnEnter()
            {
                _weapon._currAttackTimer = 0f;
                _weapon.Attack();
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                _weapon.TransitionTo(WeaponStateType.Check_InRange);
            }

            public override void OnLeave() { }
        }
    }
}