using Kx.Sdk.UI.State;
using Kx.UI.State;

namespace Kx.Tests;

public sealed class UiStateStoreTests {
    private static readonly UiStateKey<string> TitleState = new("example.title");

    [Fact]
    public void WhenTypedKeyValueIsSetThenTypedLookupReturnsIt() {
        IUiStateStore store = new UiStateStore();
        store.Set(TitleState, "bound title");

        var found = store.TryGet(TitleState, out string? value);

        Assert.True(found);
        Assert.Equal("bound title", value);
    }

    [Fact]
    public void WhenTypedSubscriptionReceivesUpdateThenListenerGetsTypedValue() {
        IUiStateStore store = new UiStateStore();
        string? observed = null;

        using var subscription = store.Subscribe(TitleState, value => observed = value);
        store.Set(TitleState, "updated title");

        Assert.Equal("updated title", observed);
    }

    [Fact]
    public void WhenTypedSubscriptionIsDisposedThenFutureUpdatesAreIgnored() {
        IUiStateStore store = new UiStateStore();
        int notifications = 0;

        var subscription = store.Subscribe(TitleState, _ => notifications++);
        subscription.Dispose();
        store.Set(TitleState, "updated title");

        Assert.Equal(0, notifications);
    }
}
