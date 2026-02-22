using System;
using System.Collections.Generic;
using DataTable;
using Definition.Enum;
using UnityEngine;

namespace Entity.EntityData
{
    [Serializable]
    public class EnemyData : TargetableObjectData
    {
        [SerializeField] private DREnemy _drEnemy;

        public EnemyData(int entityId, EnemyType enemyType, int level) : base(
            entityId, (int)enemyType, CampType.Enemy)
        {
            DREnemy enemyRow = GameEntry.DataTable.GetDataTableRow<DREnemy>((int)enemyType);

            if (enemyRow == null)
            {
                throw new Exception($"Enemy data table row is missing, EnemyType='{enemyType}'.");
            }
            else
            {
                _drEnemy = enemyRow;
            }

            int effectiveLevel = Mathf.Max(1, level);
            MaxHealthBase = enemyRow.MaxHealth + enemyRow.HpAddPerLevel * (effectiveLevel - 1);
        }

        public EnemyType EnemyType => (EnemyType)_drEnemy.Id;

        public int EntityTypeId => _drEnemy.EntityTypeId;

        public override int MaxHealthBase { get; }

        public int AttackDamage => _drEnemy.AttackDamage;

        public float AttackCooldown => _drEnemy.AttackCooldown;

        public float AttackRange => _drEnemy.AttackRange;

        public float SpeedBase => _drEnemy.Speed;

        public int DropCoin => _drEnemy.DropCoin;

        public int DropExp => _drEnemy.DropExp;

        public float DropPercent => _drEnemy.DropPercent;

        public IReadOnlyDictionary<string, string> Params => _drEnemy.Params;

        public bool TryGetParam(string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key) || _drEnemy?.Params == null)
            {
                return false;
            }

            return _drEnemy.Params.TryGetValue(key.ToLower(), out value);
        }
    }
}
