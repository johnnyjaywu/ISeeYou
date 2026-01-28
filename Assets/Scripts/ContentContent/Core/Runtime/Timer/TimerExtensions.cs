using System;

namespace ContentContent
{
    public static class TimerExtensions
    {
        public static TimerHandle OnFinish(this TimerHandle handle, Action callback)
        {
            if (handle.IsValid) handle.InternalTimer.RegisterOnFinish(callback);
            return handle;
        }

        public static TimerHandle OnCancel(this TimerHandle handle, Action callback)
        {
            if (handle.IsValid) handle.InternalTimer.RegisterOnCancel(callback);
            return handle;
        }

        public static TimerHandle OnInterval(this TimerHandle handle, float interval, Action callback)
        {
            if (handle.IsValid)
            {
                handle.InternalTimer.SetInterval(interval);
                handle.InternalTimer.RegisterOnInterval(callback);
            }
            return handle;
        }

        public static TimerHandle WithJitter(this TimerHandle handle, float amount)
        {
            if (handle.IsValid) handle.InternalTimer.SetJitter(amount);
            return handle;
        }
    }
}