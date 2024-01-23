using System.Collections;
using System.Linq;
using DG.Tweening;
using SEEP.InputHandlers;
using TMPro;
using UnityEngine;
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
        [SerializeField] public Vector3 markerOffset;

        private bool _isInitialized;
        
        private DroneInputHandler _inputHandler;
        private Collider[] _colliders;
        private int _interactableObjectsCount;
        private GameObject _closestInteractable;
        private Vector3 _startPointerSize;
        private GameObject _marker;
        private RectTransform _pointerTransform;
        private Image _pointerImage;
        private Sprite _forwardPointer;
        private Sprite _sidePointer;
        private TextMeshProUGUI _pointerText;

        private Vector3 _calculatedVelocity;

        private Camera _camera;

        private IEnumerator Start()
        {
            _camera = Camera.main;
            _inputHandler = GetComponent<DroneInputHandler>();
            _colliders = new Collider[bufferSize];
            var canvas = GameObject.FindGameObjectWithTag("Player UI").transform;
            _marker = Resources.Load<GameObject>("Interact");
            _marker = Instantiate(_marker, canvas);
            
            _pointerTransform = _marker.GetComponent<RectTransform>();
            _pointerImage = _marker.GetComponent<Image>();

            var textChild = _marker.transform.GetChild(0);
            _pointerText = textChild.GetComponent<TextMeshProUGUI>();

            _startPointerSize = _pointerTransform.sizeDelta;
            _sidePointer = Resources.Load<Sprite>("UI/SidePointer");
            _forwardPointer = Resources.Load<Sprite>("UI/ForwardPointer");
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
                _marker.SetActive(false);
                return;
            }

            _marker.SetActive(true);

            _closestInteractable = _colliders.OrderBy(obj =>
                obj ? Vector3.Distance(obj.transform.position, transform.position) : Mathf.Infinity
            ).First().gameObject;
            
            if (!_inputHandler.Interact) return;

            _closestInteractable.GetComponent<IInteractable>().Interact(this);
        }
        
        private void LateUpdate()
        {
            if (!_isInitialized || _interactableObjectsCount <= 0) return;
            
            // Giving limits to the icon so it sticks on the screen
            // Below calculations witht the assumption that the icon anchor point is in the middle
            // Minimum X position: half of the icon width
            var minX = _pointerImage.GetPixelAdjustedRect().width / 2;
            // Maximum X position: screen width - half of the icon width
            var maxX = Screen.width - minX;

            // Minimum Y position: half of the height
            var minY = _pointerImage.GetPixelAdjustedRect().height / 2;
            // Maximum Y position: screen height - half of the icon height
            var maxY = Screen.height - minY;

            // Temporary variable to store the converted position from 3D world point to 2D screen point
            var realPos = _camera.WorldToScreenPoint(_closestInteractable.transform.position );
            var pos = realPos;

            // Check if the target is behind us, to only show the icon once the target is in front
            if(Vector3.Dot((_closestInteractable.transform.position - _camera.transform.position), _camera.transform.forward) < 0)
            {
               // _pointerImage.sprite = _sidePointer;
                // Check if the target is on the left side of the screen
                // Place it on the right (Since it's behind the player, it's the opposite)
                pos.x = pos.x < Screen.width / 2 ? maxX : minX;
            }
            /*else
            {
                _pointerImage.sprite = _forwardPointer;
            }*/

            // Limit the X and Y positions
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            
            //_pointerTransform.rotation = RotatePointer(1 * realPos - pos);

            // Update the marker's position
            _pointerImage.transform.position = Vector3.SmoothDamp(_pointerImage.transform.position, pos, ref _calculatedVelocity, smoothTime);;
            // Change the meter text to the distance with the meter unit 'm'
            //meter.text = ((int)Vector3.Distance(target.position, transform.position)).ToString() + "m";
        }
        
        private static Quaternion RotatePointer(Vector2 direction) // поворачивает PointerUI в направление direction
        {		
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}