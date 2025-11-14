using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapOverlayRenderer : MonoBehaviour {
    static MinimapOverlayRenderer _instance;
    static readonly List<MinimapIcon> _pendingIcons = new();
    static Sprite _fallbackCircleSprite;
    static Texture2D _fallbackCircleTexture;

    [SerializeField]
    MinimapConfig _config;
    [SerializeField]
    Camera _minimapCamera;
    [SerializeField]
    RectTransform _iconRoot;
    [SerializeField]
    Image _iconTemplate;
    [SerializeField]
    Image _playerDirectionTemplate;
    [SerializeField]
    bool _hideWhenOutsideBounds = true;

    readonly Dictionary<MinimapIcon, IconEntry> _entries = new();
    readonly List<MinimapIcon> _orderedIcons = new();
    readonly Queue<Image> _pool = new();
    Image _playerDirectionImage;
    RectTransform _playerDirectionRect;
    Vector3 _lastCameraFlatForward = Vector3.forward;
    bool _hasLastCameraFlatForward;

    void Awake() {
        if (_instance != null && _instance != this) {
            Debug.LogWarning("Multiple MinimapOverlayRenderer instances detected. The latest instance will be used.");
        }
        _instance = this;
        if (_iconRoot == null)
            _iconRoot = transform as RectTransform;
        if (_iconTemplate != null)
            _iconTemplate.gameObject.SetActive(false);

        if (_pendingIcons.Count > 0) {
            foreach (var icon in _pendingIcons)
                InternalRegister(icon);
            _pendingIcons.Clear();
        }
    }

    void OnDestroy() {
        if (_instance == this)
            _instance = null;
    }

    void Update() {
        if (_minimapCamera == null || _config == null)
            return;
        bool coneUpdatedThisFrame = false;
        for (int i = 0; i < _orderedIcons.Count; i++) {
            var icon = _orderedIcons[i];
            if (icon == null)
                continue;
            if (_entries.TryGetValue(icon, out var entry)) {
                var visible = UpdateIcon(entry, out var anchorPos);
                if (icon.IconType == MinimapIconType.Player) {
                    coneUpdatedThisFrame = true;
                    UpdatePlayerDirectionCone(icon, visible, anchorPos);
                }
            }
        }

        if (!coneUpdatedThisFrame)
            HidePlayerDirectionCone();
    }

    static public void Register(MinimapIcon icon) {
        if (icon == null)
            return;
        if (_instance == null) {
            if (!_pendingIcons.Contains(icon))
                _pendingIcons.Add(icon);
            return;
        }
        _instance.InternalRegister(icon);
    }

    static public void Unregister(MinimapIcon icon) {
        if (icon == null)
            return;
        if (_instance == null) {
            _pendingIcons.Remove(icon);
            return;
        }
        _instance.InternalUnregister(icon);
    }

    void InternalRegister(MinimapIcon icon) {
        if (_entries.ContainsKey(icon))
            return;
        var image = GetImageFromPool();
        var entry = new IconEntry(icon, image);
        _entries.Add(icon, entry);
        _orderedIcons.Add(icon);
    }

    void InternalUnregister(MinimapIcon icon) {
        if (!_entries.TryGetValue(icon, out var entry))
            return;
        _entries.Remove(icon);
        _orderedIcons.Remove(icon);
        ReturnToPool(entry.Image);
    }

    bool UpdateIcon(IconEntry entry, out Vector2 anchorPos) {
        anchorPos = default;
        var icon = entry.Icon;
        if (icon == null) {
            ReturnToPool(entry.Image);
            return false;
        }

        var viewportPos = _minimapCamera.WorldToViewportPoint(icon.WorldPosition);
        bool inFront = viewportPos.z > 0;
        bool withinBounds = viewportPos.x is > 0f and < 1f && viewportPos.y is > 0f and < 1f;

        if (!inFront) {
            entry.Image.enabled = false;
            return false;
        }

        if (_hideWhenOutsideBounds && !withinBounds) {
            entry.Image.enabled = false;
            return false;
        }

        if (!_hideWhenOutsideBounds && !withinBounds)
            viewportPos = ClampViewport(viewportPos);

        entry.Image.enabled = true;

        Vector2 anchored = ViewportToAnchoredPosition(viewportPos);
        anchored = ClampAnchoredPosition(anchored);
        entry.Rect.anchoredPosition = anchored;
        anchorPos = anchored;

        var baseStyle = _config.GetStyle(icon.IconType);
        var color = icon.HasColorOverride ? icon.ColorOverride : baseStyle.Color;
        var diameter = icon.HasSizeOverride ? icon.SizeOverride : baseStyle.Diameter;

        entry.Image.color = color;
        entry.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, diameter);
        entry.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, diameter);
        return true;
    }

    Image GetImageFromPool() {
        Image image = _pool.Count > 0 ? _pool.Dequeue() : CreateImage();
        image.gameObject.SetActive(true);
        return image;
    }

    void ReturnToPool(Image image) {
        if (image == null)
            return;
        image.gameObject.SetActive(false);
        _pool.Enqueue(image);
    }

    Image CreateImage() {
        if (_iconRoot == null)
            _iconRoot = transform as RectTransform;
        Image image;
        if (_iconTemplate != null) {
            image = Instantiate(_iconTemplate, _iconRoot);
        }
        else {
            var go = new GameObject("MinimapIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_iconRoot, false);
            image = go.GetComponent<Image>();
            image.raycastTarget = false;
            if (image.sprite == null)
                image.sprite = GetFallbackCircleSprite();
        }
        return image;
    }

    Vector3 ClampViewport(Vector3 viewportPos) {
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);
        return viewportPos;
    }

    Vector2 ViewportToAnchoredPosition(Vector3 viewport) {
        if (_iconRoot == null)
            _iconRoot = transform as RectTransform;

        var rect = _iconRoot.rect;
        return new Vector2(
            (viewport.x - 0.5f) * rect.width,
            (viewport.y - 0.5f) * rect.height
        );
    }

    Vector2 ClampAnchoredPosition(Vector2 anchored) {
        if (_config == null || _iconRoot == null)
            return anchored;
        float padding = _config.IconEdgePadding;
        var rect = _iconRoot.rect;
        float halfWidth = rect.width * 0.5f - padding;
        float halfHeight = rect.height * 0.5f - padding;
        anchored.x = Mathf.Clamp(anchored.x, -halfWidth, halfWidth);
        anchored.y = Mathf.Clamp(anchored.y, -halfHeight, halfHeight);
        return anchored;
    }

    static Sprite GetFallbackCircleSprite() {
        if (_fallbackCircleSprite != null)
            return _fallbackCircleSprite;

        const int size = 32;
        _fallbackCircleTexture = new Texture2D(size, size, TextureFormat.ARGB32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "MinimapFallbackCircle"
        };

        Color[] pixels = new Color[size * size];
        float radius = size * 0.5f - 1f;
        Vector2 center = new(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                int idx = y * size + x;
                Vector2 pos = new(x + 0.5f, y + 0.5f);
                float dist = Vector2.Distance(pos, center);
                float alpha = dist <= radius ? 1f : 0f;
                pixels[idx] = new Color(1f, 1f, 1f, alpha);
            }
        }
        _fallbackCircleTexture.SetPixels(pixels);
        _fallbackCircleTexture.Apply();

        _fallbackCircleSprite = Sprite.Create(
            _fallbackCircleTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        _fallbackCircleSprite.name = "MinimapFallbackCircleSprite";

        return _fallbackCircleSprite;
    }

    void UpdatePlayerDirectionCone(MinimapIcon playerIcon, bool iconVisible, Vector2 anchorPos) {
        if (_config == null || !_config.ShowPlayerDirectionCone || !iconVisible) {
            HidePlayerDirectionCone();
            return;
        }
        EnsurePlayerDirectionCone();
        if (_playerDirectionRect == null || _playerDirectionImage == null)
            return;

        _playerDirectionRect.anchoredPosition = anchorPos;
        _playerDirectionImage.enabled = true;
        _playerDirectionImage.color = _config.PlayerDirectionColor;
        _playerDirectionImage.fillAmount = Mathf.Clamp01(_config.PlayerDirectionAngle / 360f);

        float size = _config.PlayerDirectionLength;
        _playerDirectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        _playerDirectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        _playerDirectionRect.pivot = new Vector2(0.5f, 0.5f);
        _playerDirectionRect.SetAsFirstSibling();

        if (GameManager.T == null || GameManager.T.Cam == null) {
            HidePlayerDirectionCone();
            return;
        }

        Vector3 cameraForward = GameManager.T.Cam.transform.forward;
        Vector3 flatForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f) {
            flatForward = _hasLastCameraFlatForward ? _lastCameraFlatForward : Vector3.forward;
        }
        else {
            flatForward.Normalize();
            _lastCameraFlatForward = flatForward;
            _hasLastCameraFlatForward = true;
        }

        Vector3 minimapForward = Vector3.ProjectOnPlane(_minimapCamera.transform.up, Vector3.up);
        if (minimapForward.sqrMagnitude < 0.0001f)
            minimapForward = Vector3.forward;
        else
            minimapForward.Normalize();

        Vector3 minimapRight = Vector3.Cross(Vector3.up, minimapForward);
        if (minimapRight.sqrMagnitude < 0.0001f)
            minimapRight = Vector3.right;
        else
            minimapRight.Normalize();

        float x = Vector3.Dot(flatForward, minimapRight);
        float y = Vector3.Dot(flatForward, minimapForward);
        float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg - 30f;
        _playerDirectionRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    void HidePlayerDirectionCone() {
        if (_playerDirectionImage != null)
            _playerDirectionImage.enabled = false;
    }

    void EnsurePlayerDirectionCone() {
        if (_playerDirectionImage != null)
            return;
        if (_iconRoot == null)
            _iconRoot = transform as RectTransform;
        if (_iconRoot == null)
            return;

        if (_playerDirectionTemplate != null) {
            _playerDirectionImage = Instantiate(_playerDirectionTemplate, _iconRoot);
        }
        else {
            var go = new GameObject("PlayerDirectionCone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_iconRoot, false);
            _playerDirectionImage = go.GetComponent<Image>();
            _playerDirectionImage.sprite = GetFallbackCircleSprite();
            _playerDirectionImage.type = Image.Type.Filled;
            _playerDirectionImage.fillMethod = Image.FillMethod.Radial360;
            _playerDirectionImage.fillOrigin = 2;
            _playerDirectionImage.raycastTarget = false;
        }
        _playerDirectionRect = _playerDirectionImage.rectTransform;
        _playerDirectionImage.enabled = false;
    }

    class IconEntry {
        public readonly MinimapIcon Icon;
        public readonly RectTransform Rect;
        public readonly Image Image;

        public IconEntry(MinimapIcon icon, Image image) {
            Icon = icon;
            Image = image;
            Rect = image.rectTransform;
        }
    }
}

