using Components;
using DataTable;

namespace Definition.DataStruct
{
    public class PropItem
    {
        private DRProp _prop;
        
        public PropItem(DRProp prop)
        {
            _prop = prop;
        }

        public void OnAttach(StatComponent statComponent)
        {
            foreach (var modifier in _prop.Modifiers)
            {
                statComponent.AddModifier(modifier);
            }
        }

        public void OnDetach(StatComponent statComponent)
        {
            foreach (var modifier in _prop.Modifiers)
            {
                statComponent.RemoveModifier(modifier);
            }
        }
    }
}