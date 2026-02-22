using UnityEngine;

namespace __GAME__.Source.Features
{
    public class TimeSystem : FeatureBase
    {
        public enum DayPhase
        {
            Day,
            Night
        }
        public float DayLength = 600f; // 10 минут
        public float NightLength = 180f;

        public float CurrentTime { get; private set; }
        public DayPhase CurrentPhase { get; private set; } = DayPhase.Day;

        private EventBus _bus;

        public override void Start()
        {
            _bus = Main.Get<EventBus>();
            CurrentTime = 0f;
        }

        public override void Tick(float dt)
        {
            CurrentTime += dt;

            float limit = CurrentPhase == DayPhase.Day ? DayLength : NightLength;

            if (CurrentTime >= limit)
            {
                SwitchPhase();
            }
        }

        private void SwitchPhase()
        {
            CurrentTime = 0f;

            CurrentPhase = CurrentPhase == DayPhase.Day
                ? DayPhase.Night
                : DayPhase.Day;

            _bus.Publish(new DayPhaseChanged { Phase = CurrentPhase });
        }
        
        public struct DayPhaseChanged
        {
            public DayPhase Phase;
        }
    }
}