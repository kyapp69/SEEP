using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = SEEP.Utils.Logger;

namespace SEEP.UI
{
    public class HackerUI : MonoBehaviour
    {
        [SerializeField] private float fadeTime = 0.07f;

        private VisualElement _pointer;
        private Label _interactText;

        private Tweener _pointerTweener;
        private Tweener _textTweener;

        private void OnEnable()
        {
            var document = GetComponent<UIDocument>();

            var root = document.rootVisualElement;

            _pointer = root.Q<VisualElement>("Pointer");
            _interactText = root.Q<Label>("InteractText");
        }

        public void SwitchPointer(bool enable)
        {
            if ((enable && _pointer.style.opacity.value == 1) || 
                (!enable && _pointer.style.opacity.value == 0)) return;
            
            _pointerTweener?.Kill();

            _pointerTweener = DOVirtual.Float(_pointer.style.opacity.value, enable ? 1f : 0f, fadeTime,
                value => { _pointer.style.opacity = value; });
        }

        public void SwitchText(bool enable)
        {
            if ((enable && _interactText.style.opacity.value == 1) ||
                (!enable && _interactText.style.opacity.value == 0)) return;
            
            _textTweener?.Kill();

            _textTweener = DOVirtual.Float(_interactText.style.opacity.value, enable ? 1f : 0f, fadeTime,
                value => { _interactText.style.opacity = value; });
        }
    }
}