using System;
using System.Collections.Generic;
using Definition;
using GameFramework.Resource;
using CustomUtility;
using UnityEngine;
using UnityGameFramework.Runtime;

public class SpriteCacheComponent : GameFrameworkComponent
{
    [SerializeField] private float _pixelsPerUnit = 100f;
    [SerializeField] private Vector2 _defaultPivot = new(0.5f, 0.5f);
    
    private Dictionary<string, Sprite> _spriteCache;
    private ResourceComponent _resource;

    void Start()
    {
        _spriteCache = new Dictionary<string, Sprite>();
        _resource = GameEntry.Resource;
    }

    public void GetSprite(string assetName, Action<Sprite> callback)
    {
        if (_spriteCache.TryGetValue(assetName, out var sprite))
        {
            callback?.Invoke(sprite);
            return;
        }
        else
        {
            _resource.LoadAsset
            (
                AssetUtility.GetUITextureIconAsset(assetName),
                Constant.AssetPriority.UIFormAsset,
                new LoadAssetCallbacks(
                    (resourcePath, asset, duration, userData) =>
                    {
                        Log.Debug(resourcePath);
                        Texture2D texture = asset as Texture2D;
                        if (texture != null)
                        {
                            Sprite newSprite = Sprite.Create(
                                texture,
                                new Rect(0, 0, texture.width, texture.height),
                                _defaultPivot,
                                _pixelsPerUnit);
                            _spriteCache.TryAdd(assetName, newSprite);
                            callback?.Invoke(newSprite);
                        }
                    },
                    (resourcePath, status, errorMessage, userData) =>
                    {
                        Log.Error("Can not load icon '{0}' from '{1}' with error message '{2}'.",
                            assetName,
                            resourcePath,
                            errorMessage);
                    }
                )
            );
        }
    }

    private void OnDestroy()
    {
        _spriteCache.Clear();
        _resource = null;
    }
}