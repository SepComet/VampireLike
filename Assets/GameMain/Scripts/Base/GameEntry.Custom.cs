//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using CustomComponent;
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
    
    public static EnemyManagerComponent EnemyManager { get; private set; }
    
    public static SpriteCacheComponent SpriteCache { get; private set; }

    public static UIRouterComponent UIRouter { get; private set; }

    private static void InitCustomComponents()
    {
        BuiltinData = UnityGameFramework.Runtime.GameEntry.GetComponent<BuiltinDataComponent>();
        HPBar = UnityGameFramework.Runtime.GameEntry.GetComponent<HPBarComponent>();
        EnemyManager = UnityGameFramework.Runtime.GameEntry.GetComponent<EnemyManagerComponent>();
        SpriteCache = UnityGameFramework.Runtime.GameEntry.GetComponent<SpriteCacheComponent>();
        UIRouter = UnityGameFramework.Runtime.GameEntry.GetComponent<UIRouterComponent>();
    }
}
