using Kx.Abstractions.UI.Actions;
using Kx.UI.Elements.Panel;
using Kx.Tests.TestInfrastructure;

namespace Kx.Tests;

public sealed class UiTargetResolverTests {
    [Fact]
    public void WhenExpressionIsSelfThenSourceIsResolved() {
        var context = new TestVisualContext();
        var source = new TestElement(context, "source");

        var resolved = UiTargetResolver.TryResolve(source, "self", out var visual);

        Assert.True(resolved);
        Assert.Same(source, visual);
    }

    [Fact]
    public void WhenExpressionIsParentThenParentIsResolved() {
        var context = new TestVisualContext();
        var parent = new StackPanel(context, "parent");
        var child = new TestElement(context, "child");
        parent.AddChild(child);
        context.UIElementManager.Add(parent);

        var resolved = UiTargetResolver.TryResolve(child, "parent", out var visual);

        Assert.True(resolved);
        Assert.Same(parent, visual);
    }

    [Fact]
    public void WhenExpressionIsRootThenRootIsResolved() {
        var context = new TestVisualContext();
        var root = new StackPanel(context, "root");
        var intermediate = new StackPanel(context, "intermediate");
        var child = new TestElement(context, "child");
        root.AddChild(intermediate);
        intermediate.AddChild(child);
        context.UIElementManager.Add(root);

        var resolved = UiTargetResolver.TryResolve(child, "root", out var visual);

        Assert.True(resolved);
        Assert.Same(root, visual);
    }

    [Fact]
    public void WhenExpressionUsesIdSyntaxThenRegisteredVisualIsResolved() {
        var context = new TestVisualContext();
        var root = new StackPanel(context, "root");
        var child = new TestElement(context, "child");
        root.AddChild(child);
        context.UIElementManager.Add(root);

        var resolved = UiTargetResolver.TryResolve(child, "id:child", out var visual);

        Assert.True(resolved);
        Assert.Same(child, visual);
    }
}
