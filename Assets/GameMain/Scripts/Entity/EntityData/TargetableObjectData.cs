//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using Definition.Enum;
using StarForce;
using UnityEngine;

namespace Entity.EntityData
{
    [Serializable]
    public abstract class TargetableObjectData : EntityDataBase
    {
        [SerializeField]
        private CampType _camp = CampType.Unknown;
        
        public TargetableObjectData(int entityId, int typeId, CampType camp)
            : base(entityId, typeId)
        {
            _camp = camp;
        }

        /// <summary>
        /// 角色阵营。
        /// </summary>
        public CampType Camp => _camp;
        
        /// <summary>
        /// 最大生命。
        /// </summary>
        public abstract int MaxHealthBase
        {
            get;
        }
    }
}
