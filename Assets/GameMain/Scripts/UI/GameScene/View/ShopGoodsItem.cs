using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ShopGoodsItem : MonoBehaviour
    {
        [SerializeField] private IconArea _iconArea;
        
        [SerializeField] private TMP_Text _titleText;
        
        [SerializeField] private TMP_Text _typeText;
        
        [SerializeField] private TMP_Text _descriptionText;
        
        [SerializeField] private TMP_Text _costText;

        #region Init

        public void Init(GoodsItemContext data)
        {
            _iconArea.OnInit(data.Icon, data.Rarity);
            _titleText.text = data.Title;
            _typeText.text = data.Type;
            _descriptionText.text = data.Description;
            _costText.text = $"{data.Price} <sprite name=\"coin\" index=0>";
        }

        #endregion
    }
}
