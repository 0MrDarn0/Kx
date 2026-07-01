// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Markup;

using Label = Kx.UI.Elements.Label;
using TextBox = Kx.UI.Elements.TextBox;

namespace KxEdit;

internal sealed class PropertyPanelBinder {
    private readonly Label? _selectionLabel;
    private readonly Label? _field1Label;
    private readonly Label? _field2Label;
    private readonly Label? _field3Label;
    private readonly Label? _field4Label;
    private readonly Label? _field5Label;
    private readonly Label? _field6Label;
    private readonly Label? _boundsLabel;
    private readonly Label? _propertiesLabel;

    private readonly TextBox? _field1Input;
    private readonly TextBox? _field2Input;
    private readonly TextBox? _field3Input;
    private readonly TextBox? _field4Input;
    private readonly TextBox? _field5Input;
    private readonly TextBox? _field6Input;
    private readonly TextBox? _boundsXInput;
    private readonly TextBox? _boundsYInput;
    private readonly TextBox? _boundsWidthInput;
    private readonly TextBox? _boundsHeightInput;
    private readonly TextBox? _propertiesInput;

    public PropertyPanelBinder(
        Label? selectionLabel,
        Label? field1Label,
        Label? field2Label,
        Label? field3Label,
        Label? field4Label,
        Label? field5Label,
        Label? field6Label,
        Label? boundsLabel,
        Label? propertiesLabel,
        TextBox? field1Input,
        TextBox? field2Input,
        TextBox? field3Input,
        TextBox? field4Input,
        TextBox? field5Input,
        TextBox? field6Input,
        TextBox? boundsXInput,
        TextBox? boundsYInput,
        TextBox? boundsWidthInput,
        TextBox? boundsHeightInput,
        TextBox? propertiesInput) {
        _selectionLabel = selectionLabel;
        _field1Label = field1Label;
        _field2Label = field2Label;
        _field3Label = field3Label;
        _field4Label = field4Label;
        _field5Label = field5Label;
        _field6Label = field6Label;
        _boundsLabel = boundsLabel;
        _propertiesLabel = propertiesLabel;
        _field1Input = field1Input;
        _field2Input = field2Input;
        _field3Input = field3Input;
        _field4Input = field4Input;
        _field5Input = field5Input;
        _field6Input = field6Input;
        _boundsXInput = boundsXInput;
        _boundsYInput = boundsYInput;
        _boundsWidthInput = boundsWidthInput;
        _boundsHeightInput = boundsHeightInput;
        _propertiesInput = propertiesInput;
    }

    public void Apply(MarkupEditorDocument document, MarkupEditorControl? selectedControl, bool frameSelected) {
        if (frameSelected) {
            document.FrameDefault.Title = _field1Input?.Text ?? document.FrameDefault.Title;
            document.FrameDefinition = Normalize(_field2Input?.Text);
            document.FrameDefault.BackgroundColor = _field3Input?.Text ?? document.FrameDefault.BackgroundColor;
            document.FrameDefault.TitleBarColor = _field4Input?.Text ?? document.FrameDefault.TitleBarColor;
            document.FrameDefault.BorderColor = _field5Input?.Text ?? document.FrameDefault.BorderColor;
            document.FrameDefault.SeparatorColor = _field5Input?.Text ?? document.FrameDefault.SeparatorColor;
            document.FrameDefault.TitleColor = Normalize(_field6Input?.Text) ?? document.FrameDefault.TitleColor;
            return;
        }

        if (selectedControl is null)
            return;

        selectedControl.Type = Fallback(_field1Input?.Text, selectedControl.Type);
        selectedControl.Id = Fallback(_field2Input?.Text, selectedControl.Id);
        selectedControl.Text = Normalize(_field3Input?.Text);
        selectedControl.Layer = Fallback(_field4Input?.Text, selectedControl.Layer);
        selectedControl.Color = Normalize(_field5Input?.Text);
        selectedControl.OnClick = Normalize(_field6Input?.Text);
        selectedControl.Bounds.X = ParseInt(_boundsXInput?.Text, selectedControl.Bounds.X);
        selectedControl.Bounds.Y = ParseInt(_boundsYInput?.Text, selectedControl.Bounds.Y);
        selectedControl.Bounds.Width = Math.Max(20, ParseInt(_boundsWidthInput?.Text, selectedControl.Bounds.Width));
        selectedControl.Bounds.Height = Math.Max(20, ParseInt(_boundsHeightInput?.Text, selectedControl.Bounds.Height));
        selectedControl.Properties = ParseProperties(_propertiesInput?.Text ?? string.Empty);
    }

    public void Refresh(MarkupEditorDocument document, MarkupEditorControl? selectedControl, bool frameSelected) {
        if (_selectionLabel is null)
            return;

        if (frameSelected) {
            _selectionLabel.Text.Value = "Auswahl: Frame";
            SetField(_field1Label, _field1Input, "Titel", document.FrameDefault.Title);
            SetField(_field2Label, _field2Input, "FrameDefinition", document.FrameDefinition ?? string.Empty);
            SetField(_field3Label, _field3Input, "Background", document.FrameDefault.BackgroundColor);
            SetField(_field4Label, _field4Input, "TitleBar", document.FrameDefault.TitleBarColor);
            SetField(_field5Label, _field5Input, "Border", document.FrameDefault.BorderColor);
            SetField(_field6Label, _field6Input, "TitleColor", document.FrameDefault.TitleColor);
            SetField(_boundsLabel, null, "Bounds", string.Empty);
            SetBoundsInputs(string.Empty, string.Empty, string.Empty, string.Empty);
            SetField(_propertiesLabel, _propertiesInput, "Properties", string.Empty);
            return;
        }

        if (selectedControl is null) {
            _selectionLabel.Text.Value = "Auswahl: -";
            return;
        }

        _selectionLabel.Text.Value = $"Auswahl: {selectedControl.Id} [{selectedControl.Type}]";
        SetField(_field1Label, _field1Input, "Type", selectedControl.Type);
        SetField(_field2Label, _field2Input, "Id", selectedControl.Id);
        SetField(_field3Label, _field3Input, "Text", selectedControl.Text ?? string.Empty);
        SetField(_field4Label, _field4Input, "Layer", selectedControl.Layer);
        SetField(_field5Label, _field5Input, "Color", selectedControl.Color ?? string.Empty);
        SetField(_field6Label, _field6Input, "OnClick", selectedControl.OnClick ?? string.Empty);
        SetField(_boundsLabel, null, "Bounds X / Y / W / H", string.Empty);
        SetBoundsInputs(
            selectedControl.Bounds.X.ToString(),
            selectedControl.Bounds.Y.ToString(),
            selectedControl.Bounds.Width.ToString(),
            selectedControl.Bounds.Height.ToString());
        SetField(_propertiesLabel, _propertiesInput, "Properties", SerializeProperties(selectedControl.Properties));
    }

    private static void SetField(Label? label, TextBox? input, string caption, string value) {
        if (label is not null)
            label.Text.Value = caption;
        if (input is not null)
            input.Text = value;
    }

    private void SetBoundsInputs(string x, string y, string width, string height) {
        if (_boundsXInput is not null)
            _boundsXInput.Text = x;
        if (_boundsYInput is not null)
            _boundsYInput.Text = y;
        if (_boundsWidthInput is not null)
            _boundsWidthInput.Text = width;
        if (_boundsHeightInput is not null)
            _boundsHeightInput.Text = height;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Fallback(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static int ParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;

    private static Dictionary<string, string> ParseProperties(string text) {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (key.Length == 0)
                continue;

            result[key] = value;
        }

        return result;
    }

    private static string SerializeProperties(IReadOnlyDictionary<string, string> properties) {
        return string.Join(Environment.NewLine, properties.Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
