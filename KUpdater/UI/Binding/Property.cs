// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Backend;

namespace KUpdater.UI.Binding;

public sealed class Property<T>(IUiThreadInvoker ui, T initialValue = default!, Action? onChanged = null) {
    private readonly IUiThreadInvoker _ui = ui ?? throw new ArgumentNullException(nameof(ui));
    private readonly Action? _onChanged = onChanged;
    private T _value = initialValue!;

    public T Value {
        get => _value;
        set {
            // Wenn Aufruf vom UI-Thread verlangt wird, asynchron ausführen.
            if (_ui.InvokeRequired) {
                _ui.BeginInvoke(new Action(() => {
                    _value = value;
                    _onChanged?.Invoke();
                }));
            } else {
                _value = value;
                _onChanged?.Invoke();
            }
        }
    }

    public static implicit operator T(Property<T> prop) => prop._value;
}
