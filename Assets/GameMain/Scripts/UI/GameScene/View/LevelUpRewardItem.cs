using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class LevelUpRewardItem : MonoBehaviour
    {
        [SerializeField] private IconArea _iconArea;

        [SerializeField] private TMP_Text _titleText;

        [SerializeField] private TMP_Text _typeText;

        [SerializeField] private TMP_Text _descriptionText;

        private LevelUpRewardItemContext _context;


        public void Init(LevelUpRewardItemContext context)
        {
            if (context == null)
            {
                Log.Error("LevelUpPropContext context is invalid.");
                return;
            }

            _context = context;

            if (_titleText != null) _titleText.text = context.Title;
            if (_descriptionText != null) _descriptionText.text = context.Description;
            if (_iconArea != null) _iconArea.OnInit(context.Icon, context.ItemRarity);

            LoadIcon(_context.IconAssetName);
        }

        private void LoadIcon(string iconAssetName)
        {
            if (_iconArea == null) return;

            if (string.IsNullOrEmpty(iconAssetName)) return;

            GameEntry.SpriteCache.GetSprite(iconAssetName, sprite => _iconArea.SetIcon(sprite));
        }
    }
}