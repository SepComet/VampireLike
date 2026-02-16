using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class DisplayItemInfoForm : UGuiForm
    {
        [SerializeField] private RectTransform _content;

        [SerializeField] private IconArea _iconArea;

        [SerializeField] private TMP_Text _itemTitle;

        [SerializeField] private TMP_Text _itemTypeText;

        [SerializeField] private TMP_Text _itemDescription;

        [SerializeField] private CommonButton _recycleButton;

        [SerializeField] private TMP_Text _itemPrice;

        [SerializeField] private bool _autoResizeHeight = true;

        [SerializeField] private float _fixedTopHeight = 150f;

        [SerializeField] private float _fixedBottomHeight = 150f;

        [SerializeField] private float _fixedWidth = 300f;

        [SerializeField] private float _screenEdgePadding = 0f;

        private DisplayItemInfoFormContext _context;
        private Vector3 _targetPos;

        public void RefreshUI(DisplayItemInfoFormContext context)
        {
            if (context == null)
            {
                Log.Warning("DisplayItemInfoForm context is invalid.");
                return;
            }

            _context = context;

            _targetPos = ConvertWorldToAnchored(_context.TargetPos);

            if (_content != null)
            {
                _content.anchoredPosition3D = _targetPos;
            }

            if (_itemTitle != null) _itemTitle.text = _context.Title ?? string.Empty;
            if (_itemTypeText != null) _itemTypeText.text = _context.TypeText ?? string.Empty;
            if (_itemDescription != null) _itemDescription.text = _context.Description ?? string.Empty;
            if (_itemPrice != null) _itemPrice.text = $"{_context.Price} <sprite name=\"coin\" index=0>";

            if (_recycleButton != null)
            {
                _recycleButton.gameObject.SetActive(_context.IsWeapon);
            }

            if (_iconArea != null)
            {
                if (!string.IsNullOrEmpty(_context.IconAssetName))
                {
                    string iconAssetName = _context.IconAssetName;
                    GameEntry.SpriteCache.GetSprite(iconAssetName, sprite =>
                    {
                        if (_context != null && _context.IconAssetName == iconAssetName)
                        {
                            _iconArea.OnInit(sprite, context.Rarity);
                            ResizeToFitContent();
                            ApplyClampedTargetPos();
                        }
                    });
                }
            }

            ResizeToFitContent();
            ApplyClampedTargetPos();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (!(userData is DisplayItemInfoFormContext context))
            {
                Log.Error("DisplayItemInfoFormContext is invalid.");
                return;
            }

            RefreshUI(context);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            _context = null;
            base.OnClose(isShutdown, userData);
        }

        private void ResizeToFitContent()
        {
            if (!_autoResizeHeight || _content == null)
            {
                return;
            }

            if (_itemDescription == null)
            {
                return;
            }

            _itemDescription.ForceMeshUpdate();
            var descriptionSize = _itemDescription.rectTransform.sizeDelta;
            descriptionSize.y = _itemDescription.preferredHeight;
            _itemDescription.rectTransform.sizeDelta = descriptionSize;
            float descriptionHeight = Mathf.Max(0f, _itemDescription.preferredHeight);
            float targetHeight = _fixedTopHeight + descriptionHeight + _fixedBottomHeight;

            Vector2 size = new Vector2(_fixedWidth, targetHeight);
            _content.sizeDelta = size;
        }

        private void ApplyClampedTargetPos()
        {
            if (_content == null)
            {
                return;
            }

            RectTransform parent = _content.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            Vector2 size = _content.sizeDelta;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = _content.rect.size;
            }

            Vector2 pivot = _content.pivot;
            float halfWidth = parent.rect.width * 0.5f;
            float halfHeight = parent.rect.height * 0.5f;

            float minX = -halfWidth + size.x * pivot.x + _screenEdgePadding;
            float maxX = halfWidth - size.x * (1f - pivot.x) - _screenEdgePadding;
            float minY = -halfHeight + size.y * pivot.y + _screenEdgePadding;
            float maxY = halfHeight - size.y * (1f - pivot.y) - _screenEdgePadding;

            float clampedX = minX <= maxX ? Mathf.Clamp(_targetPos.x, minX, maxX) : 0f;
            float clampedY = minY <= maxY ? Mathf.Clamp(_targetPos.y, minY, maxY) : 0f;

            _content.anchoredPosition3D = new Vector3(clampedX, clampedY, _targetPos.z);
        }

        private Vector3 ConvertWorldToAnchored(Vector3 worldPos)
        {
            if (_content == null)
            {
                return worldPos;
            }

            RectTransform parent = _content.parent as RectTransform;
            if (parent == null)
            {
                return worldPos;
            }

            Canvas canvas = _content.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, uiCamera, out Vector2 localPoint))
            {
                return worldPos;
            }

            float z = _content.anchoredPosition3D.z;
            return new Vector3(localPoint.x, localPoint.y, z);
        }
    }
}
