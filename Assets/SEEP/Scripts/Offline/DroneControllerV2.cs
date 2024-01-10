using System;
using System.Collections;
using DavidFDev.DevConsole;
using DG.Tweening;
using NaughtyAttributes;
using SEEP.InputHandlers;
using SEEP.Utils;
using TMPro;
using UnityEngine;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Offline
{
    [RequireComponent(typeof(DroneInputHandler))]
    public class DroneControllerV2 : MonoBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Speed settings")] [SerializeField,]
        private MovementType movementType;

        [SerializeField, Range(0f, 100f), Tooltip("Max ground speed")]
        private float maxSpeed = 10f;

        [SerializeField] private bool enableSnapping = true;

        [SerializeField, Range(0f, 100f), Tooltip("Max snap speed, when snapping to ground working"), InfoBox(
             "Max snap speed needs to be not equal to max speed. It should be more or less then max speed",
             EInfoBoxType.Warning), ShowIf("enableSnapping")]
        private float maxSnapSpeed = 100f;

        [Space(2f)]
        [Header("Ground check settings")]
        [SerializeField, Min(0f), Tooltip("Distance to check ground with ray casts")]
        private float maxDistToGround;

        [SerializeField] private LayerMask groundMask = -1;

        [SerializeField] private LayerMask stairsMask = -1;

        [SerializeField] private bool enableBoosters;

        [SerializeField, ShowIf("enableBoosters")]
        private LayerMask boostersMask = -1;

        [Space(2f)]
        [Header("Acceleration settings")]
        [SerializeField, Range(0f, 100f), Tooltip("Maximum speed of acceleration on the ground")]
        private float maxAcceleration = 1f;

        [SerializeField, Range(0f, 100f), Tooltip("Maximum speed of acceleration on the ground")]
        private float maxAirAcceleration = 1f;

        [SerializeField, Range(0, 1)] private float reduceSidewaysStrength = 0.5f;

        [SerializeField, Range(0f, 1f)] private float speedSmoothTime = 0.1f;

        [SerializeField, ShowIf("enableBoosters"), Range(0f, 100f)]
        private float boosterAcceleration = 0f;

        [SerializeField] private bool compensateStairsGravity = false;

        [SerializeField, ShowIf("compensateStairsGravity"), InfoBox("Experimental feature", EInfoBoxType.Warning)]
        private bool compensateOnStraightStairs;

        [Space(2f)] [Header("Jump settings")] [SerializeField, Range(0f, 10f), Tooltip("Jump height in meters")]
        private float jumpHeight = 1f;

        [SerializeField, Range(0, 5), Tooltip("Count of available air jumps")]
        private int maxAirJumps = 1;

        [Space(2f), Header("Rotation options")] [SerializeField]
        private Transform playerInputSpace;

        [SerializeField] private RotationType rotationType = RotationType.RotateWithCamera;

        [SerializeField, Range(0f, 1f)] private float rotationSmoothTime = 0.04f;

        [SerializeField, Range(0f, 1f)] private float rotationVelocitySpeed = 0.04f;

        [SerializeField, Range(0f, 1f)] private float rotationVelocityThreshold = 0.1f;

        [SerializeField] private float rotationUpAxisSpeed = 0.5f;

        [Space(2f)] [Header("Angle settings")] [SerializeField, Range(0, 90), Tooltip("Max ground angle for moving")]
        private float maxGroundAngle = 25f;

        [SerializeField, Range(0, 90), Tooltip("Max angle for stairs")]
        private float maxStairsAngle = 45f;

        [Space(2f)] [Header("Debug settings")] [SerializeField]
        private bool showDebugInfo;

        [SerializeField, ShowIf("showDebugInfo")]
        private TextMeshProUGUI debugText;

        [SerializeField] private Transform cameraOrientation;

        #endregion

        #region PRIVATE VARIABLES

        private Rigidbody _rigidbody;

        private DroneInputHandler _inputHandler;

        private Vector3 _velocity, _desiredVelocity;

        //Хранит слой, которого последнего касался
        private int _lastGroundLayer;

        //Вектор нормали, полученной при контакте с землей
        private Vector3 _contactNormal;

        //Вектор нормали для склона
        private Vector3 _steepNormal;

        private bool _desiredJump;

        //Количество точек соприкосновения с землёй
        private int _groundContactCount;

        //Количество точек соприкосновения со стеной
        private int _steepContactCount;

        //Счётчик прыжков
        private int _jumpPhase;

        //Значение для расчёта точек соприкосновения
        private float _minGroundDotProduct;

        //Значение для расчёта точек соприкосновения с лестницей
        private float _minStairsDorProduct;

        //Счётчик шагов с момента последнего касания с землей, необходимо для прижимания вниз, на стыках
        private int _stepsSinceLastGrounded;

        //Счетчик с момента последнего прыжка, нужен для того, чтобы нейтрализовать негативное
        //воздействие прижимание к земле, когда хотим прыгать
        private float _stepsSinceLastJump;

        //Сила, с которой компенсируем скольжение по лестнице
        private Vector3 _stairsForce;

        //Валидирован ли вывод для дебага
        private bool _debugValidated;

        //Угол, на который вращается объект
        private float _calculatedAngle;

        //Текущая скорость с которой поворачивается объект
        private float _currentAngleVelocity;

        //Отображает скорость изменения вектора ускорения к вектору желаемому
        private Vector3 _currentVectorVelocity;

        //Вектор, который отображает скорость с которой тормозит, при отпускании управления
        private Vector2 _currentBrakeVelocity;

        //Направление гравитации
        private Vector3 _upAxis, _forwardAxis, _rightAxis, _gravityForce;

        private Quaternion _gravityRotation;

        private enum RotationType
        {
            RotateWithVelocity,
            RotateWithCamera
        }

        private enum MovementType
        {
            MoveWithSmooth
        }

        #endregion

        #region PUBLIC VARIABLES

        private bool OnGround => _groundContactCount > 0;
        private bool OnSteep => _steepContactCount > 0;

        #endregion

        #region MONOBEHAVIOUR

        private void OnValidate()
        {
            //Вычисляем значение, для подсчёта точек соприкосновения с землёй, используя максимальный угол для земли
            _minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

            //Аналогично со значением для лестницы
            _minStairsDorProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);

            if (enableSnapping)
            {
                if (Math.Abs(maxSnapSpeed - maxSpeed) < 0.01f)
                {
                    Logger.Warning(this,
                        "Max snapping speed should preferably not be equal to max speed. Please increase or decrease max snapping speed");
                }
            }
            else
            {
                maxSnapSpeed = 0;
            }

            if (enableBoosters)
            {
                if (boostersMask == -1)
                    Logger.Warning(this, "Please select booster layer!");
            }
            else
            {
                boosterAcceleration = 0f;
            }

            if (showDebugInfo)
            {
                if (debugText == null)
                {
                    Logger.Error(this, "Please choose text object for debug");
                }
                else
                {
                    _debugValidated = true;
                }
            }
        }

        private void Awake()
        {
            _inputHandler = GetComponent<DroneInputHandler>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            OnValidate();
#if DEBUG
            DebugInitialize();
#endif
        }

        private void Update()
        {
            if (DevConsole.IsOpen) return;

            if (playerInputSpace)
            {
                _rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, _upAxis);
                _forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, _upAxis);
                RotateObject();
                var forward = playerInputSpace.forward;
                //forward.y = 0f;
                forward.Normalize();
                var right = playerInputSpace.right;
                //right.y = 0f;
                right.Normalize();
                _desiredVelocity = (forward * _inputHandler.Control.y + right * _inputHandler.Control.x) * maxSpeed;
            }
            else
            {
                _rightAxis = ProjectDirectionOnPlane(Vector3.right, _upAxis);
                _forwardAxis = ProjectDirectionOnPlane(Vector3.forward, _upAxis);
                _desiredVelocity = new Vector3(_inputHandler.Control.x, 0f, _inputHandler.Control.y) * maxSpeed;
            }

            if (_debugValidated)
            {
                ShowDebugInfo();
            }

            //Кешируем прыжок
            _desiredJump |= Input.GetButtonDown("Jump");
        }

        private void FixedUpdate()
        {
            _gravityForce = CustomGravity.GetGravity(_rigidbody.position, out _upAxis);
            UpdateState();
            AdjustVelocity();
            if (compensateStairsGravity)
                CompensateStairsForce();
            if (enableBoosters)
                BoostAdjust();
            //Если хотим прыгнуть
            if (_desiredJump)
            {
                //Обнуляем флаг прыжка
                _desiredJump = false;
                //Вызываем обработчик прыжка
                Jump(_gravityForce);
            }

            _velocity += _gravityForce * Time.deltaTime;

            _rigidbody.velocity = _velocity;

            //Обнуляем необходимые переменные для сделующего шага
            ClearState();
        }

        private void OnCollisionEnter(Collision collision)
        {
            //Вызываем обработку того, стоим ли мы на земле
            EvaluateCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            //Вызываем обработку того, стоим ли мы на земле
            EvaluateCollision(collision);
        }

        #endregion

        #region METHODS

        private void EvaluateCollision(Collision collision)
        {
            //Получаем значение, взависимости от слоя
            var minDot = GetMinDot(collision.gameObject.layer);

            //Кешируем последнее значение слоя
            _lastGroundLayer = collision.gameObject.layer;

            //Перебираем все точки, которых мы касаемся
            for (var i = 0; i < collision.contactCount; i++)
            {
                //Получаем вектор нормали (указывает вверх, от точки прикосновени)
                var normal = collision.GetContact(i).normal;

                var upDot = Vector3.Dot(_upAxis, normal);

                //Если угол больше, чем вычисленное на OnValidate, который зависит от maxGroundAngle
                if (upDot >= minDot)
                {
                    //То считаем, что мы стоим на земле и добавляем к счётчику контактов с землей
                    _groundContactCount += 1;

                    //Добавляем нормаль, для прыжка, чтобы вычислить среднюю нормаль
                    _contactNormal += normal;
                }
                else if (upDot > -0.01f)
                {
                    //Считаем что касаемся склона
                    _steepContactCount += 1;
                    //Добавляем нормаль склона
                    _steepNormal += normal;
                }
            }
        }

        private void RotateObject()
        {
            _gravityRotation = Quaternion.LookRotation(_forwardAxis, _upAxis);
            switch (rotationType)
            {
                case RotationType.RotateWithVelocity:
                    if (_velocity.magnitude > rotationVelocityThreshold)
                    {
                        // Проекция вектора на плоскость
                        var projectedVector = _velocity - Vector3.Dot(_velocity, _upAxis) * _upAxis;

                        // Вычисляем координаты в новом базисе
                        var x = Vector3.Dot(projectedVector, _rightAxis);
                        var y = Vector3.Dot(projectedVector, _forwardAxis);

                        // Создаем двухмерный вектор
                        var inPlaneVector2D = new Vector2(x, y);

                        var targetAngle = Mathf.Atan2(inPlaneVector2D.x, inPlaneVector2D.y) * Mathf.Rad2Deg;

                        _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, targetAngle,
                            ref _currentAngleVelocity,
                            rotationVelocitySpeed);

                        //Apply calculated angle

                        transform.rotation = _gravityRotation * Quaternion.Euler(0, _calculatedAngle, 0);
                    }

                    break;
                case RotationType.RotateWithCamera:
                    _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, playerInputSpace.eulerAngles.y,
                        ref _currentAngleVelocity,
                        rotationSmoothTime);

                    //Apply calculated angle
                    //transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);
                    transform.rotation = _gravityRotation * Quaternion.Euler(0, _calculatedAngle, 0);
                    break;
            }
        }

        private void UpdateState()
        {
            //Добавляем шаг, что мы в воздухе с нашего последнего шага 
            _stepsSinceLastGrounded += 1;
            //Добавляем шаг, с момента нашего последнего прыжка
            _stepsSinceLastJump += 1;
            //Кешируем ускорение, ведь мы будем менять его вручную вместо AddForce
            _velocity = _rigidbody.velocity;

            //Проверяем заземлены мы, и можем ли присосаться к земле
            if (OnGround || SnapToGround() || CheckSteepContacts())
            {
                //Однако, если мы на земле, то всё же обнуляем счетчик шагов с последнего прикосновения
                _stepsSinceLastGrounded = 0;

                if (_stepsSinceLastJump > 1)
                {
                    _jumpPhase = 0;
                }

                //Обнуляем счетчик прыжков
                _jumpPhase = 0;

                //Если количество соприкосновений с землей, большего одного
                if (_groundContactCount > 1)
                {
                    //То нормализуем вектор, для определение направления вверх
                    _contactNormal.Normalize();
                }
            }
            else
            {
                //Если не на земле, то устанавливаем нормаль вверх
                _contactNormal = _upAxis;
            }
        }

        //Притягивает к земле, используется, когда по физике мы должны оторваться от земли, но по углу наклона это
        //всё ещё считается землёй. Соответсвенно мы не отрываемся от земли, а приятгиваемся к ней.
        //По крайне мере, пытаемся, учитывая нашу максимальную скорость притягивания к земле
        private bool SnapToGround()
        {
            //Если шагов с последнего касания земли, больше чем один
            //(больше чем один, потому, что мы считаем на шаг назад, относительно актуального состояния)
            //А также, если шагов с момента последнего прыжка, меньше, либо, равно двум
            if (_stepsSinceLastGrounded > 1 || _stepsSinceLastJump <= 2)
            {
                return false;
            }

            //Кешируем скорость
            var speed = _velocity.magnitude;

            //Если текущаю скорость больше чем максимальная скорость для притягивания
            if (speed > maxSnapSpeed)
            {
                return false;
            }

            //TODO: Добавить большее количество рейкастов, для точного контроля
            //Если ничего нету под рейкастом
            if (!Physics.Raycast(_rigidbody.position, -_upAxis, out var hit, maxDistToGround, groundMask))
            {
                return false;
            }

            var upDot = Vector3.Dot(_upAxis, hit.normal);

            //Проверяем точку, на возможное прикосновения
            if (upDot < GetMinDot(hit.collider.gameObject.layer))
            {
                return false;
            }

            //Если ничего из этого не сработало, то значит,
            //мы оторвались от земли на один кадр, но всё ещё находимся над ней

            //Устанавливаем как минимум одну точку контакта, а именно ту точку, куда уперся рейкаст
            _groundContactCount = 1;
            _contactNormal = hit.normal;

            //Теперь необходимо скорректировать скорость, чтобы мы действительно были на земле
            var dot = Vector3.Dot(_velocity, hit.normal);

            //Гравитация по-прежнему работает, если в процессе подсчёта, выходит,
            //что мы будем противиться гравитации, то пропускаем следующие код
            if (dot > 0f)
                _velocity = (_velocity - hit.normal * dot).normalized * speed;

            return true;
        }

        private void AdjustVelocity()
        {
            switch (movementType)
            {
                case MovementType.MoveWithSmooth:
                    if (_desiredVelocity.magnitude > 0.001f)
                    {
                        //Получаем новые оси, которые проецированы на нашу плоскость на которой мы находимся
                        //Позволяет не терять ускорение при взбирании на горку
                        var xAxis = ProjectDirectionOnPlane(_rightAxis, _contactNormal);
                        var zAxis = ProjectDirectionOnPlane(_forwardAxis, _contactNormal);

                        //Проецируем наше ускорение на полученные оси
                        var currentX = Vector3.Dot(_velocity, xAxis);
                        var currentZ = Vector3.Dot(_velocity, zAxis);

                        //Выбираем правильное ускорение, взависимости от нахождения на земле
                        var acceleration = OnGround ? maxAcceleration : maxAirAcceleration;

                        _velocity = Vector3.SmoothDamp(_velocity, _desiredVelocity, ref _currentVectorVelocity,
                            speedSmoothTime, acceleration);

                        /*var sideAxis = Vector3.Cross(_desiredVelocity, Vector3.up).normalized;
                        var sideForce = Vector3.Dot(_velocity, sideAxis);

                        _velocity -= sideAxis * (sideForce * reduceSidewaysStrength);*/
                    }
                    else if (_velocity.magnitude > 0.01f && OnGround)
                    {
                        //Получаем новые оси, которые проецированы на нашу плоскость на которой мы находимся
                        //Позволяет не терять ускорение при взбирании на горку
                        var xAxis = ProjectDirectionOnPlane(_rightAxis, _contactNormal);
                        var zAxis = ProjectDirectionOnPlane(_forwardAxis, _contactNormal);

                        //Проецируем наше ускорение на полученные оси
                        var currentX = Vector3.Dot(_velocity, xAxis);
                        var currentZ = Vector3.Dot(_velocity, zAxis);

                        var newVelocity = new Vector2(currentX, currentZ);
                        newVelocity = Vector2.SmoothDamp(newVelocity, Vector2.zero, ref _currentBrakeVelocity,
                            speedSmoothTime);

                        //Добавляем плавно скорость, используя наши новые оси
                        _velocity += xAxis * (newVelocity.x - currentX) + zAxis * (newVelocity.y - currentZ);
                    }

                    break;
            }
        }

        private void BoostAdjust()
        {
            if ((boostersMask.value & (1 << _lastGroundLayer)) == 0) return;
            var boostVector = ProjectOnContactPlane(_gravityForce) * Time.deltaTime;
            _velocity -= boostVector * boosterAcceleration;
        }

        private void CompensateStairsForce()
        {
            if (compensateOnStraightStairs)
            {
                /*var angle = Vector3.Dot(_contactNormal, Vector3.up);
                angle /= _contactNormal.magnitude * Vector3.up.magnitude;
                if ((stairsMask.value & (1 << _lastGroundLayer)) == 0 ||
                    !(Mathf.Acos(angle) <= _minGroundDotProduct)) return;
                    */

                _stairsForce = ProjectOnContactPlane(_gravityForce) * Time.deltaTime;
                _velocity -= _stairsForce;
            }
            else
            {
                if ((stairsMask.value & (1 << _lastGroundLayer)) == 0) return;
                _stairsForce = ProjectOnContactPlane(_gravityForce) * Time.deltaTime;
                _velocity -= _stairsForce;
            }
        }

        private void Jump(Vector3 gravity)
        {
            //Определяем направление прыжка
            Vector3 jumpDirection;
            if (OnGround)
            {
                jumpDirection = _contactNormal;
            }
            else if (OnSteep)
            {
                //Обнуляем счётчик прыжков, если мы отпрыгнули от стены
                _jumpPhase = 0;
                jumpDirection = _steepNormal;
            }
            else if (maxAirJumps > 0 && _jumpPhase <= maxAirJumps)
            {
                //Если мы падаем, но при этом не прыгали
                if (_jumpPhase == 0)
                    //То устанавливаем счетчик прыжка на 1, как будто мы прыгнули
                    _jumpPhase = 1;
                jumpDirection = _contactNormal;
            }
            else
            {
                return;
            }

            //Увеличиваем счётчик прыжков
            _jumpPhase += 1;

            //Обнуляем счётчик с момента последнего прыжка
            _stepsSinceLastJump = 0;

            //Высчитываем силу прыжка для прыжка на определенную высоту
            var jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);

            //Находим среднее направление между поверхностью от которой отталкиваемся и направлением вверх,
            //что позволяет отпрыгивать от стен прямо вверх
            jumpDirection = (jumpDirection + _upAxis).normalized;

            //Высчитываем скорость относительно нормали вектора
            var alignedSpeed = Vector3.Dot(_velocity, _contactNormal);
            //Если положительная, то прыгаем
            if (alignedSpeed > 0f)
            {
                //Ограничиваем скорость от максимальной и обнуляем в случае отрицательного результата
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }

            //Отпрыгиваем от нормали
            _velocity += jumpDirection * jumpSpeed;
        }

        private void ClearState()
        {
            //Обнуляем счетчик контактов
            _groundContactCount = _steepContactCount = 0;
            //Устаналиваем нормаль вверх
            _contactNormal = _steepNormal = Vector3.zero;
        }

        #endregion

        #region UTILS

        //Проверка на нахождение в трещине/ущелине, если нет, то нет смысла проверять на лестницу
        private bool CheckSteepContacts()
        {
            //Если количество контактов со склоном меньше 2, то выходим
            if (_steepContactCount <= 1) return false;

            //Нормализуем нормаль от склонов
            _steepNormal.Normalize();

            var upDot = Vector3.Dot(_upAxis, _steepNormal);
            //Если угол нормали меньше, чем значение зависимое от maxStairsAngle, то выходим
            if (!(upDot >= _minGroundDotProduct)) return false;

            //Иначе принимаем наш склон, как землю
            _groundContactCount = 1;
            _contactNormal = _steepNormal;
            return true;
        }

        //Проецирует вектор на нормаль полученную при соприкосновении с землей
        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - _contactNormal * Vector3.Dot(vector, _contactNormal);
        }

        private Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        //Выдаёт значение для расчета точек соприкосновения, взависимости от слоя
        private float GetMinDot(int layer)
        {
            if ((stairsMask.value & (1 << layer)) == 1 || (boostersMask.value & (1 << layer)) == 1)
                return _minStairsDorProduct;
            return (groundMask.value & (1 << layer)) == 1 ? _minGroundDotProduct : 0f;
        }

        private void ShowDebugInfo()
        {
            debugText.text = $"Velocity: {_rigidbody.velocity:f2} - Speed: {_rigidbody.velocity.magnitude:f2}" +
                             $"\nDesired velocity: {_desiredVelocity:f2}" +
                             $"\nLast Layer: {LayerMask.LayerToName(_lastGroundLayer)}" +
                             $"\nJump Phase: {_jumpPhase}" +
                             $"\nCompensate stairs gravity: {false}" +
                             $"\nOn steep: {OnSteep}" +
                             $"\nGravity: {_gravityForce}. Up axis: {_upAxis}. Sources: {CustomGravity.GravitySourceCount}";
        }

        #endregion

        #region DEBUG

#if DEBUG
        private void DebugInitialize()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Physics.simulationMode = SimulationMode.FixedUpdate;
            DevConsole.AddCommand(Command.Create<RotationType>(
                name: "rotationtype",
                aliases: "rotate",
                helpText: "Change rotation mode",
                p1: Parameter.Create(
                    name: "mode",
                    helpText: "Rotation mode"
                ),
                callback: mode => { rotationType = mode; }
            ));
        }
#endif

        #endregion
    }
}