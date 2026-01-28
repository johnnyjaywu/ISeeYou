using System.Collections.Generic;
using UnityEngine;

namespace ContentContent
{
    public abstract partial class Timer
    {
        private static readonly Stack<Timer> pool = new(32);

        internal static Timer Get(float val, TimerMode m, MonoBehaviour owner, bool unscaled, bool repeat, float interval)
        {
            Timer t = pool.Count > 0 ? pool.Pop() : new ConcreteTimer(val);
            t.Configure(val, m, unscaled, repeat, interval);
            t.SetOwner(owner);
            return t;
        }

        public static TimerHandle Countdown(float duration, MonoBehaviour owner, bool unscaled = false)
        {
            return Get(duration, TimerMode.Countdown, owner, unscaled, false, 0).Start();
        }

        public static TimerHandle Stopwatch(MonoBehaviour owner, bool unscaled = false)
        {
            return Get(0, TimerMode.Stopwatch, owner, unscaled, false, 0).Start();
        }

        static partial void Recycle(Timer timer)
        {
            if (!pool.Contains(timer)) pool.Push(timer);
        }

        public static void WarmPool(int count)
        {
            for (int i = 0; i < count; i++) pool.Push(new ConcreteTimer(0));
        }
    }
}