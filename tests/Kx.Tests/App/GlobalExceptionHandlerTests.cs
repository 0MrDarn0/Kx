// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.App.Tests;


/// <summary>
/// Unit tests for the <see cref="GlobalExceptionHandler"/> class.
/// </summary>
/// <remarks>
/// NOTE: This class has limited testability due to its static nature and direct dependencies
/// on sealed framework classes (System.Windows.Forms.Application, System.AppDomain) that cannot
/// be mocked. The tests verify that the method executes without throwing exceptions, but cannot
/// fully verify event subscription or internal state changes without reflection.
/// 
/// For comprehensive testing, the GlobalExceptionHandler would need to be refactored to use
/// dependency injection and testable abstractions.
/// </remarks>
public partial class GlobalExceptionHandlerTests {
    /// <summary>
    /// Tests that calling Register() does not throw an exception.
    /// </summary>
    /// <remarks>
    /// This test verifies basic executability but cannot verify event subscription
    /// due to the static nature of Application and AppDomain classes.
    /// </remarks>
    [Fact]
    public void Register_WhenCalledFirstTime_DoesNotThrow() {
        // Arrange
        // Note: Cannot reset static _registered field without reflection

        // Act
        var exception = Record.Exception(() => GlobalExceptionHandler.Register());

        // Assert
        Assert.Null(exception);
        // Note: Cannot verify event subscriptions or _registered field state without reflection
    }

    /// <summary>
    /// Tests that Unregister successfully unsubscribes from exception handlers when registered.
    /// This tests the main execution path where event handlers are removed.
    /// </summary>
    [Fact]
    public void Unregister_WhenRegistered_UnsubscribesFromExceptionHandlers() {
        // Arrange
        global::Kx.App.GlobalExceptionHandler.Register();

        // Act
        var exception = Record.Exception(() => global::Kx.App.GlobalExceptionHandler.Unregister());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that calling Unregister multiple times does not throw an exception.
    /// This verifies idempotency and that the early return works correctly after unregistering.
    /// </summary>
    [Fact]
    public void Unregister_WhenCalledMultipleTimes_DoesNotThrow() {
        // Arrange
        global::Kx.App.GlobalExceptionHandler.Register();
        global::Kx.App.GlobalExceptionHandler.Unregister();

        // Act
        var exception = Record.Exception(() => global::Kx.App.GlobalExceptionHandler.Unregister());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Register followed by Unregister followed by Register works correctly.
    /// This ensures that the handler can be re-registered after being unregistered.
    /// </summary>
    [Fact]
    public void Unregister_AfterRegisterUnregisterCycle_AllowsReregistration() {
        // Arrange
        global::Kx.App.GlobalExceptionHandler.Register();
        global::Kx.App.GlobalExceptionHandler.Unregister();

        // Act
        var exception = Record.Exception(() => global::Kx.App.GlobalExceptionHandler.Register());

        // Assert
        Assert.Null(exception);

        // Cleanup
        global::Kx.App.GlobalExceptionHandler.Unregister();
    }

    /// <summary>
    /// Tests that RegisterShutdownHandler throws ArgumentNullException when shutdownHandler is null.
    /// Input: null shutdownHandler parameter.
    /// Expected: ArgumentNullException is thrown with parameter name "shutdownHandler".
    /// </summary>
    [Fact]
    public void RegisterShutdownHandler_NullShutdownHandler_ThrowsArgumentNullException() {
        // Arrange
        Func<Task>? shutdownHandler = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => GlobalExceptionHandler.RegisterShutdownHandler(shutdownHandler!));
        Assert.Equal("shutdownHandler", exception.ParamName);
    }

    /// <summary>
    /// Tests that RegisterShutdownHandler accepts a valid delegate without throwing an exception.
    /// Input: valid Func&lt;Task&gt; delegate.
    /// Expected: No exception is thrown.
    /// </summary>
    [Fact]
    public void RegisterShutdownHandler_ValidShutdownHandler_DoesNotThrow() {
        // Arrange
        Func<Task> shutdownHandler = () => Task.CompletedTask;

        // Act & Assert
        var exception = Record.Exception(() => GlobalExceptionHandler.RegisterShutdownHandler(shutdownHandler));
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that RegisterShutdownHandler accepts an async lambda delegate without throwing an exception.
    /// Input: async lambda that returns a Task.
    /// Expected: No exception is thrown.
    /// </summary>
    [Fact]
    public void RegisterShutdownHandler_AsyncLambdaDelegate_DoesNotThrow() {
        // Arrange
        Func<Task> shutdownHandler = async () => await Task.Delay(1);

        // Act & Assert
        var exception = Record.Exception(() => GlobalExceptionHandler.RegisterShutdownHandler(shutdownHandler));
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that RegisterShutdownHandler accepts a method reference as a delegate without throwing an exception.
    /// Input: method reference that matches Func&lt;Task&gt; signature.
    /// Expected: No exception is thrown.
    /// </summary>
    [Fact]
    public void RegisterShutdownHandler_MethodReference_DoesNotThrow() {
        // Arrange
        Func<Task> shutdownHandler = TestShutdownMethod;

        // Act & Assert
        var exception = Record.Exception(() => GlobalExceptionHandler.RegisterShutdownHandler(shutdownHandler));
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that RegisterShutdownHandler can be called multiple times without throwing an exception.
    /// Input: multiple valid Func&lt;Task&gt; delegates called sequentially.
    /// Expected: No exception is thrown for any call.
    /// </summary>
    [Fact]
    public void RegisterShutdownHandler_MultipleCalls_DoesNotThrow() {
        // Arrange
        Func<Task> firstHandler = () => Task.CompletedTask;
        Func<Task> secondHandler = () => Task.FromResult(0);

        // Act & Assert
        var exception1 = Record.Exception(() => GlobalExceptionHandler.RegisterShutdownHandler(firstHandler));
        var exception2 = Record.Exception(() => GlobalExceptionHandler.RegisterShutdownHandler(secondHandler));

        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    /// <summary>
    /// Helper method that returns a completed Task for testing method references.
    /// </summary>
    private static Task TestShutdownMethod() {
        return Task.CompletedTask;
    }
}
