// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.UI.Interface;

namespace KUpdater.UI.Control;

public sealed class Property<T>(IUiThreadInvoker ui, T initialValue = default!, Action? onChanged = null) {
    private readonly IUiThreadInvoker _ui = ui;
    private readonly Action? _onChanged = onChanged;
    private T _value = initialValue;

    public T Value {
        get => _value;
        set {
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
