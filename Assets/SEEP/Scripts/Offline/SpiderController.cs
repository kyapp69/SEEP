using System;
using SEEP.InputHandlers;
using UnityEngine;

namespace SEEP.Offline
{
    [RequireComponent(typeof(SpiderInputHandler))]
    public class SpiderController : MonoBehaviour
    {
        [SerializeField] private float _speed = 3f;
        [SerializeField] private float smoothness = 5f;
        [SerializeField] private int raysNb = 8;
        [SerializeField] private float raysEccentricity = 0.2f;
        [SerializeField] private float outerRaysOffset = 2f;
        [SerializeField] private float innerRaysOffset = 25f;

        private Vector3 velocity;
        private Vector3 lastVelocity;
        private Vector3 lastPosition;
        private Vector3 forward;
        private Vector3 upward;
        private Quaternion lastRot;
        private Vector3[] pn;

        private SpiderInputHandler _inputHandler;

        private Vector3[] GetIcoSphereCoords(int depth)
        {
            var res = new Vector3[(int)Mathf.Pow(4, depth) * 12];
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            res[0] = (new Vector3(t, 1, 0));
            res[1] = (new Vector3(-t, -1, 0));
            res[2] = (new Vector3(-1, 0, t));
            res[3] = (new Vector3(0, -t, 1));
            res[4] = (new Vector3(-t, 1, 0));
            res[5] = (new Vector3(1, 0, t));
            res[6] = (new Vector3(-1, 0, -t));
            res[7] = (new Vector3(0, t, -1));
            res[8] = (new Vector3(t, -1, 0));
            res[9] = (new Vector3(1, 0, -t));
            res[10] = (new Vector3(0, t, 1));
            res[11] = (new Vector3(0, -t, -1));

            return res;
        }

        private Vector3[] GetClosestPointIco(Vector3 point, Vector3 up, float halfRange)
        {
            var res = new Vector3[2] { point, up };

            var dirs = GetIcoSphereCoords(0);
            raysNb = dirs.Length;

            var amount = 1f;

            foreach (var dir in dirs)
            {
                var ray = new Ray(point + up * 0.15f, dir);
                //Debug.DrawRay(ray.origin, ray.direction);
                if (!Physics.SphereCast(ray, 0.01f, out var hit, 2f * halfRange)) continue;

                res[0] += hit.point;
                res[1] += hit.normal;
                amount += 1;
            }

            res[0] /= amount;
            res[1] /= amount;
            return res;
        }

        private static Vector3[] GetClosestPoint(Vector3 point, Vector3 forward, Vector3 up, float halfRange,
            float eccentricity, float offset1, float offset2, int rayAmount)
        {
            Vector3[] res = new Vector3[2] { point, up };
            Vector3 right = Vector3.Cross(up, forward);
            float normalAmount = 1f;
            float positionAmount = 1f;

            Vector3[] dirs = new Vector3[rayAmount];
            float angularStep = 2f * Mathf.PI / (float)rayAmount;
            float currentAngle = angularStep / 2f;
            for (int i = 0; i < rayAmount; ++i)
            {
                dirs[i] = -up + (right * Mathf.Cos(currentAngle) + forward * Mathf.Sin(currentAngle)) * eccentricity;
                currentAngle += angularStep;
            }

            foreach (Vector3 dir in dirs)
            {
                RaycastHit hit;
                Vector3 largener = Vector3.ProjectOnPlane(dir, up);
                Ray ray = new Ray(point - (dir + largener) * halfRange + largener.normalized * offset1 / 100f, dir);
                //Debug.DrawRay(ray.origin, ray.direction);
                if (Physics.SphereCast(ray, 0.01f, out hit, 2f * halfRange))
                {
                    res[0] += hit.point;
                    res[1] += hit.normal;
                    normalAmount += 1;
                    positionAmount += 1;
                }

                ray = new Ray(point - (dir + largener) * halfRange + largener.normalized * offset2 / 100f, dir);
                //Debug.DrawRay(ray.origin, ray.direction, Color.green);
                if (Physics.SphereCast(ray, 0.01f, out hit, 2f * halfRange))
                {
                    res[0] += hit.point;
                    res[1] += hit.normal;
                    normalAmount += 1;
                    positionAmount += 1;
                }
            }

            res[0] /= positionAmount;
            res[1] /= normalAmount;
            return res;
        }

        private void Start()
        {
            _inputHandler = GetComponent<SpiderInputHandler>();
            velocity = new Vector3();
            forward = transform.forward;
            upward = transform.up;
            lastRot = transform.rotation;
        }

        private void FixedUpdate()
        {
            velocity = (smoothness * velocity + (transform.position - lastPosition)) / (1f + smoothness);
            if (velocity.magnitude < 0.00025f)
                velocity = lastVelocity;
            lastPosition = transform.position;
            lastVelocity = velocity;

            float multiplier = 1f;
            /*if (Input.GetKey(KeyCode.LeftShift))
                multiplier = 2f;
                */

            float valueY = _inputHandler.Movement.y;
            if (valueY != 0)
                transform.position += transform.forward * (valueY * _speed * multiplier * Time.fixedDeltaTime);
            float valueX = _inputHandler.Movement.x;
            if (valueX != 0)
                transform.position += Vector3.Cross(transform.up, transform.forward) *
                                      (valueX * _speed * multiplier * Time.fixedDeltaTime);

            if (valueX != 0 || valueY != 0)
            {
                pn = GetClosestPoint(transform.position, transform.forward, transform.up, 0.5f, 0.1f, 30, -30, 4);
                //        pn = GetClosestPointIco(transform.position, transform.up, 0.2f);

                upward = pn[1];

                Vector3[] pos = GetClosestPoint(transform.position, transform.forward, transform.up, 0.5f,
                    raysEccentricity, innerRaysOffset, outerRaysOffset, raysNb);
                transform.position = Vector3.Lerp(lastPosition, pos[0], 1f / (1f + smoothness));

                forward = velocity.normalized;
                Quaternion q = Quaternion.LookRotation(forward, upward);
                transform.rotation = Quaternion.Lerp(lastRot, q, 1f / (1f + smoothness));
            }

            lastRot = transform.rotation;
        }
    }
}