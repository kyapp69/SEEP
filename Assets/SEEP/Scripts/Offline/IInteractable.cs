using SEEP.Network.Controllers;

namespace SEEP.Offline
{
    public interface IInteractable
    {
        public string GetMessage();
        
        public void Interact(InteractorController interactor);
    }
}