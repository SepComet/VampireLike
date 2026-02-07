using System;
using DataTable;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public class PlayerData : TargetableObjectData
    {
        private int _maxExp;

        public PlayerData(int entityId, int typeId) : base(entityId, typeId, CampType.Player)
        {
            MaxHealthBase = MaxHealthBase;
        }

        public override int MaxHealthBase { get; }
    }
}