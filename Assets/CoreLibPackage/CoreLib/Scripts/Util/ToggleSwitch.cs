using System;
using System.Threading;

namespace CoreLib.Util
{
    /// <summary>
    /// Manages a toggle state using an internal counter. Provides methods to activate
    /// temporary states and handles state transitions based on the counter value.
    /// </summary>
    /// <remarks>
    /// When a toggle is activated using the <see cref="On"/> method, the internal counter
    /// is incremented, and the state is considered "on" if the counter is greater than zero.
    /// The state automatically reverts once the associated toggle object is disposed.
    /// </remarks>
    public class ToggleSwitch
    {
        /// <summary>
        /// Tracks the active state count of the toggle switch. The count increases when the switch is activated
        /// and decreases when deactivated. A positive value indicates that the switch is in an "on" state.
        /// </summary>
        private int onCount;

        /// <summary>
        /// Gets a value indicating whether the toggle switch is currently active.
        /// </summary>
        /// <value>
        /// <c>true</c> if the toggle switch is active (i.e., the internal count is greater than zero); otherwise, <c>false</c>.
        /// </value>
        public bool Value => onCount > 0;

        /// <summary>
        /// Allows the definition of custom behavior for the specified operator by overriding
        /// its default functionality, enabling specific use purposes, such as comparisons,
        /// arithmetic operations, or conversions, in the associated type.
        /// </summary>
        public static implicit operator bool(ToggleSwitch toggle)
        {
            return toggle;
        }

        /// <summary>
        /// Activates a temporary state in the associated <see cref="ToggleSwitch"/> by incrementing
        /// its internal counter. The returned disposable object controls the lifespan of this increment,
        /// and when disposed, the counter is decremented to revert the state.
        /// </summary>
        /// <returns>
        /// A disposable instance of <see cref="ToggleSwitch.Toggle"/> that manages the lifecycle
        /// of the incremented state.
        /// </returns>
        public virtual IDisposable On()
        {
            return new Toggle(this, 1);
        }

        /// <summary>
        /// Represents a disposable toggle mechanism that increments or decrements
        /// a count in a containing <see cref="ToggleSwitch"/>. When the toggle
        /// is instantiated, the count in the associated <see cref="ToggleSwitch"/>
        /// is incremented by a specified value, and when it is disposed, the count
        /// is decremented by the same value.
        /// </summary>
        /// <remarks>
        /// This struct is primarily used to manage the state of a toggle switch in
        /// a safe and controlled way, ensuring that the state is properly decremented
        /// when the toggle is disposed.
        /// </remarks>
        public readonly struct Toggle : IDisposable
        {
            /// <summary>
            /// Gets a value indicating whether the toggle switch is currently "on".
            /// </summary>
            /// <value>
            /// <c>true</c> if the toggle switch is "on" (i.e., the internal count is greater than zero); otherwise, <c>false</c>.
            /// </value>
            private readonly ToggleSwitch value;

            /// <summary>
            /// Represents the increment value used to modify the state of a <see cref="ToggleSwitch"/>
            /// when a <see cref="ToggleSwitch.Toggle"/> instance is created or disposed.
            /// </summary>
            /// <remarks>
            /// The <c>count</c> value is added to the internal state of a <see cref="ToggleSwitch"/>
            /// when a <see cref="ToggleSwitch.Toggle"/> is instantiated, and subtracted
            /// when it is disposed. This ensures safe state transitions managed via the toggle mechanism.
            /// </remarks>
            private readonly int count;

            /// <summary>
            /// Represents a disposable toggle mechanism that modifies the state of
            /// a containing <see cref="ToggleSwitch"/> by incrementing or decrementing
            /// an internal counter. When the <see cref="Toggle"/> is created, the value is
            /// incremented, and when disposed, the value is decremented.
            /// </summary>
            /// <remarks>
            /// This struct provides a controlled approach for managing the state transitions
            /// in a <see cref="ToggleSwitch"/>. Use this to ensure the internal state is properly
            /// incremented and decremented in a thread-safe manner.
            /// </remarks>
            public Toggle(ToggleSwitch value, int count)
            {
                this.value = value;
                this.count = count;

                Interlocked.Add(ref value.onCount, count);
            }

            /// <summary>
            /// Releases resources used by the current instance of <see cref="Toggle"/> and
            /// modifies the state of the associated <see cref="ToggleSwitch"/> by decrementing
            /// its internal count.
            /// </summary>
            /// <remarks>
            /// This method ensures that the internal count of the <see cref="ToggleSwitch"/> is
            /// properly decremented when the <see cref="Toggle"/> is disposed. This helps maintain
            /// the consistency of the toggle switch's state, particularly in concurrent or multi-threaded
            /// environments.
            /// </remarks>
            public void Dispose()
            {
                Interlocked.Add(ref value.onCount, -count);
            }
        }
    }
}