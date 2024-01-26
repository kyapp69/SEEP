using SEEP.Network.Controllers;
using SEEP.Offline.Controllers;
using UnityEngine;
using UnityEngine.Events;

namespace SEEP.Offline.Interactables
{
    public class OfflineButton : OfflineInteractable
    {
        [SerializeField] private UnityEvent callback;

        public override void Interact(InteractorController interactor)
        {
            callback?.Invoke();
        }
    }
}