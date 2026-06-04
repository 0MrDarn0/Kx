using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.Rendering;

namespace Kx.Tests.TestInfrastructure;

internal sealed class TestElement(IVisualContext context, string id) : UIElement(context, id) {
    protected override void OnDraw(IKxCanvas canvas) {
    }
}
