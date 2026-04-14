# GridSplitter

`GridSplitter` is a built-in framework control that lets users resize adjacent `Grid` columns or rows at runtime with the mouse.

## When to use it

Use `GridSplitter` when a `Grid` should expose a live draggable divider between two content areas, for example:
- changelog and news panels
- navigation and detail panes
- upper and lower editor regions

A concrete app example lives in `apps/KxUpdater/Plugins/KalTheme/Assets/UI/Content/main_content.yaml`.

## Basic behavior

- A vertical splitter resizes neighboring columns.
- A horizontal splitter resizes neighboring rows.
- During dragging, the two affected segments are stored as pixel lengths.
- The splitter uses the existing UI mouse capture flow, so resizing stays active while dragging.

## Recommended layout pattern

The simplest pattern is to place the splitter into its own thin grid column or row.

Vertical example:

```yaml
controls:
  - type: Grid
    id: body
    columns:
      - width:
          value: 1
          unit: Star
      - width:
          value: 12
          unit: Pixel
      - width:
          value: 1
          unit: Star
    rows:
      - height:
          value: 1
          unit: Star
    children:
      - type: Grid
        id: left_panel
        gridRow: 0
        gridColumn: 0
        children: []
      - type: GridSplitter
        id: body_splitter
        gridRow: 0
        gridColumn: 1
        color: "#7C6E4B"
        properties:
          minSize: "180"
          hoverColor: "#A8925A"
          activeColor: "#E8D9B4"
      - type: Grid
        id: right_panel
        gridRow: 0
        gridColumn: 2
        children: []
```

Horizontal example:

```yaml
controls:
  - type: Grid
    id: editor_layout
    columns:
      - width:
          value: 1
          unit: Star
    rows:
      - height:
          value: 1
          unit: Star
      - height:
          value: 10
          unit: Pixel
      - height:
          value: 1
          unit: Star
    children:
      - type: Grid
        id: upper_panel
        gridRow: 0
        gridColumn: 0
        children: []
      - type: GridSplitter
        id: editor_splitter
        gridRow: 1
        gridColumn: 0
        properties:
          orientation: "Horizontal"
          minSize: "120"
      - type: Grid
        id: lower_panel
        gridRow: 2
        gridColumn: 0
        children: []
```

## Supported properties

Set these values through the control `properties` map unless noted otherwise.

- `orientation`
  - `Vertical` by default
  - use `Horizontal` to resize rows instead of columns
- `minSize`
  - minimum size of each affected segment in logical pixels
- `targetColumn`
  - optional explicit left-side column index for vertical resizing
- `targetRow`
  - optional explicit upper row index for horizontal resizing
- `hoverColor`
  - splitter track color while hovered
- `activeColor`
  - splitter track color while dragging
- `gripColor`
  - grip marker color
- `color`
  - regular track color, set on the control itself

## Target selection rules

If no explicit target is provided:
- a vertical splitter uses the column to its left and the column to its right
- a horizontal splitter uses the row above and the row below

If needed, `targetColumn` or `targetRow` can override the inferred neighbor pair.

## Notes

- A splitter works best when the divider column or row already has a small fixed pixel size.
- After the first drag, the affected neighbor sizes are no longer `Star`; they become fixed pixel lengths.
- `minSize` helps prevent panes from collapsing into unusable sizes.
