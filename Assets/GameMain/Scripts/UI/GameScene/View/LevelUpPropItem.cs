using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class LevelUpPropItem : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;

        [SerializeField] private TMP_Text _titleText;

        [SerializeField] private TMP_Text _typeText;

        [SerializeField] private TMP_Text _descriptionText;

        private LevelUpPropContext _context;


        public void Init(LevelUpPropContext context)
        {
            if (context == null)
            {
                Log.Error("LevelUpPropContext context is invalid.");
                return;
            }

            _context = context;

            if (_titleText != null) _titleText.text = context.Title;
            if (_typeText != null) _typeText.text = context.Type;
            if (_descriptionText != null) _descriptionText.text = context.Description;
            if (_iconImage != null) _iconImage.sprite = context.Icon;

            LoadIcon(_context.IconAssetName);
        }

        private void LoadIcon(string iconAssetName)
        {
            if (_iconImage == null) return;

            if (string.IsNullOrEmpty(iconAssetName)) return;

            GameEntry.SpriteCache.GetSprite(iconAssetName, sprite => _iconImage.sprite = sprite);
        }
    }
}