// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.Events;

/// <summary>
/// Represents an asynchronous operation without parameters.
/// Equivalent to <see cref="Func{Task}"/>.
/// </summary>
public delegate Task AsyncAction();

/// <summary>
/// Represents an asynchronous operation with one parameter.
/// Equivalent to <see cref="Func{T, Task}"/>.
/// </summary>
/// <typeparam name="T">The type of the input parameter.</typeparam>
public delegate Task AsyncAction<in T>(T arg);

/// <summary>
/// Represents an asynchronous operation with two parameters.
/// Equivalent to <see cref="Func{T1, T2, Task}"/>.
/// </summary>
/// <typeparam name="T1">The type of the first input parameter.</typeparam>
/// <typeparam name="T2">The type of the second input parameter.</typeparam>
public delegate Task AsyncAction<in T1, in T2>(T1 arg1, T2 arg2);

/// <summary>
/// Represents an asynchronous operation with three parameters.
/// Equivalent to <see cref="Func{T1, T2, T3, Task}"/>.
/// </summary>
/// <typeparam name="T1">The type of the first input parameter.</typeparam>
/// <typeparam name="T2">The type of the second input parameter.</typeparam>
/// <typeparam name="T3">The type of the third input parameter.</typeparam>
public delegate Task AsyncAction<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3);
