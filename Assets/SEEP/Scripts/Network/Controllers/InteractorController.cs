using System.Collections;
using System.Linq;
using DG.Tweening;
using FishNet.Object;
using SEEP.InputHandlers;
using TMPro;
using UnityEngine;
using SEEP.Utils.Typewriter;
using UnityEngine.UI;

namespace SEEP.Network.Controllers
{
    [RequireComponent(typeof(DroneInputHandler))]
    public class InteractorController : MonoBehaviour
    {
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField, Range(0.1f, 5f)] private float interactiveRange = 0.5f;
        [SerializeField] private int bufferSize = 3;
        [SerializeField] private float smoothTime = 0.1f;

        private bool _isInitialized;
        private bool _isVisible;
        
        private DroneInputHandler _inputHandler;
        
        private Collider[] _colliders;
        private int _interactableObjectsCount;
        private GameObject _closestInteractableObject;
        private IInteractable _interactable;
        private bool _isBehind;

        private Tweener _tweener;
        
        private GameObject _marker;
        private Image _pointerImage;
        private TextMeshProUGUI _pointerText;
        private string _currentText;
        private Typewriter _typewriter;
        private Coroutine _changeTextCoroutine;

        private Vector3 _calculatedVelocity;

        private Camera _camera;

        private IEnumerator Start()
        {
            if (TryGetComponent<NetworkBehaviour>(out var networkController))
            {
                if (!networkController.Owner.IsLocalClient)
                {
                    Destroy(this);
                    yield return null;
                }
            }
            _camera = Camera.main;
            _inputHandler = GetComponent<DroneInputHandler>();
            _colliders = new Collider[bufferSize];
            var canvas = GameObject.FindGameObjectWithTag("Player UI").transform;
            _marker = Resources.Load<GameObject>("Interact");
            _marker = Instantiate(_marker, canvas);
            
            _marker.GetComponent<RectTransform>();
            _pointerImage = _marker.GetComponent<Image>();
            _pointerImage.color = new Color(255, 255, 255, 0);

            var textChild = _marker.transform.GetChild(0);
            _pointerText = textChild.GetComponent<TextMeshProUGUI>();
            _pointerText.color = new Color(255, 255, 255, 0);

            _typewriter = textChild.GetComponent<Typewriter>();
            _typewriter.SetTargetTextMesh(_pointerText);
            _typewriter.Animate();
            
            _isInitialized = true;
            yield return null;
        }

        private void Update()
        {
            if (!_isInitialized) return;
            
            _interactableObjectsCount =
                Physics.OverlapSphereNonAlloc(transform.position, interactiveRange, _colliders, interactableLayer);

            if (_interactableObjectsCount <= 0)
            {
                ChangeVisibility(false);
                return;
            }

            ChangeVisibility(true);

            _closestInteractableObject = _colliders.OrderBy(obj =>
                obj ? Vector3.Distance(obj.transform.position, transform.position) : Mathf.Infinity
            ).First().gameObject;
            
            _interactable = _closestInteractableObject.GetComponent<IInteractable>();
            
            ChangeVisibility(!_isBehind);
            
            if (!_inputHandler.Interact || _isBehind) return;

            _interactable.Interact(this);
        }

        private void ChangeVisibility(bool newState)
        {
            if (_isVisible == newState) return;

            _tweener?.Kill();

            _isVisible = newState;
            
            if(!_isVisible)
                _typewriter.Stop();
            _tweener = DOVirtual.Float(_pointerImage.color.a, _isVisible ? 1f : 0f, smoothTime, value =>
            {
                var tempColor = new Color(255, 255, 255, value);
                _pointerImage.color = tempColor;
                _pointerText.color = tempColor;
            });
        }
        
        private void LateUpdate()
        {
            if (!_isInitialized || _interactableObjectsCount <= 0) return;

            var minX = _pointerImage.GetPixelAdjustedRect().width / 2;
            var maxX = Screen.width - minX;
            var minY = _pointerImage.GetPixelAdjustedRect().height / 2;
            var maxY = Screen.height - minY;
            var realPos = _camera.WorldToScreenPoint(_closestInteractableObject.transform.position );
            var pos = realPos;
            
            _isBehind = !IsObjectInCameraView();

            if (!_isVisible || _isBehind) return;

            // Limit the X and Y positions
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            
            _pointerImage.transform.position = Vector3.SmoothDamp(_pointerImage.transform.position, pos, ref _calculatedVelocity, smoothTime);;
            var interactorText = _interactable.GetMessage();

            if (_currentText == interactorText) return;
            
            if(_typewriter.IsWorking)
                _typewriter.Stop();

            _currentText = interactorText;
            _typewriter.Animate(interactorText);
        }
        
        private bool IsObjectInCameraView()
        {
            var directionToObject = _closestInteractableObject.transform.position - _camera.transform.position;
            var angleToObject = Vector3.Angle(_camera.transform.forward, directionToObject);

            return angleToObject <= _camera.fieldOfView / 1.5f;
        }
    }
}