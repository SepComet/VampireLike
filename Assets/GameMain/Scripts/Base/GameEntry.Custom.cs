//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using CustomComponent;
using Simulation;
using StarForce;
using UI;
using UnityEngine;

/// <summary>
/// 游戏入口。
/// </summary>
public partial class GameEntry : MonoBehaviour
{
    public static BuiltinDataComponent BuiltinData { get; private set; }

    public static HPBarComponent HPBar { get; private set; }

    public static DamageTextComponent DamageText { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static RuntimeDebugPanelComponent RuntimeDebugPanel { get; private set; }
#endif

    public static EnemyManagerComponent EnemyManager { get; private set; }

    public static SimulationWorld SimulationWorld { get; private set; }

    public static SpriteCacheComponent SpriteCache { get; private set; }

    public static UIRouterComponent UIRouter { get; private set; }

    private static void InitCustomComponents()
    {
        BuiltinData = UnityGameFramework.Runtime.GameEntry.GetComponent<BuiltinDataComponent>();
        HPBar = UnityGameFramework.Runtime.GameEntry.GetComponent<HPBarComponent>();
        DamageText = UnityGameFramework.Runtime.GameEntry.GetComponent<DamageTextComponent>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RuntimeDebugPanel = UnityGameFramework.Runtime.GameEntry.GetComponent<RuntimeDebugPanelComponent>();
#endif

        EnemyManager = UnityGameFramework.Runtime.GameEntry.GetComponent<EnemyManagerComponent>();
        SimulationWorld = UnityGameFramework.Runtime.GameEntry.GetComponent<SimulationWorld>();
        SpriteCache = UnityGameFramework.Runtime.GameEntry.GetComponent<SpriteCacheComponent>();
        UIRouter = UnityGameFramework.Runtime.GameEntry.GetComponent<UIRouterComponent>();
    }
}