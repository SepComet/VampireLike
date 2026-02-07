//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;
using Definition.Enum;

namespace Definition.DataStruct
{
    [StructLayout(LayoutKind.Auto)]
    public struct ImpactData
    {
        public ImpactData(CampType camp /*, int health*/, int attackBase, StatProperty attackStat = null,
            StatProperty defenseStat = null, StatProperty dodgeStat = null)
        {
            Camp = camp;
            //Health = health;
            AttackBase = attackBase;
            AttackStat = attackStat;
            DefenseStat = defenseStat;
            DodgeStat = dodgeStat;
        }

        public CampType Camp { get; }

        public int AttackBase { get; }

        public StatProperty AttackStat { get; }

        public StatProperty DefenseStat { get; }

        public StatProperty DodgeStat { get; }
    }
}