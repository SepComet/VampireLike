using System;
using Definition.Enum;
using UnityEngine;

namespace Entity.EntityData
{
    [Serializable]
    public class EnemyProjectileData : EntityDataBase
    {
        public EnemyProjectileData(int entityId, int ownerEntityId, CampType ownerCamp, int attackDamage,
            float speed, float lifeTime, Vector3 direction)
            : base(entityId, 0)
        {
            OwnerEntityId = ownerEntityId;
            OwnerCamp = ownerCamp;
            AttackDamage = attackDamage;
            Speed = speed;
            LifeTime = lifeTime;
            Direction = direction;
        }

        public int OwnerEntityId { get; }
        public CampType OwnerCamp { get; }
        public int AttackDamage { get; }
        public float Speed { get; }
        public float LifeTime { get; }
        public Vector3 Direction { get; }
    }
}
