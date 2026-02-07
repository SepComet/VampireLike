//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Entity.EntityData;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public abstract class EntityBase : EntityLogic
    {
        [SerializeField] private EntityDataBase _entityData = null;

        public int Id => Entity.Id;

        public Animation CachedAnimation { get; private set; }
        
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            CachedAnimation = GetComponent<Animation>();
        }
        
        protected override void OnRecycle()
        {
            base.OnRecycle();
        }
        
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            _entityData = userData as EntityDataBase;
            if (_entityData == null)
            {
                Log.Error("Entity data is invalid.");
                return;
            }

            Name = Utility.Text.Format("[Entity {0}]", Id);
            CachedTransform.localPosition = _entityData.Position;
            CachedTransform.localRotation = _entityData.Rotation;
            CachedTransform.localScale = Vector3.one;
        }

        
        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
        }
        
        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            base.OnAttached(childEntity, parentTransform, userData);
        }
        
        protected override void OnDetached(EntityLogic childEntity, object userData)
        {
            base.OnDetached(childEntity, userData);
        }
        
        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);
        }
        
        protected override void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            base.OnDetachFrom(parentEntity, userData);
        }
        
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
        }
    }
}