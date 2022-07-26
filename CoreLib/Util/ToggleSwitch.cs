using System;
using System.Threading;

namespace CoreLib
{
    public class SpecialToggle : ToggleSwitch
    {
        public override IDisposable On()
        {
            CoreLibPlugin.Logger.LogWarning("You said you know what you are doing! If it breaks it breaks!");
            return base.On();
        }
    }
    
    public class ToggleSwitch
    {
        private int onCount;

        public bool Value => onCount > 0;

        public static implicit operator bool(ToggleSwitch toggle)
        {
            return toggle.Value;
        }

        public virtual IDisposable On()
        {
            return new Toggle(this, 1);
        }

        public readonly struct Toggle : IDisposable
        {
            private readonly ToggleSwitch value;
            private readonly int count;

            public Toggle(ToggleSwitch value, int count)
            {
                this.value = value;
                this.count = count;

                Interlocked.Add(ref value.onCount, count);
            }

            public void Dispose()
            {
                Interlocked.Add(ref value.onCount, -count);
            }
        }
    }
}
