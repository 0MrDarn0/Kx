using System.Reflection;

using KxUpdater;

namespace KxUpdater.Tests;

public sealed class NewsCoordinatorTests {
    [Fact]
    public void WhenStructuredNewsYamlIsAppliedThenExplicitEntriesAreUsed() {
        var harness = new NewsCoordinatorHarness();
        harness.EnsureNewsSelectionBinding();

        harness.Apply(
            """
            entries:
              - title: "Launcher 1.1"
                content: |
                  Added a dedicated news feed.
                  Improved updater layout.
              - title: "Launcher 1.0"
                content: |
                  Initial release.
            """);

        Assert.Equal(["Launcher 1.1", "Launcher 1.0"], harness.NewsTitles);
        Assert.Equal(0, harness.SelectedIndex);
        Assert.Equal($"Added a dedicated news feed.{Environment.NewLine}Improved updater layout.", harness.ChangelogText);
    }

    [Fact]
    public void WhenLegacyChangelogTextIsAppliedThenExistingParsingStillWorks() {
        var harness = new NewsCoordinatorHarness();
        harness.EnsureNewsSelectionBinding();

        harness.Apply(
            """
            # Latest News
            Added a dedicated news file.

            # Previous News
            Legacy changelog parsing still works.
            """);

        Assert.Equal(["Latest News", "Previous News"], harness.NewsTitles);
        Assert.Equal(0, harness.SelectedIndex);
        Assert.Equal("Added a dedicated news file.", harness.ChangelogText);
    }

    private sealed class NewsCoordinatorHarness : IDisposable {
        private static readonly Type _coordinatorType = typeof(MainWindow).Assembly.GetType("KxUpdater.NewsCoordinator", throwOnError: true)!;
        private readonly object _instance;
        private Action<int>? _selectedIndexChanged;

        public string[] NewsTitles { get; private set; } = [];
        public int SelectedIndex { get; private set; } = -1;
        public string ChangelogText { get; private set; } = string.Empty;

        public NewsCoordinatorHarness() {
            _instance = Activator.CreateInstance(
                _coordinatorType,
                (Func<Action<int>, IDisposable>)SubscribeToSelectedIndex,
                (Action<string[]>)SetNewsTitles,
                (Action<int>)SetSelectedIndex,
                (Action<string>)SetChangelogText) ?? throw new InvalidOperationException("Failed to create NewsCoordinator test instance.");
        }

        public void EnsureNewsSelectionBinding() {
            Invoke("EnsureNewsSelectionBinding");
        }

        public void Apply(string text) {
            Invoke("ApplyChangelogEntries", text);
        }

        public void Dispose() {
            if (_instance is IDisposable disposable)
                disposable.Dispose();
        }

        private IDisposable SubscribeToSelectedIndex(Action<int> callback) {
            ArgumentNullException.ThrowIfNull(callback);
            _selectedIndexChanged = callback;
            return new CallbackDisposable(() => _selectedIndexChanged = null);
        }

        private void SetNewsTitles(string[] titles) {
            ArgumentNullException.ThrowIfNull(titles);
            NewsTitles = titles;
        }

        private void SetSelectedIndex(int selectedIndex) {
            SelectedIndex = selectedIndex;
            _selectedIndexChanged?.Invoke(selectedIndex);
        }

        private void SetChangelogText(string text) {
            ArgumentNullException.ThrowIfNull(text);
            ChangelogText = text;
        }

        private void Invoke(string methodName, params object[] arguments) {
            var method = _coordinatorType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                ?? throw new MissingMethodException(_coordinatorType.FullName, methodName);

            method.Invoke(_instance, arguments);
        }
    }

    private sealed class CallbackDisposable(Action dispose) : IDisposable {
        private readonly Action _dispose = dispose;
        private bool _disposed;

        public void Dispose() {
            if (_disposed)
                return;

            _dispose();
            _disposed = true;
        }
    }
}
