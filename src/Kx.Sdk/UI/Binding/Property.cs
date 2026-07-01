// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.WindowHost;

namespace Kx.Sdk.UI.Binding;

// <summary>
/// Wraps a value with UI-thread-aware change notifications so consumers can rely on UI-thread affinity for updates and callbacks.
/// </summary>
/// <param name="ui">Dispatcher used to marshal updates to the UI thread.</param>
/// <param name="initialValue">Initial value for the property.</param>
/// <param name="onChanged">Optional callback invoked after the value is set (always executed on the UI thread).</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="ui"/> is null.</exception>
public sealed class Property<T>(IUiDispatcher ui, T initialValue = default!, Action? onChanged = null) : IProperty<T> {
    private readonly IUiDispatcher _ui = ui ?? throw new ArgumentNullException(nameof(ui));
    private readonly Action? _onChanged = onChanged;
    private T _value = initialValue!;

    /// <summary>
    /// The current value. Mutations are marshaled to the UI thread when required so that consumers with UI-thread affinity observe consistent behavior.
    /// </summary>
    public T Value {
        get => _value;
        set {
            if (_ui.InvokeRequired) {
                _ui.BeginInvoke(new Action(() => {
                    _value = value;
                    _onChanged?.Invoke();
                }));
            }
            else {
                _value = value;
                _onChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Allows implicit conversion to the underlying value to simplify usage where a <typeparamref name="T"/> is expected.
    /// </summary>
    public static implicit operator T(Property<T> prop) => prop._value;
}
