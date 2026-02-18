using System;
using DataTable;
using Definition.Enum;
using UnityEngine;

namespace Entity.EntityData
{
    [Serializable]
    public class EnemyData : TargetableObjectData
    {
        [SerializeField] private float _speedBase = 0;
        
        [SerializeField] private int _dropCoin = 0;
        
        [SerializeField] private int _dropExp = 0;
        
        [SerializeField] private float _dropPercent = 0;

        public EnemyData(int entityId, int typeId, int level) : base(
            entityId, typeId, CampType.Enemy)
        {
            DREnemy enemyRow = GameEntry.DataTable.GetDataTableRow<DREnemy>(typeId);
            int effectiveLevel = Mathf.Max(1, level);
            MaxHealthBase = enemyRow.MaxHealth + enemyRow.HpAddPerLevel * (effectiveLevel - 1);
            SpeedBase = enemyRow.Speed;
            DropCoin = enemyRow.DropCoin;
            DropExp = enemyRow.DropExp;
            DropPercent = enemyRow.DropPercent;
        }

        public override int MaxHealthBase { get; }

        public float SpeedBase
        {
            get => _speedBase;
            set => _speedBase = value;
        }

        public int DropCoin
        {
            get => _dropCoin;
            set => _dropCoin = value;
        }

        public int DropExp
        {
            get => _dropExp;
            set => _dropExp = value;
        }

        public float DropPercent
        {
            get => _dropPercent;
            set => _dropPercent = value;
        }
    }
}
