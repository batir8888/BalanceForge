# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BalanceForge is a Unity Editor plugin for managing tabular game balance data (character stats, item properties, etc.). It provides a custom editor window with undo/redo, filtering, sorting, validation, and CSV import/export. All source lives under `Assets/BalanceForge/`.

Unity version: **6000.3.12f1**

## Running Tests

Tests use NUnit via Unity's Test Framework and run in **Edit Mode** only.

- **In Unity Editor**: Window > General > Test Runner > Edit Mode > Run All
- **CLI batch mode**:
  ```bash
  unity -projectPath . -runTests -testPlatform editmode -batchmode -quit
  ```
- Test file: `Assets/BalanceForge/Tests/Editor/BalanceForgeTests.cs`

There is no build script — open the project in Unity to build or play.

## Architecture

The plugin follows a layered design with clear separation between data, services, and UI:

### Data Layer (`Core/Data/`)

- **`BalanceTable.cs`** — ScriptableObject that is the root asset. Owns a list of `ColumnDefinition`s and `BalanceRow`s. Validates all rows on demand and tracks `lastModified`.
- **`BalanceRow.cs`** — One row of data. Uses type-aware serialization: values are stored in separate typed lists (`stringValues`, `intValues`, etc.) keyed by column ID. Implements `ISerializationCallbackReceiver` to pack/unpack a `Dictionary<string, object>` at serialize/deserialize time.
- **`ColumnDefinition.cs`** — Schema for a column: `ColumnType`, display name, default value, required flag, and an optional `IValidator`.
- **`ColumnType.cs`** — Enum: `String`, `Integer`, `Float`, `Boolean`, `Enum`, `AssetReference`, `Color`, `Vector2`, `Vector3`.
- **`Operations/Filtering.cs`** — `IFilter` interface, `FilterCondition`, and `FilterOperator` (Equals, NotEquals, GreaterThan, LessThan, Contains, StartsWith, EndsWith).
- **`Operations/TableSorter.cs`** — Sort rows by any column ascending/descending.

### Services Layer (`Services/`)

- **`UndoRedoService.cs`** — Command pattern: maintains undo/redo stacks; cleared when a table is loaded.
- **`Commands.cs`** — `ICommand` interface plus concrete implementations: `AddRowCommand`, `RemoveRowCommand`, `SetCellValueCommand`, etc.
- **`ClipboardService.cs`** — Copy/paste cell values; integrates with Ctrl+C/V hotkeys.
- **`IValidator.cs` / `Validators.cs`** — Pluggable validation; built-in validators for range and regex constraints.

### Editor UI Layer (`Editor/UI/`)

- **`BalanceTableEditorWindow.cs`** — Main window (Window > BalanceForge > Table Editor). Handles virtual scrolling, cell selection, inline editing, filtering panel, sorting, and the validation errors panel. Hotkeys: Ctrl+Z/Y (undo/redo), Delete, Ctrl+C/V.
- **`Windows/CreateTableWizard.cs`** — Wizard dialog for creating a new `BalanceTable` asset.

### Import/Export (`ImportExport/`)

- **`CSVExporter.cs`** — Serializes a `BalanceTable` to CSV using `DisplayName` as headers.
- **`CSVImporter.cs`** — Parses CSV, auto-detects column types, and validates structural compatibility before importing.

### Design Patterns

| Pattern | Where |
|---|---|
| Command | `ICommand` / `UndoRedoService` / `Commands.cs` |
| Strategy | `IFilter`, `IValidator` |
| ScriptableObject asset | `BalanceTable` |
| Type-aware serialization | `BalanceRow` (pack/unpack typed dictionaries) |

## Key Constraints

- All editor code must be inside `#if UNITY_EDITOR` guards (or placed under an `Editor/` folder asmdef) — this is an editor-only tool and must not ship in player builds.
- `BalanceRow` serialization is sensitive: the typed storage lists and the `Dictionary<string, object>` runtime cache must stay in sync. Always go through `SetValue`/`GetValue` rather than directly manipulating the typed lists.
- The `ICommand` pattern is the only correct way to mutate table data from the UI — direct mutations bypass undo/redo.
