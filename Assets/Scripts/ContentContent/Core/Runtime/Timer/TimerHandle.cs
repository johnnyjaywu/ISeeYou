using System;

namespace ContentContent
{
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        private readonly Timer timer;
        private readonly uint version;

        internal TimerHandle(Timer timerInstance)
        {
            timer = timerInstance;
            version = timerInstance.Version;
        }

        public bool IsValid => timer != null && timer.Version == version;
        public bool IsRunning => IsValid && timer.IsRunning;
        public float Progress => IsValid ? timer.Progress : 0f;
        public float CurrentTime => IsValid ? timer.CurrentTime : 0f;

        internal Timer InternalTimer => timer;

        public void Pause() { if (IsValid) timer.Pause(); }
        public void Resume() { if (IsValid) timer.Resume(); }
        public void Stop() { if (IsValid) timer.Stop(); }

        public TimerHandle Restart()
        {
            if (IsValid)
            {
                timer.Stop();
                return timer.Start();
            }
            return default;
        }

        public bool Equals(TimerHandle other) => timer == other.timer && version == other.version;
        public override bool Equals(object obj) => obj is TimerHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(timer, version);
        public static bool operator ==(TimerHandle left, TimerHandle right) => left.Equals(right);
        public static bool operator !=(TimerHandle left, TimerHandle right) => !left.Equals(right);
    }
}