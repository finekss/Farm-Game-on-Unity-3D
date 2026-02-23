namespace __GAME__.Source.Unity.Interaction
{
    public interface IInteractable
    {
        // Отображаемое имя для UI / Debug
        string DisplayName { get; }

        // Можно ли сейчас взаимодействовать
        bool CanInteract { get; }

        // Выполнить взаимодействие
        void Interact();
    }
}