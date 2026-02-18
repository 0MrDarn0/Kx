// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Backend.BackendAbstractions;

public interface IUiThreadInvoker {
    bool InvokeRequired { get; }
    void BeginInvoke(Delegate d);
    void Invoke(Action action);
}
