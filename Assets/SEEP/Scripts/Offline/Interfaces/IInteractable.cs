using SEEP.Network.Controllers;

namespace SEEP.Offline.Interfaces
{
    public interface IInteractable
    {
        public string GetMessage();
        
        public void Interact(InteractorController interactor);
    }
}