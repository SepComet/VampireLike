using Definition.Enum;
using UnityEngine;

namespace UI
{
    public class DisplayItemInfoFormContext : UIContext
    {
        public string IconAssetName;
        public string Title;
        public string TypeText;
        public ItemRarity Rarity;
        public string Description;
        public int Price;
        public bool IsWeapon;
        public Vector3 TargetPos;
    }
}