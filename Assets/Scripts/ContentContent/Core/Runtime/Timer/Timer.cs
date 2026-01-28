using System;
using UnityEngine;

namespace ContentContent
{
    /// <summary>
    /// The internal execution engine. Handles timing logic and delta-time application.
    /// This class is abstract to enforce factory usage via TimerHandle.
    /// </summary>
    public abstract partial class Timer : IDisposable
    {
        internal enum TimerMode { Countdown, Stopwatch }

        private bool disposed;
        private MonoBehaviour owner;
        private bool hasOwner;

        private TimerMode mode;
        private bool useUnscaledTime;
        private bool isRepeating;

        protected float initialTime;
        protected float intervalTime;
        private float intervalAccumulator;

        private float jitterAmount;
        private float currentInterval;

        private event Action onStart;
        private event Action onCancel;
        private event Action onFinish;
        private event Action onInterval;

        /// <summary>
        /// A version ID that increments on every reuse. 
        /// Ensures TimerHandles cannot control a recycled timer.
        /// </summary>
        internal uint Version { get; private set; }
        public float CurrentTime { get; private set; }
        public bool IsRunning { get; private set; }

        public float Progress => mode == TimerMode.Countdown ? Mathf.Clamp01(CurrentTime / initialTime) : 0;

        internal Timer(float value)
        {
            initialTime = value;
        }

        // --- Event Registration ---

        internal void RegisterOnStart(Action cb) => onStart += cb;
        internal void RegisterOnCancel(Action cb) => onCancel += cb;
        internal void RegisterOnFinish(Action cb) => onFinish += cb;
        internal void RegisterOnInterval(Action cb) => onInterval += cb;

        public void SetOwner(MonoBehaviour timerOwner)
        {
            owner = timerOwner;
            hasOwner = timerOwner != null;
        }

        public void SetInterval(float interval)
        {
            intervalTime = interval;
            UpdateJitteredInterval();
        }

        public void SetJitter(float amount)
        {
            jitterAmount = amount;
            UpdateJitteredInterval();
        }

        private void UpdateJitteredInterval()
        {
            currentInterval = intervalTime + UnityEngine.Random.Range(-jitterAmount, jitterAmount);
            currentInterval = Mathf.Max(0.01f, currentInterval);
        }

        // --- Logic Control ---

        internal TimerHandle Start()
        {
            CurrentTime = mode == TimerMode.Countdown ? initialTime : 0;
            intervalAccumulator = 0;

            if (!IsRunning)
            {
                IsRunning = true;
                TimerManager.RegisterTimer(this);
                onStart?.Invoke();
            }

            return new TimerHandle(this);
        }

        public void Pause() => IsRunning = false;
        public void Resume() => IsRunning = true;

        public void Stop()
        {
            if (IsRunning || !disposed)
            {
                IsRunning = false;
                TimerManager.DeregisterTimer(this);
                onCancel?.Invoke();
            }
        }

        public void Tick()
        {
            // If the owner was destroyed OR the owner was disabled/deactivated...
            if (hasOwner && (owner == null || !owner.gameObject.activeInHierarchy))
            {
                Dispose();
                return;
            }

            if (!IsRunning) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (mode == TimerMode.Countdown)
            {
                CurrentTime -= dt;
                if (CurrentTime <= 0)
                {
                    if (isRepeating)
                    {
                        while (CurrentTime <= 0)
                        {
                            CurrentTime += initialTime;
                            onInterval?.Invoke();
                        }
                    }
                    else
                    {
                        IsRunning = false;
                        TimerManager.DeregisterTimer(this);
                        onFinish?.Invoke();
                    }
                }
            }
            else
            {
                CurrentTime += dt;
            }

            if (intervalTime > 0)
            {
                intervalAccumulator += dt;
                while (intervalAccumulator >= currentInterval)
                {
                    intervalAccumulator -= currentInterval;
                    onInterval?.Invoke();

                    if (jitterAmount > 0) UpdateJitteredInterval();
                }
            }
        }

        internal void Configure(float val, TimerMode m, bool unscaled, bool repeat, float interval)
        {
            unchecked { Version++; }
            initialTime = val;
            CurrentTime = m == TimerMode.Countdown ? val : 0;
            mode = m;
            useUnscaledTime = unscaled;
            isRepeating = repeat;
            intervalTime = interval;
            currentInterval = interval;
            jitterAmount = 0;

            IsRunning = false;
            disposed = false;
            intervalAccumulator = 0;

            onStart = onCancel = onFinish = onInterval = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Stop();
            Recycle(this);
        }

        static partial void Recycle(Timer timer);

        private sealed class ConcreteTimer : Timer
        {
            public ConcreteTimer(float value) : base(value) { }
        }
    }
}