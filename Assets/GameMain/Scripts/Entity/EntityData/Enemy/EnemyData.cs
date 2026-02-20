using System;
using DataTable;
using Definition.Enum;
using UnityEngine;

namespace Entity.EntityData
{
    [Serializable]
    public class EnemyData : TargetableObjectData
    {
        [SerializeField] private EnemyType _enemyType;
        
        [SerializeField] private int _entityTypeId;

        [SerializeField] private float _speedBase = 0;

        [SerializeField] private int _dropCoin = 0;

        [SerializeField] private int _dropExp = 0;

        [SerializeField] private float _dropPercent = 0;

        public EnemyData(int entityId, EnemyType enemyType, int level) : base(
            entityId, (int)enemyType, CampType.Enemy)
        {
            DREnemy enemyRow = GameEntry.DataTable.GetDataTableRow<DREnemy>((int)enemyType);

            if (enemyRow == null)
            {
                throw new Exception($"Enemy data table row is missing, EnemyType='{enemyType}'.");
            }

            int effectiveLevel = Mathf.Max(1, level);

            _enemyType = enemyType;
            _entityTypeId = enemyRow.EntityTypeId;
            MaxHealthBase = enemyRow.MaxHealth + enemyRow.HpAddPerLevel * (effectiveLevel - 1);
            _speedBase = enemyRow.Speed;
            _dropCoin = enemyRow.DropCoin;
            _dropExp = enemyRow.DropExp;
            _dropPercent = enemyRow.DropPercent;
        }

        public EnemyType EnemyType => _enemyType;
        
        public int EntityTypeId => _entityTypeId;

        public override int MaxHealthBase { get; }

        public float SpeedBase => _speedBase;

        public int DropCoin => _dropCoin;

        public int DropExp => _dropExp;

        public float DropPercent => _dropPercent;
    }
}