using Components;
using DataTable;
using Definition.Enum;

namespace Definition.DataStruct
{
    public class PropItem
    {
        private readonly StatModifier[] _modifiers;
        public string Title { get; private set; }
        public string IconAssetName { get; private set; }
        public ItemRarity Rarity { get; private set; }
        public StatModifier[] Modifiers => _modifiers;

        public PropItem(DRProp prop)
        {
            if (prop == null) return;

            _modifiers = prop.Modifiers;
            Title = prop.Title;
            Rarity = prop.Rarity;
            IconAssetName = prop.IconAssetName;
        }

        public PropItem(StatModifier[] modifiers)
        {
            _modifiers = modifiers;
        }

        public PropItem(StatModifier[] modifiers, ItemRarity rarity, string title, string iconAssetName)
        {
            _modifiers = modifiers;
            Title = title;
            Rarity = rarity;
            IconAssetName = iconAssetName;
        }

        public void OnAttach(StatComponent statComponent)
        {
            if (_modifiers == null || statComponent == null) return;
            foreach (var modifier in _modifiers)
            {
                statComponent.AddModifier(modifier);
            }
        }

        public void OnDetach(StatComponent statComponent)
        {
            if (_modifiers == null || statComponent == null) return;
            foreach (var modifier in _modifiers)
            {
                statComponent.RemoveModifier(modifier);
            }
        }
    }
}