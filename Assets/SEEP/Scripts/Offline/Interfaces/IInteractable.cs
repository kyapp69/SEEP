using SEEP.Network.Controllers;

namespace SEEP.Offline.Interfaces
{
    public interface IInteractable
    {
        public InteractableType GetInteractableType();
        
        public string GetMessage();
        
        public void Interact(InteractorController interactor);
    }

    public enum InteractableType
    {
        Object,
        Zone
    }
}