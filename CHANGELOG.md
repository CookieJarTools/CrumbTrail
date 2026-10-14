# Changelog

All notable changes to this package are written here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this package follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-10-14

### Fixed

- **Enter still took two presses**, because 1.2.0 fixed the symptom rather than the cause. The
  field is delayed now, which means it reports what was typed exactly when the text is committed,
  and committing is what Enter does. There is no longer any ordering to get wrong: the task is
  written down because the field said so, not because a key was seen.

### Added

- **A Create button beside the field**, for anyone who would rather press it than trust Enter. It
  reads the live text rather than the committed value, so typing and clicking it straight away
  writes down what is actually on screen.

## [1.2.0] - 2026-10-14

### Added

- **Every task opens into a note, written in Markdown.** Headings, bold, italic, bullets, quotes,
  inline code and fenced code all draw. The note shows as drawn text and turns into the raw
  Markdown when you press Edit, because a note is read far more often than it is written. The
  chevron stays lit on a task that carries one, so a note is never hidden.
- **`[[Page Name]]` in a note links to a CookBook page** and opens it when clicked. CrumbTrail does
  not depend on the CookBook: the part that knows about it is a separate assembly that only builds
  when the CookBook is installed, in the same way LayerCake does it. Without it, the link says what
  is missing rather than doing nothing.

### Fixed

- **Enter took two presses to write a task down.** The field syncs what was typed into its value as
  part of handling Return, so reading it inside the key callback saw whatever was there beforehand,
  which on the first press was nothing. The first press only committed the text and the second one
  found it. It reads a tick later now.

## [1.1.0] - 2026-10-14

### Fixed

- **Nothing you typed was ever saved.** Tasks were written in eight places and only four of them
  told Unity anything had changed, and none of those four were the ones that matter: ticking a
  task off, typing its text, setting its priority and adding a task all changed memory only. The
  asset was never marked dirty, so Unity did not even offer to save on quit, and the work went
  without a word. Everything that changes a list or a task now goes through one method that writes
  the file, folded into a single write per editor tick.
- **The database no longer appears in the root of your project.** It lived at
  `Assets/ToDoListDatabase.asset` in whatever project installed the package. It is now
  `ProjectSettings/CrumbTrail.asset`, which travels with the project and stays out of the way.
- **`TaskPriority` no longer carries an `All` member.** It was there so the filter dropdown could
  reuse the enum, which meant a task could be saved with a priority that means nothing.

### Added

- **One field at the top where Enter writes a task down.** The gap between noticing something and
  getting it written is where a list stops being used, so there is nothing in that gap.
- **A task can point at a GameObject, prefab, material or script.** It wears the name from then
  on, and clicking the badge pings it. This is the part a note elsewhere cannot do.
- Clearing out the finished tasks, and a count of what is left on each list tab.
- Designed empty states, for a list with nothing on it and for a search that matches nothing.

### Changed

- **The look is the house one**: near-black surfaces, hairline rules, one accent, priority as a
  colour down the edge of a task rather than a word, and what you can do with a task kept out of
  sight until the pointer is on it.
- **Searching, hiding what is finished, filtering by priority and deleting a list are behind one
  menu** instead of a row of toggles across the top.
- The files are split into `Data` and `UI` rather than sitting in one folder.

### Note for anyone upgrading

Old lists are not carried over. The database moved and the class behind it was renamed, so any
`Assets/ToDoListDatabase.asset` is left where it is and ignored. Copy anything you still want
across by hand before deleting it.

## [1.0.1] - 2026-09-23

### Added

- The package, as it stands: a simple to-do list that lives inside the Unity editor.
