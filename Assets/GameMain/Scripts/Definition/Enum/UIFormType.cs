//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace Definition.Enum
{
    /// <summary>
    /// 界面编号。
    /// </summary>
    public enum UIFormType : byte
    {
        Undefined = 0,

        /// <summary>
        /// 弹出框。
        /// </summary>
        DialogForm = 1,

        /// <summary>
        /// 主菜单。
        /// </summary>
        MenuForm = 100,

        /// <summary>
        /// 设置。
        /// </summary>
        SettingForm = 101,

        /// <summary>
        /// 关于。
        /// </summary>
        AboutForm = 102,
        
        /// <summary>
        /// 主菜单。
        /// </summary>
        StartMenuForm = 200,
        
        /// <summary>
        /// 选择角色。
        /// </summary>
        SelectRoleForm = 201,
        
        /// <summary>
        /// 游戏商店。
        /// </summary>
        ShopForm = 202,
        
        /// <summary>
        /// 游戏HUD。
        /// </summary>
        HudForm = 203,

        /// <summary>
        /// 升级选择。
        /// </summary>
        LevelUpForm = 204,
    }
}
