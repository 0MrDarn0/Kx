// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.WindowHost;

namespace Kx.UI.Binding;

public sealed class Property<T>(IUiDispatcher ui, T initialValue = default!, Action? onChanged = null) {
    private readonly IUiDispatcher _ui = ui ?? throw new ArgumentNullException(nameof(ui));
    private readonly Action? _onChanged = onChanged;
    private T _value = initialValue!;

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
