using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Elements;

using SkiaSharp;

namespace Kx.Tests.TestInfrastructure;

internal sealed class TestElement(IVisualContext context, string id) : UIElement(context, id) {
    protected override void OnDraw(SKCanvas canvas) {
    }
}
