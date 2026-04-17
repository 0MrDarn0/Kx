// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Binding;

/// <summary>
/// Represents a bindable property that marshals changes to the UI thread.
/// </summary>
/// <typeparam name="T">Type of the stored value.</typeparam>
public interface IProperty<T> {
    /// <summary>
    /// Gets or sets the value. Setting must marshal to the UI thread when required.
    /// </summary>
    T Value { get; set; }
}
