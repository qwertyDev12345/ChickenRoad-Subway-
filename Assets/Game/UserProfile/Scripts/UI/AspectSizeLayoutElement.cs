using System;
using UnityEngine;
using UnityEngine.UI;

namespace UserProfile.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class AspectSizeLayoutElement : LayoutElement
    {
        private Image _image = null;
        private RectTransform _rectTransform = null;

        private void Update()
        {
            if (_image == null)
                _image = GetComponent<Image>();

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            float width = _rectTransform.rect.height;

            var rect = _image.sprite.rect;
            
            float aspect = rect.width / rect.height;
            
            width *= aspect;
            
            if (this.preferredWidth != width)
                this.preferredWidth = width;
            
            if (this.minWidth != width)
                this.minWidth = width;
        }
    }
}
