using UnityEngine;

namespace SEEP.Offline.Interactables
{
    public class OfflineLamp : MonoBehaviour
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
            _material.color = _state ? Color.yellow : Color.black;
        }
    }
}