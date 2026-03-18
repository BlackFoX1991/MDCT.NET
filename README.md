# MDCT.NET

MDCT.NET is a Windows Forms Markdown project made up of two parts:

- `MarkdownControlNET`: an editable Markdown control that renders Markdown as a formatted document while keeping the original source editable.
- `MarkdownPad`: a tabbed desktop editor built on top of the control.

The main idea behind the project is not just "render Markdown", but "edit Markdown through a document-like surface without giving up source fidelity". Markdown markers can be hidden in presentation mode, tables can be edited visually, images can be previewed inline, and the underlying Markdown remains the real source of truth.

## Projects

### `MarkdownControlNET`

`MarkdownControlNET` is the reusable control library. It targets `net10.0-windows` and is designed for WinForms applications that need an interactive Markdown editing surface rather than a plain text box or a read-only preview pane.

### `MarkdownPad`

`MarkdownPad` is the reference application and sample host for the control. It demonstrates the intended editing workflow, document navigation, theming, table editing, printing, color formatting, and session handling.

## What Makes `MarkdownControlNET` Different

`MarkdownControlNET` is built as a source-aware Markdown editor control. It renders Markdown blocks and inline constructs visually, but the document is still stored and edited as Markdown text. Cursor movement, selection, undo/redo, copy/paste, link activation, and block operations are all mapped back to source positions.

Key characteristics:

- Editable rendered Markdown, not just a preview.
- Source-preserving behavior with source-to-visual and visual-to-source mapping.
- WinForms-native control API and event model.
- Designed for desktop editors and document-centric workflows.
- Supports both interactive editing and presentation/printing scenarios.

## `MarkdownControlNET` Feature Set

### Block-Level Features

- Paragraphs
- ATX headings (`#` through `######`)
- Block quotes
- Quote admonitions such as `> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`, `> [!WARNING]`, and `> [!CAUTION]`
- Ordered and unordered lists
- Nested lists
- GitHub-style task list items
- Fenced code blocks
- Horizontal rules
- Pipe tables with alignment parsing
- Standalone image blocks
- Footnote definitions

### Inline Features

- Bold
- Italic
- Bold+italic combinations
- Strike-through
- Inline code
- Markdown links
- Bare URL detection
- Inline images
- Footnote references
- Inline foreground color wrappers
- Inline background color wrappers

Color syntax examples:

```md
![FG:#32A852](Green text)
![BG:#5E5A5A](Text with a background)
![FG:#32A852](Nested color: ![BG:#5E5A5A](foreground + background))
```

### Editing and Interaction Features

- Undo and redo
- Cut, copy, paste, and select all
- Inline snippet insertion
- Heading application command
- Quote toggle command
- Wrap-selection-in-code-fence command
- Table insertion command
- Search with find-next support
- Anchor navigation inside Markdown documents
- Link activation events for web links, file links, Markdown file links, and heading/footnote anchors

### Presentation Features

- Hidden Markdown markers in formatted view where appropriate
- Inline image rendering
- Standalone image preview blocks
- Table layout and visual cell rendering
- Quote and admonition styling
- Inline code chip rendering
- Themed rendering for light, dark, and system mode
- Scaled document rendering for printing and presentation output

### Editor/Host Integration

Important public capabilities exposed by the control include:

- `LoadDocument(...)`
- `RenderDocumentPage(...)`
- `DocumentRenderHeight`
- `ThemeMode`
- `AllowAutoThemeChange`
- `CanSideScroll`
- `SuppressEditableRawModes`
- `DocumentBasePath`
- `NavigateToMarkdownAnchor(...)`
- `FindNext()`

Important events include:

- `MarkdownChanged`
- `ThemeChanged`
- `FindRequested`
- `LinkActivated`
- `ViewScaleRequested`

## Table, Raw, and Editing UX

One of the stronger parts of the control is its table and source/presentation workflow.

- Markdown tables are rendered as visual grids.
- Table cells can be edited through a visual grid editor.
- The editor can switch back to raw table source when needed.
- Raw presentation modes are also supported for situations where exact source editing is required.
- Code fences and other source-sensitive regions are handled with editing-aware behavior instead of being treated as static preview output.

This lets the control stay practical for real Markdown authoring, not just display.

## `MarkdownPad` Features

`MarkdownPad` is the desktop application that showcases the control as a complete Markdown editor.

### Core Application Features

- Multi-tab editing
- New/open/save/save as/save all
- Recent files menu
- Close/close others/close all tab workflows
- Drag-and-drop file opening
- Session restore for previously open documents
- Per-tab modified state tracking

### Editing Features

- Uses `MarkdownControlNET` as the main editor surface
- Insert link dialog
- Insert image dialog
- Table designer dialog
- Heading commands
- Quote toggle
- Code fence wrapping
- Foreground color command with color picker
- Background color command with color picker
- Matching color actions in the editor context menu
- Toolbar shortcuts for common authoring actions

### Navigation and Search

- Find dialog
- Find next
- Clickable links
- Markdown document link handling
- Heading anchor navigation
- Footnote-aware navigation through the control

### Visual and Document Features

- Page-like document surface instead of a flat editor canvas
- Adjustable view scale
- Toolbar view-scale slider
- Keyboard zoom shortcuts
- System, light, and dark theme support

### Output Features

- Print
- Print preview
- Scaled rendering for printed pages

## Minimal Usage Example

```csharp
using MarkdownGdi;

var editor = new MarkdownGdiEditor
{
    Dock = DockStyle.Fill,
    ThemeMode = EditorThemeMode.System,
    AllowAutoThemeChange = true
};

editor.LoadDocument(
    "# Hello\n\nThis is **MDCT.NET**.\n\n![FG:#32A852](Colored text)",
    documentBasePath: null,
    resetUndoStacks: true);

editor.LinkActivated += (_, e) =>
{
    Console.WriteLine($"Link clicked: {e.Target}");
};

Controls.Add(editor);
```

## Build

Build the control library:

```powershell
dotnet build .\MarkdownControlNET\MarkdownControlNET.csproj
```

Build the sample editor:

```powershell
dotnet build .\MarkdownPad\MarkdownPad.csproj
```

Run the sample editor:

```powershell
dotnet run --project .\MarkdownPad\MarkdownPad.csproj
```

## Scope and Notes

MDCT.NET is an editor-focused Markdown implementation. The parsing and presentation behavior are intentionally shaped around desktop editing UX, source fidelity, and practical authoring features such as visual tables, source-aware navigation, inline media, and mixed presentation/raw interaction.

It is therefore best understood as an editable Markdown document control and a reference editor, rather than as a generic CommonMark compliance project.
