using Definition.Enum;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class IconArea : MonoBehaviour
    {
        [SerializeField] private Image _board;

        [SerializeField] private Image _icon;

        public void OnInit(Sprite sprite, ItemRarity rarity)
        {
            SetIcon(sprite);
            SetRarity(rarity);
        }

        public void SetIcon(Sprite sprite)
        {
            _icon.sprite = sprite;
        }

        public void SetRarity(ItemRarity rarity)
        {
            _board.color = rarity switch
            {
                ItemRarity.White => Color.white,
                ItemRarity.Green => Color.green,
                ItemRarity.Blue => Color.blue,
                ItemRarity.Purple => Color.magenta,
                ItemRarity.Red => Color.red,
                _ => Color.clear,
            };
        }

        public void OnReset()
        {
            SetIcon(null);
            SetRarity(ItemRarity.None);
        }
    }
}