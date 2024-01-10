using System;
using DG.Tweening;
using NaughtyAttributes;
using SEEP.Utils;
using UnityEngine;
using UnityEditor;

public enum OrbitPosition
{
    Top,
    Middle,
    Bottom
}

#region Helper Classes

[Serializable]
public class OrbitRing
{
    public float radius = 3f;
    public float height = 2.5f;
    public Color color = Color.red;

    public OrbitRing(float radius, float height, Color color)
    {
        this.radius = radius;
        this.height = height;
        this.color = color;
    }

    public OrbitRing(float radius, float height)
    {
        this.radius = radius;
        this.height = height;
        this.color = Color.green;
    }

    public float GetBorderDistanceToReference()
    {
        return Mathf.Sqrt((radius * radius) + (height * height));
    }
}

#endregion

[ExecuteInEditMode]
public class ThirdPersonCamera : MonoBehaviour
{
    #region Inspector Settings

    [Header("Editor Settings")] [SerializeField]
    private bool showGizmos = true;

    [SerializeField] private bool editorPreview = true;

    [Header("Targets")] [SerializeField] GameObject follow = null;
    [SerializeField] private GameObject lookAt = null;

    [Header("Orbits")] [SerializeField] private OrbitRing topRing = new OrbitRing(2f, 1.4f, Color.red);
    [SerializeField] private OrbitRing middleRing = new OrbitRing(5f, 3f, Color.red);
    [SerializeField] private OrbitRing bottomRing = new OrbitRing(1f, -1f, Color.red);

    [Header("Positioning")] [SerializeField]
    private bool lockHeight = false;

    [SerializeField] [ShowIf("lockHeight")]
    private float fixedHeight = .5f;

    [SerializeField] private bool lockTranslation = false;

    [SerializeField] [Range(0f, 360f)] [ShowIf("lockTranslation")]
    private float fixedTranslation = 0f;

    [SerializeField] private bool avoidClipping = true;

    [ShowIf(nameof(avoidClipping))] [SerializeField]
    private LayerMask avoidClippingLayerMask = 0;

    [ShowIf(nameof(avoidClipping))] [SerializeField]
    private float clipDistance = 5f;

    [ShowIf(nameof(avoidClipping))] [SerializeField]
    private float clippingOffset = 0f;

    [SerializeField] [Range(-180, 180)] private float horizontalTilt = 0f;
    [SerializeField] private float horizontalOffset = 0f;
    [SerializeField] [Range(-180, 180)] private float verticalTilt = 0f;
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private bool useCustomGravity = false;

    [SerializeField, ShowIf(nameof(useCustomGravity))]
    private float rotationUpAxisSpeed = 0.7f;

    [SerializeField, ShowIf(nameof(useCustomGravity))]
    private Transform customWorldUpAxis = null;

    [Header("Controls")] [SerializeField] private bool captureCursor = false;

    [Header("X axis")] [SerializeField] private string horizontalAxis = "Mouse X";
    [SerializeField] private float horizontalSensitivity = 1f;
    [SerializeField] private bool invertX = false;
    [Header("Y axis")] [SerializeField] private string verticalAxis = "Mouse Y";
    [SerializeField] private float verticalSensitivity = 0.8f;
    [SerializeField] private bool invertY = true;

    #endregion

    #region Private Variables

    private float _cameraTranslation = 0f;
    private float _verticalMultiplier = 10f;
    private float _referenceHeight = 0f;
    private float _referenceDistance;
    private float _noClippingHeight;
    private float _noClippingDistance;
    private OrbitRing _cameraRing = null;
    private Vector3 _gravityUp;
    private Vector3 _up;
    private Vector3 _right;
    private Vector3 _forward;
    private bool _rotationCompleted = true;

    #endregion

    // ===================== Lifecycle ===================== //

    #region Lifecycle Methods

    private void Start()
    {
        _referenceHeight = middleRing.height;
    }

    private void Update()
    {
        if ((!Application.isPlaying && !editorPreview) || !(Time.timeScale > 0)) return;

        if (captureCursor && Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        SetNormalVectors();
        SetPosition();
        SetRotation();
    }

    private void OnDrawGizmos()
    {
        if (follow == null || !showGizmos) return;

        DrawRing(topRing);
        DrawRing(middleRing);
        DrawRing(bottomRing);
    }

    #endregion

    // ===================== Update steps ===================== //

    #region Update Steps

    private void SetNormalVectors()
    {
        if (useCustomGravity)
        {
            var tempUpAxis = CustomGravity.GetUpAxis(follow.transform.position);
            if ((tempUpAxis != _gravityUp || _up != _gravityUp) && _rotationCompleted)
            {
                _rotationCompleted = false;
                _gravityUp = tempUpAxis;

                var tempGravityUpAxis = _gravityUp.normalized;

                // Вычисляем косинус угла между векторами
                var cosAngle = Vector3.Dot(tempGravityUpAxis, Vector3.forward);

                Vector3 calculatedForward;
                // Проверяем, являются ли векторы параллельными
                if (Mathf.Approximately(cosAngle, 1) || Mathf.Approximately(cosAngle, -1))
                {
                    calculatedForward = Vector3.Cross(_gravityUp, Vector3.up);
                    Debug.Log("up");
                }
                else
                {
                    calculatedForward = Vector3.Cross(_gravityUp, Vector3.forward);
                    Debug.Log("forward");
                }
                
                var gravityRotation = Quaternion.LookRotation(calculatedForward, _gravityUp);
                customWorldUpAxis.DORotate(gravityRotation.eulerAngles, rotationUpAxisSpeed, RotateMode.FastBeyond360)
                    .OnComplete(() => { _rotationCompleted = true; });
                //Debug.Log(vec);
            }

            _up = customWorldUpAxis.up;
            //Debug.Log();
        }
        else
        {
            _up = Vector3.up;
        }

        _right = Vector3.Cross(_up, Vector3.right);
        _forward = Vector3.Cross(_up, _right);
    }

    private void SetPosition()
    {
        ReadInputs();
        _referenceDistance = 0f;

        _cameraRing = GetCameraRing();

        _referenceHeight = _cameraRing.height;
        var distance = _cameraRing.GetBorderDistanceToReference();
        _referenceDistance = Mathf.Sqrt(distance * distance - _referenceHeight * _referenceHeight);
        if (avoidClipping)
        {
            CorrectClipping(Mathf.Min(distance, clipDistance));
        }

        var heightVector = _up * (avoidClipping ? _noClippingHeight : _referenceHeight);
        var distanceVector = -_forward * (avoidClipping ? _noClippingDistance : _referenceDistance);

        transform.position = follow.transform.position + heightVector + distanceVector;
        transform.RotateAround(follow.transform.position, _up, _cameraTranslation);
    }

    private void SetRotation()
    {
        LookAt(_up, lookAt.transform);

        var verticalAngles = _forward * verticalTilt;
        var horizontalAngles = _up * horizontalTilt;

        var eulerRotation = verticalAngles + horizontalAngles;

        transform.Rotate(eulerRotation.x, eulerRotation.y, eulerRotation.z);

        ApplyPositionOffset();
    }

    #endregion

    // ===================== Input ===================== //

    #region Input Methods

    private void ReadInputs()
    {
        if (lockHeight)
        {
            _referenceHeight = fixedHeight;
        }
        else if (Application.isPlaying)
        {
            _referenceHeight += Input.GetAxis(verticalAxis) * verticalSensitivity * (invertY ? -1 : 1);
        }

        if (lockTranslation)
        {
            _cameraTranslation = fixedTranslation;
        }
        else if (Application.isPlaying)
        {
            _cameraTranslation += Input.GetAxis(horizontalAxis) * _verticalMultiplier * horizontalSensitivity *
                                  (invertX ? -1 : 1);
            switch (_cameraTranslation)
            {
                case > 360f:
                    _cameraTranslation -= 360f;
                    break;
                case < 0f:
                    _cameraTranslation += 360f;
                    break;
            }
        }
    }

    #endregion

    // ===================== Positioning ===================== //

    #region Positioning Methods

    private OrbitRing GetCameraRing()
    {
        if (_referenceHeight >= topRing.height)
        {
            return new OrbitRing(topRing.radius, topRing.height);
        }

        if (_referenceHeight >= middleRing.height)
        {
            var radius = EaseLerpRingRadius(middleRing, topRing);
            return new OrbitRing(radius, _referenceHeight);
        }

        if (_referenceHeight >= bottomRing.height)
        {
            var radius = EaseLerpRingRadius(bottomRing, middleRing);
            return new OrbitRing(radius, _referenceHeight);
        }

        return new OrbitRing(bottomRing.radius, bottomRing.height);
    }

    private void CorrectClipping(float raycastDistance)
    {
        var ray = new Ray(follow.transform.position, (transform.position - follow.transform.position).normalized);

        if (Physics.Raycast(ray, out var hit, raycastDistance, avoidClippingLayerMask, QueryTriggerInteraction.Ignore))
        {
            var safeDistance = hit.distance - clippingOffset;
            var sinAngle = _referenceHeight / raycastDistance;
            var cosAngle = _referenceDistance / raycastDistance;

            _noClippingHeight = safeDistance * sinAngle;
            _noClippingDistance = safeDistance * cosAngle;
        }
        else
        {
            _noClippingHeight = _referenceHeight;
            _noClippingDistance = _referenceDistance;
        }
    }

    private void ApplyPositionOffset()
    {
        transform.position =
            transform.position + (transform.right * horizontalOffset) + (transform.up * verticalOffset);
    }

    #endregion

    // ===================== Rotation ===================== //

    #region Rotation Methods

    private void LookAt(Vector3 normal, Transform lookAt)
    {
        var targetDirection = (lookAt.position - transform.position).normalized;
        transform.localRotation = Quaternion.LookRotation(targetDirection, normal);
    }

    #endregion

    // ===================== Utils ===================== //

    #region Utils Methods

    private float EaseLerpRingRadius(OrbitRing r1, OrbitRing r2)
    {
        var lerpState = Mathf.InverseLerp(r1.height, r2.height, _referenceHeight);
        if (r1.radius > r2.radius)
        {
            lerpState = lerpState * lerpState;
        }
        else
        {
            lerpState = Mathf.Sqrt(lerpState);
        }

        var radius = Mathf.Lerp(r1.radius, r2.radius, lerpState);
        return radius;
    }

    private void DrawRing(OrbitRing ring)
    {
#if UNITY_EDITOR
        Handles.color = ring.color;
        var position = follow.transform.position + (_up * ring.height);
        Handles.DrawWireDisc(position, _up, ring.radius);
#endif
    }

    #endregion

    // ===================== Setters ===================== //

    #region Setters Methods

    public void SetFollow(GameObject follow)
    {
        this.follow = follow;
    }

    public void SetLookAt(GameObject lookAt)
    {
        this.lookAt = lookAt;
    }

    public void SetOrbitRing(OrbitPosition position, OrbitRing orbit)
    {
        switch (position)
        {
            case OrbitPosition.Top:
                topRing = orbit;
                break;
            case OrbitPosition.Middle:
                middleRing = orbit;
                break;
            case OrbitPosition.Bottom:
                bottomRing = orbit;
                break;
        }
    }

    public void SetLockHeight(bool lockHeight)
    {
        this.lockHeight = lockHeight;
    }

    public void SetLockTranslation(bool lockTranslation)
    {
        this.lockTranslation = lockTranslation;
    }

    public void SetAvoidClipping(bool avoidClipping)
    {
        this.avoidClipping = avoidClipping;
    }

    public void SetClipDistance(float clipDistance)
    {
        this.clipDistance = clipDistance;
    }

    public void SetClippingOffset(float clippingOffset)
    {
        this.clippingOffset = clippingOffset;
    }

    public void SetHorizontalTilt(float horizontalTilt)
    {
        this.horizontalTilt = horizontalTilt;
    }

    public void SetHorizontalOffset(float horizontalOffset)
    {
        this.horizontalOffset = horizontalOffset;
    }

    public void SetVerticalTilt(float verticalTilt)
    {
        this.verticalTilt = verticalTilt;
    }

    public void SetVerticalOffset(float verticalOffset)
    {
        this.verticalOffset = verticalOffset;
    }

    public void SetUseCustomGravity(bool useCustomGravity)
    {
        this.useCustomGravity = useCustomGravity;
    }

    public void SetCaptureCursor(bool captureCursor)
    {
        this.captureCursor = captureCursor;
    }

    public void SetHorizontalAxis(string horizontalAxis)
    {
        this.horizontalAxis = horizontalAxis;
    }

    public void SetHorizontalSensitivity(float horizontalSensitivity)
    {
        this.horizontalSensitivity = horizontalSensitivity;
    }

    public void SetInvertX(bool invertX)
    {
        this.invertX = invertX;
    }

    public void SetVerticalAxis(string verticalAxis)
    {
        this.verticalAxis = verticalAxis;
    }

    public void SetVerticalSensitivity(float verticalSensitivity)
    {
        this.verticalSensitivity = verticalSensitivity;
    }

    public void SetInvertY(bool invertY)
    {
        this.invertY = invertY;
    }

    #endregion
}