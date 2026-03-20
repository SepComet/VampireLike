using System;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public sealed class WeaponLanceParamsData
    {
        /// <summary>
        /// 横向半宽，表示前戳矩形判定的一半宽度。
        /// </summary>
        public float HitHalfWidth { get; set; }

        /// <summary>
        /// 旧字段兼容，未配置 HitHalfWidth 时回退使用。
        /// </summary>
        public float HitRadius { get; set; }

        /// <summary>
        /// 前戳判定盒体的总高度。
        /// </summary>
        public float HitHeight { get; set; }

        /// <summary>
        /// 判定盒体中心相对战斗平面的高度偏移。
        /// </summary>
        public float HitCenterYOffset { get; set; }

        /// <summary>
        /// 前刺距离，同时驱动武器位移和命中长度。
        /// </summary>
        public float PierceLength { get; set; }

        /// <summary>
        /// 旧字段兼容，未配置 PierceLength 时回退使用。
        /// </summary>
        public float ThrustDistance { get; set; }

        /// <summary>
        /// 判定起点相对武器当前位置的前置偏移。
        /// </summary>
        public float ForwardOffset { get; set; }

        /// <summary>
        /// 追踪目标时的转向速度。
        /// </summary>
        public float RotateSpeed { get; set; }

        /// <summary>
        /// 向前突刺阶段耗时。
        /// </summary>
        public float AttackDuration { get; set; }

        /// <summary>
        /// 收枪返回阶段耗时。
        /// </summary>
        public float ReturnDuration { get; set; }
    }

    [Serializable]
    public class WeaponLanceData : WeaponData
    {
        public WeaponLanceParamsData ParamsData { get; }

        public WeaponLanceData(int entityId, int ownerId, CampType ownerCamp)
            : base(entityId, WeaponType.WeaponLance, ownerId, ownerCamp)
        {
            ParamsData = ParseParams<WeaponLanceParamsData>();
        }
    }
}
