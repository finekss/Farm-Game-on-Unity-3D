namespace __GAME__.Source.Features
{
    public class GameStateMachine : FeatureBase
    {   
        public enum GameState
        {
            None,
            MainMenu,
            Loading,
            Playing,
            Paused,
            GameOver
        }
        public GameState CurrentState { get; private set; } = GameState.None;

        private EventBus _bus;

        public override void Start()
        {
            _bus = Main.Get<EventBus>();
            ChangeState(GameState.Loading);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            Exit(CurrentState);

            CurrentState = newState;

            Enter(CurrentState);
        }

        private void Enter(GameState state)
        {
            _bus.Publish(new GameStateEntered { State = state });
        }

        private void Exit(GameState state)
        {
            _bus.Publish(new GameStateExited { State = state });
        }
        
        public struct GameStateEntered
        {
            public GameState State;
        }

        public struct GameStateExited
        {
            public GameState State;
        }
    }
}