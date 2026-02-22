namespace __GAME__.Source.Features
{
    public class GameManager : FeatureBase
    {
        private EventBus _bus;
        private GameStateMachine _stateMachine;
        private TimeSystem _timeSystem;

        public override void Start()
        {
            _bus = Main.Get<EventBus>();
            _stateMachine = Main.Get<GameStateMachine>();
            _timeSystem = Main.Get<TimeSystem>();

            _bus.Subscribe<GameStateMachine.GameStateEntered>(OnGameStateEntered);
            _bus.Subscribe<TimeSystem.DayPhaseChanged>(OnDayPhaseChanged);

            _stateMachine.ChangeState(GameStateMachine.GameState.Playing);
        }

        private void OnGameStateEntered(GameStateMachine.GameStateEntered evt)
        {
            if (evt.State == GameStateMachine.GameState.Playing)
            {
                // Можно включить системы, спавн игрока и тд
            }
        }

        private void OnDayPhaseChanged(TimeSystem.DayPhaseChanged evt)
        {
            if (evt.Phase == TimeSystem.DayPhase.Night)
            {
                _bus.Publish(new RaidStarted());
            }
            else
            {
                _bus.Publish(new RaidEnded());
            }
        }
        
        public struct RaidStarted { }
        public struct RaidEnded { }
    }
}