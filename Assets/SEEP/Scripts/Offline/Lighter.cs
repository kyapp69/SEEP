using System;
using UnityEngine;

namespace SEEP.Offline
{
    public class Lighter : MonoBehaviour
    {
        private Material _material;
        private bool _state;
        
        private void Awake()
        {
            _material = GetComponent<MeshRenderer>().material;
            _state = false;
        }

        public void Switch()
        {
            _state = !_state;
            
            if (_state)
            {
                _material.EnableKeyword("_EMISSION");
            }
            else
            {
                _material.DisableKeyword("_EMISSION");
            }
        }
    }
}