---
description: Instructions for generating Conventional Commits messages in this repo
applyTo: "**"
---

# Conventional Commit Message Instructions

## Mission
Guide generation of concise commit messages that follow the Conventional Commits spec for consistent history, changelog automation, and semantic versioning alignment.

## Scope & Boundaries
- **Included:** Crafting commit message text only (type/scope/description/body/breaking change) for this repository.
- **Excluded:** Performing commits, altering files, or changing repo state.
- **Prohibited:** Deviating from allowed types, exceeding header length, adding trailing periods, or omitting required header parts.

## Expected Output
- Produce a single commit message string in Conventional Commits format.
- Do not add extra prose, commentary, or code diffs—only the message.
- Use imperative, concise language; keep header ≤72 characters.

## Format
```
<type>[optional scope]: <short description>

<body>

BREAKING CHANGE: <description>

```
### Types
feat | fix | docs | style | refactor | perf | test | build | ci | chore | revert

### Scope
- Optional but recommended; use meaningful module/domain identifiers (e.g., api, css, content).
- Keep consistent casing per repo conventions (kebab-case or PascalCase acceptable; avoid snake_case).

### Description
- Imperative, lowercase start unless proper noun; no trailing period; specific and concise.

### Body (optional)
- Blank line after header.
- Explain WHY and context; wrap at ~72 chars; can include issue references.

### Breaking Changes (optional)
- New line after body (or header if no body) starting with `BREAKING CHANGE:`.
- Describe what breaks and how to migrate; may also indicate with `!` after type/scope.

## Process
1) Identify primary change type and optional scope.
2) Draft header `<type>[scope]: <description>` ensuring length and casing rules.
3) If useful, add body explaining rationale, trade-offs, and references.
4) If breaking, add `BREAKING CHANGE:` section with migration guidance.
5) Re-check against validation list before outputting only the commit message.

## Validation Checklist
- Header uses allowed type; scope (if present) is concise and relevant.
- Description imperative, lowercase start, no trailing period, header ≤72 chars.
- Blank line before body; body focuses on WHY, not diff details.
- Breaking change noted with required prefix and guidance when applicable.
- No extra commentary outside the commit message.

## Examples
- Good: `fix(css): remove duplicate classes`
- Good: `feat(api)!: change user response`

  Body:
  ``
  Standardize responses to wrap data/meta for consistency and error handling.

  BREAKING CHANGE: Clients must read data from response.data instead of root.
  ``
- Bad: `updated stuff` (missing type, scope, and imperative form)
- Bad: `feat: Add JWT-based login.` (capitalized, trailing period)

## Guardrails
- If context is insufficient to pick type/scope, ask for clarification before emitting a message.
- Do not invent breaking changes; include only when explicitly indicated by changes.
- Prefer multiple commits when disparate changes are involved; otherwise note secondary changes in body.
- Keep messages self-contained and readable without diff access.
