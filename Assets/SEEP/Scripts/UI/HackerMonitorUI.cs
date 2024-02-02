using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEEP.UI
{
    public class HackerMonitorUI : MonoBehaviour
    {
        private VisualElement _pointer;

        private Tweener _pointerTweener;

        private bool _isActive;

        private void OnEnable()
        {
            var document = GetComponent<UIDocument>();
            _pointer = document.rootVisualElement.Q<VisualElement>("Pointer");
            document.panelSettings.SetScreenToPanelSpaceFunction(_ =>
            {
                var invalidPosition = new Vector2(float.NaN, float.NaN);

                var cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (!Physics.Raycast(cameraRay, out var hit, 100f, LayerMask.GetMask("UI"))) return invalidPosition;

                var pixelUV = hit.textureCoord;

                pixelUV.y = 1 - pixelUV.y;

                pixelUV.x *= document.panelSettings.targetTexture.width;
                pixelUV.y *= document.panelSettings.targetTexture.height;

                if (_isActive)
                {
                    _pointer.style.left = pixelUV.x;
                    _pointer.style.top = pixelUV.y;
                }

                return pixelUV;
            });
        }

        public void SwitchPointer(bool enable)
        {
            if ((enable && _pointer.style.opacity.value == 1) ||
                (!enable && _pointer.style.opacity.value == 0)) return;

            _pointerTweener?.Kill();

            _isActive = enable;

            _pointerTweener = DOVirtual.Float(_pointer.style.opacity.value, enable ? 1f : 0f, 0.07f,
                value => { _pointer.style.opacity = value; });
        }
    }
}