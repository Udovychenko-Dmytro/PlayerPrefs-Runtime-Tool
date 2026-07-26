# Changelog

All notable changes to the PlayerPrefsRuntime Tool are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [3.1.0] - 2026-07-25

### Added
- **Add Entry dialog** in the runtime viewer: key input, Int/Float/String type selector, value validation and a two-step overwrite guard for existing keys.
- **Delete All** button with confirmation dialog; single-entry Remove now also asks for confirmation.
- **Export / Import**: export all PlayerPrefs to a versioned, typed JSON document (clipboard + file in `persistentDataPath`); import it back via a paste dialog with per-key error reporting.
- **Automatic JSON backup** to `persistentDataPath` before Delete All and before every import. Delete All is cancelled unless its backup is complete; import reports backup failures/partial snapshots and continues.
- **Public mutation API**: `PlayerPrefsRuntime.SetInt/SetFloat/SetString/DeleteKey/DeleteAll/TryDeleteAll/ExportToJson/ImportFromJson`.
- **`PlayerPrefsRuntime.OnEntryChanged` event** — raised after any entry is changed through the tool (`null` key = DeleteAll).
- **`PlayerPrefsRuntime.Version`** constant.
- **Editor Window** (`Tools > PlayerPrefs Runtime Viewer`): view, search, sort, edit, add, delete and export/import PlayerPrefs without Play Mode.
- **WebGL support**: new fetcher parses Unity's binary PlayerPrefs file from the Emscripten `/idbfs` mount — no JavaScript plugin required.
- **Device gesture trigger** (opt-in): hold three fingers for two seconds to open the viewer — `PlayerPrefsRuntime.EnableGestureTrigger()`.
- **Type filter chips** (Int/Float/String) in the viewer header.
- **Tools toggle** in the viewer header (next to the close button): shows/hides the action bar (Add / Delete All / Export / Import) and the type filter bar. Both bars are **hidden by default**, so the header takes two rows instead of four and the entry list gets the screen space back. The button stays highlighted while the bars are shown; the header, separator and scroll area resize with it.
- **Data size indicator**: total prefs size in the header and per-entry size in the detail dialog (WebGL has a 1 MB cap).
- **JSON pretty-print** for JSON string values in the entry dialog; new **Copy Value** button copies the raw value.
- **Demo buttons** (Show Viewer / Log All / Reset Demo Data) so the demo scene is fully usable on devices.
- **EditMode unit tests** covering the JSON normalization helper, the export/import serializer, the Windows registry value decoder, the WebGL UPP parser, the entry value model, the entry-dialog value display, and the PlayerPrefs mutation core (writer).
- **PlayMode smoke tests** covering runtime Canvas creation, refresh, destruction/rebuild, entry-dialog lifecycle, the tools toggle and the header title/subtitle bands.

### Changed
- Entry list is now **virtualized**: a small recycled row pool replaces one GameObject per entry — thousands of entries scroll smoothly.
- Viewer header reworked: second toolbar row (Add / Delete All / Export / Import) and a filter row, both collapsed behind the header "Tools" toggle.
- All PlayerPrefs writes go through a centralized writer core shared by the runtime UI, the editor window and the public API.
- `com.unity.nuget.newtonsoft-json` is now pinned directly in the package manifest.
- Viewer coroutines (search debounce, status messages) run on a dedicated `PlayerPrefsRuntimeCoroutineHost` component on the viewer panel instead of borrowing the panel's `Image` as a coroutine runner.
- All viewer layout values (bar insets, search field and clear button sizes, accent/separator heights, outline distances, fade durations, canvas reference resolution) live in `PlayerPrefsRuntimeViewConstants` instead of as literals in `PlayerPrefsRuntimeViewerBuilder`. The builder no longer carries unreachable "reuse the existing viewer objects" code paths, since it always creates a fresh Canvas.

### Fixed
- Viewer header title no longer draws over the subtitle on narrow windows: the two labels had overlapping anchor bands (title from 45%, subtitle up to 55% of the header width), so a wrapped tool name — or a long status message — spilled into its neighbour. The bands now split the row at 50% and both labels shrink to fit instead of wrapping out of their band.
- Windows registry value decoding extracted into a platform-agnostic, unit-tested decoder.
- Entry dialog no longer breaks on very large values: the displayed value is capped to a safe length (with a "showing first N of M characters" note) so it stays under the legacy UI `Text` vertex limit instead of failing to render, and inline editing is blocked for oversized values. The full value stays available via Copy Value, Export and the public API.
- Search field no longer logs a "GameObject can only contain one Selectable component" error when the viewer opens (a redundant `Button` was being added to the `InputField`'s GameObject).
- WebGL UPP parsing now supports keys whose UTF-8 representation is 128 bytes or longer, and no longer scans another build's `/idbfs` directory as a fallback.
- Plain strings that happen to look like base64 remain unchanged; only the explicit `$base64Binary;` representation is decoded.
- Typed JSON envelopes reject unsupported format versions, while legacy flat imports can use scalar keys named `version` or `prefs`.
- Malformed envelope attempts such as `{"version":1,"prefs":null}` are rejected as a whole instead of importing sibling fields as legacy preferences.
- Automatic backups use unique UTC filenames, so destructive operations in the same second cannot overwrite an earlier snapshot.
- Delete All now requires an explicit completeness signal from the platform fetcher; partial WebGL files, interrupted Windows enumeration, lossy Android values, malformed plist data and dropped keys cancel deletion. A verified empty store is a successful no-op, while the current iOS native bridge remains conservatively read-only for Delete All because it cannot report filtered values.
- Windows numeric `REG_SZ` values remain strings, and the literal key `(Unnamed)` is no longer rejected.
- Runtime UI recovers its row pool after external Canvas destruction and entry dialogs reopen in view mode with current values.
- Runtime dialogs use Unity's version-appropriate built-in font fallback instead of requesting the removed `Arial.ttf` resource on Unity 2022.2+.
- `OnEntryChanged` isolates each subscriber, allowing later handlers to run when an earlier one throws.
- The macOS Editor plist fetcher is excluded from player builds.
- Float/double entry text uses invariant round-trip formatting, so saving an unchanged value does not lose precision.
- Empty/corrupt keys returned by platform storage are skipped instead of being exposed as a writable `(Unnamed)` entry.
- Entry saving tolerates synchronous change handlers that close or rebuild the viewer.
- The tracked `PlayerPrefsRuntimeTool.unitypackage` has been regenerated from the complete v3.1 source tree.

## [3.0.1] - 2025

### Changed
- Reworked new Input System support in the demo (keyboard handling works with legacy, new, or both input backends).

## [3.0.0] - 2025

### Added
- **Edit Mode** in the entry detail dialog: edit Int/Float/String values with validation, save directly to PlayerPrefs, delete entries.
- Callback-based UI refresh after edits.

### Changed
- Updated entry dialog UI with Edit/Save buttons and error messages.

## [2.x and earlier]

- Runtime viewer with search, sorting, type badges and safe-area support.
- Platform fetchers: Android (SharedPreferences), iOS/macOS (native UserDefaults plugins), Windows (registry), Windows/macOS Editor.
