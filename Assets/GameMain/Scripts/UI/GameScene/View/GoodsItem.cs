using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GoodsItem : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        
        [SerializeField] private TMP_Text _titleText;
        
        [SerializeField] private TMP_Text _typeText;
        
        [SerializeField] private TMP_Text _descriptionText;
        
        [SerializeField] private TMP_Text _costText;
        
        [SerializeField] private CommonButton _purchaseButton;

        #region Init

        public void Init(GoodsItemContext data)
        {
            _iconImage.sprite = data.Icon;
            _titleText.text = data.Title;
            _typeText.text = data.Type;
            _descriptionText.text = data.Description;
            _costText.text = data.Price.ToString();
        }

        #endregion
    }
}
