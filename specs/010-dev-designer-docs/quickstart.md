# Quickstart: Developer & Designer Documentation Bootstrap

**Feature**: 010-dev-designer-docs
**Date**: 2026-03-03

## What This Feature Does

Creates the `docs/` directory at the project root with 25 markdown files providing architecture references, per-system documentation, designer guides, and supporting materials for the VoidHarvest project. Required by Constitution v1.4.0.

## Prerequisites

- Git checkout of branch `010-dev-designer-docs`
- Access to the VoidHarvest codebase for reference
- Markdown editor (VS Code recommended for Mermaid preview)

## Implementation Approach

This is a documentation-only deliverable. The workflow is:

1. **Create directory structure** — `docs/architecture/`, `docs/systems/`, `docs/designer-guide/`
2. **Write architecture docs** (5 files) — Cross-cutting diagrams and explanations
3. **Write system docs** (12 files) — Per-feature reference with 10 mandatory sections each
4. **Write designer guides** (4 files) — Non-programmer workflows and SO catalog
5. **Write supporting docs** (4 files) — Glossary, troubleshooting, onboarding, assembly map
6. **Validate** — Check all sections present, diagrams render, cross-references resolve

## Key Constraints

- All diagrams use Mermaid syntax
- Designer guide contains zero code, zero namespaces, zero jargon
- Every system doc has exactly 10 sections (Purpose, Architecture Diagram, State Shape, Actions, SO Configs, ECS Components, Events, Assembly Dependencies, Key Types, Designer Notes)
- Content must accurately reflect current codebase state (post-Spec 009)

## Verification

- Count: 25 files in correct directory structure
- Sections: Each system doc has 10 headings
- Diagrams: Each system + architecture doc has at least 1 Mermaid block
- Designer: `grep -rn 'namespace\|sealed record\|readonly struct\|public class' docs/designer-guide/` returns nothing
- Links: All relative cross-references resolve
