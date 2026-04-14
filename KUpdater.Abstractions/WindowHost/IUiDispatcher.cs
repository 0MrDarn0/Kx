// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.WindowHost;

public interface IUiDispatcher {
    bool InvokeRequired { get; }
    void BeginInvoke(Delegate d);
    void Invoke(Action action);
}
