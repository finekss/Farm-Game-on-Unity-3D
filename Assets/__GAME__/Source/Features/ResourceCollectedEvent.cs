namespace __GAME__.Source.Features
{
    public struct ResourceCollectedEvent
    {
        public string Id;
        public int Amount;

        public ResourceCollectedEvent(string id, int amount)
        {
            Id = id;
            Amount = amount;
        }
    }
}