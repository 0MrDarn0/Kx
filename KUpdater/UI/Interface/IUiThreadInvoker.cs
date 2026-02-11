// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Interface;

public interface IUiThreadInvoker {
    bool InvokeRequired { get; }
    void BeginInvoke(Delegate d);
}
