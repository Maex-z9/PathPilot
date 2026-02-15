---
phase: quick-1
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/PathPilot.Core/Services/UpdateCheckService.cs
  - src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs
  - src/PathPilot.Desktop/SettingsWindow.axaml.cs
  - src/PathPilot.Desktop/TreeViewerWindow.axaml
  - src/PathPilot.Desktop/TreeViewerWindow.axaml.cs
autonomous: true
must_haves:
  truths:
    - "Node search feature changes are in their own atomic commit"
    - "Auto-update installer changes are in their own atomic commit"
    - "Each commit has a conventional commit message matching project style"
  artifacts:
    - path: "git log"
      provides: "Two new commits on main"
  key_links: []
---

<objective>
Create two separate atomic commits for already-implemented features: node search in the skill tree viewer, and auto-install updates via direct installer download.

Purpose: Clean git history with logical, atomic commits.
Output: Two commits on main branch.
</objective>

<execution_context>
@/home/max/.claude/get-shit-done/workflows/execute-plan.md
@/home/max/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
All changes are already implemented but unstaged. No code changes needed -- only staging and committing.
</context>

<tasks>

<task type="auto">
  <name>Task 1: Commit node search feature</name>
  <files>
    src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs
    src/PathPilot.Desktop/TreeViewerWindow.axaml
    src/PathPilot.Desktop/TreeViewerWindow.axaml.cs
  </files>
  <action>
Stage ONLY the three files related to node search and commit them:
- `src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs` (HighlightedNodeIds, NavigateToNode, highlight rendering)
- `src/PathPilot.Desktop/TreeViewerWindow.axaml` (SearchBox + Popup UI)
- `src/PathPilot.Desktop/TreeViewerWindow.axaml.cs` (debounced search, keyboard handling, navigation)

Commit message style: `feat: add node search to skill tree viewer`

Do NOT stage any other files.
  </action>
  <verify>
`git log --oneline -1` shows the node search commit. `git diff --stat` shows only UpdateCheckService.cs, SettingsWindow.axaml.cs, and .planning/config.json remaining.
  </verify>
  <done>Node search files committed as a single atomic commit.</done>
</task>

<task type="auto">
  <name>Task 2: Commit auto-update installer feature</name>
  <files>
    src/PathPilot.Core/Services/UpdateCheckService.cs
    src/PathPilot.Desktop/SettingsWindow.axaml.cs
  </files>
  <action>
Stage ONLY the two files related to auto-install updates and commit them:
- `src/PathPilot.Core/Services/UpdateCheckService.cs` (installer URL extraction, DownloadInstallerAsync)
- `src/PathPilot.Desktop/SettingsWindow.axaml.cs` (download+run installer with progress, app shutdown)

Commit message style: `feat: auto-download and run installer for updates`

Do NOT stage .planning/config.json or any other files.
  </action>
  <verify>
`git log --oneline -2` shows both new commits. `git diff --stat` shows only .planning/config.json remaining unstaged.
  </verify>
  <done>Auto-update installer files committed as a single atomic commit, separate from node search.</done>
</task>

</tasks>

<verification>
- `git log --oneline -3` shows two new feat commits followed by the existing docs commit
- No unrelated files included in either commit
- .planning/config.json remains unstaged (not part of either feature)
</verification>

<success_criteria>
Two atomic commits exist on main: one for node search, one for auto-update installer. Each commit contains only files related to its feature.
</success_criteria>

<output>
After completion, create `.planning/quick/1-commit-node-search-and-auto-update-insta/1-SUMMARY.md`
</output>
