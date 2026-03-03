# Data Model: Developer & Designer Documentation Bootstrap

**Date**: 2026-03-03
**Feature**: 010-dev-designer-docs

## Overview

This feature produces documentation files, not runtime data structures. The "data model" defines the taxonomy of document types, their required sections, and relationships.

## Document Types

### Architecture Document

A cross-cutting reference document targeting developers.

| Field | Description | Constraints |
|-------|-------------|-------------|
| Title | Document heading | Must match constitution-mandated name |
| Purpose | 2-3 sentence scope description | Required |
| Mermaid Diagrams | One or more diagrams | Minimum 1 per doc; must use valid Mermaid syntax |
| Cross-references | Links to related system docs | Relative markdown links |
| Content Sections | Varies by document | See per-document requirements in spec FR-004 through FR-008 |

**Instances**: overview.md, state-management.md, event-system.md, dependency-injection.md, data-pipeline.md

### System Document

A per-feature reference document targeting developers. Has 10 mandatory sections.

| Section | Description | Required |
|---------|-------------|----------|
| 1. Purpose | 2-3 sentence system scope | Yes |
| 2. Architecture Diagram | Mermaid data flow diagram | Yes (min 1 diagram) |
| 3. State Shape | Immutable records and readonly structs | Yes (may be "None" for stateless systems) |
| 4. Actions | Reducer action types handled | Yes (may be "None") |
| 5. ScriptableObject Configs | SO types with fields, defaults, ranges | Yes (may be "None") |
| 6. ECS Components | IComponentData, BlobAssets, singletons | Yes (may state "managed layer only") |
| 7. Events | Published and subscribed event types | Yes (may be "None") |
| 8. Assembly Dependencies | Referenced assemblies | Yes |
| 9. Key Types | Type name → role mapping table | Yes |
| 10. Designer Notes | What designers can change without code | Yes |

**Instances**: camera.md, input.md, ship.md, mining.md, procedural.md, resources.md, hud.md, docking.md, station-services.md, targeting.md, station.md, world.md

### Designer Guide

A non-programmer-facing document with step-by-step workflows.

| Field | Description | Constraints |
|-------|-------------|-------------|
| Title | Guide name | Descriptive, jargon-free |
| Introduction | What this guide helps you do | 1-2 sentences, plain language |
| Steps | Numbered workflow | Asset paths, field descriptions, no code |
| Field Reference | Table of configurable settings | Name, description, default, valid range |
| Tips | Common mistakes or best practices | Optional |

**Constraints**: Zero C# code, zero namespace references, zero architectural jargon. Unity Editor menu paths treated as proper nouns.

**Instances**: scriptable-objects.md, adding-ores.md, adding-stations.md, tuning-reference.md

### Supporting Document

Reference material for onboarding and daily development.

| Document | Primary Content | Key Constraint |
|----------|----------------|----------------|
| glossary.md | Term → Definition table | ~30 terms, alphabetical |
| troubleshooting.md | Problem → Cause → Solution entries | Must include all known DOTS/ECS gotchas |
| onboarding.md | Recommended reading order with links | Must link to all other docs |
| assembly-map.md | Mermaid dependency graph | Must show all 29 assemblies |

## Relationships

```text
onboarding.md ──links-to──► architecture/*.md
onboarding.md ──links-to──► systems/*.md
onboarding.md ──links-to──► designer-guide/*.md
onboarding.md ──links-to──► glossary.md

architecture/overview.md ──references──► systems/*.md (per feature)
architecture/state-management.md ──references──► systems/*.md (state shapes)
architecture/event-system.md ──references──► systems/*.md (events)

systems/*.md ──references──► architecture/*.md (cross-cutting context)
systems/*.md ──references──► glossary.md (terminology)

designer-guide/*.md ──references──► scriptable-objects.md (SO catalog)
designer-guide/*.md ──references──► tuning-reference.md (parameter lookup)
```

## Validation Rules

1. Every system doc MUST have exactly 10 sections (numbered headings matching the System Document schema above)
2. Every architecture doc MUST have at least 1 Mermaid code block
3. Every system doc MUST have at least 1 Mermaid code block
4. assembly-map.md MUST have at least 1 Mermaid code block
5. Designer guide docs MUST NOT contain patterns matching: `namespace`, `using`, `class `, `public `, `private `, `void `, `static `, `sealed record`, `readonly struct`
6. All relative links MUST resolve to existing files within the `docs/` directory
7. Glossary MUST be alphabetically sorted
