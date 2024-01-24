using SEEP.Network.Controllers;

namespace SEEP.Network
{
    public interface IInteractable
    {
        public string GetMessage();
        
        public void Interact(InteractorController interactor);
    }
}