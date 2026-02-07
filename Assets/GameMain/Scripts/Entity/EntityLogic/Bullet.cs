//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Definition.DataStruct;
using Entity.EntityData;
using StarForce;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    /// <summary>
    /// 子弹类。
    /// </summary>
    public class Bullet : EntityBase
    {
        [SerializeField] private BulletData m_BulletData = null;

        public ImpactData GetImpactData()
        {
            //TODO:子弹的 AttackStat 不该为 null
            return new ImpactData(m_BulletData.OwnerCamp, m_BulletData.Attack, null);
        }
        
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }
        
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_BulletData = userData as BulletData;
            if (m_BulletData == null)
            {
                Log.Error("Bullet data is invalid.");
                return;
            }
        }
        
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            CachedTransform.Translate(Vector3.forward * m_BulletData.Speed * elapseSeconds, Space.World);
        }
    }
}