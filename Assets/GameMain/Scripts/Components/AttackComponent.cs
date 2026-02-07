using System.Collections;
using System.Collections.Generic;
using Entity;
using UnityEngine;

namespace Components
{
    public class AttackComponent : MonoBehaviour
    {
        private bool _isAttacking = false;
        private List<WeaponBase> _weapons;

        public void SetIsAttacking(bool isAttacking) => _isAttacking = isAttacking;

        public void OnInit()
        {
        }

        public void OnUpdate(float elapsedTime, float realElapseSeconds)
        {
            
        }
    }
}