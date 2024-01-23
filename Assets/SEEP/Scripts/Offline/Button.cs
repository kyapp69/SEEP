using UnityEngine;
using UnityEngine.Events;

namespace SEEP.Network.Controllers
{
    public class Button : MonoBehaviour, IInteractable
    {
        [SerializeField] private UnityEvent callback;
        
        public void Interact(InteractorController interactor)
        {
            callback?.Invoke();
        }
    }
}