using System;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public sealed class WeaponLanceParamsData
    {
        /// <summary>
        /// 枪尖命中半径。
        /// </summary>
        public float HitRadius { get; set; }

        /// <summary>
        /// 武器模型前刺的位移距离。
        /// </summary>
        public float ThrustDistance { get; set; }

        /// <summary>
        /// 实际判定的前刺长度。
        /// </summary>
        public float PierceLength { get; set; }

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
