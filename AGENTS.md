# Working Rules

## Scope

- This repository supports Claude Code and Codex.
- Keep repository skills under `.agents/skills`; expose the same skills to Claude through `.claude/skills`.
- The current repository skills are limited to Nx workflows.

## Workspace

- This is an Nx monorepo with `apps/mobile`, `apps/web`, and `apps/api`.
- Use `nx-workspace` to inspect projects, targets, and dependencies.
- Use `nx-generate` before scaffolding, `nx-run-tasks` for Nx tasks, `nx-plugins` for plugin work, and `nx-import` for repository imports.
- Run Nx through the workspace package manager and use the Nx MCP server when useful.
- Check `node_modules/@nx/<plugin>/PLUGIN.md` for plugin guidance when that file exists.
- Check `nx --help` or Nx documentation before using unfamiliar flags.

## Architecture

- The Core API is one shared backend codebase and service for every BU.
- The Worker is one shared codebase; each running instance selects a BU through configuration.
- Data is split into one external Core database and isolated external BU databases; the current configuration enables BU01–BU02.
- BU separation applies to database connections and runtime configuration, not source code.
- Each BU Worker receives its own `BU_ID`, queue settings, and BU database connection.
- API and workers connect directly to configured PostgreSQL and RabbitMQ endpoints without Kubernetes.

## Verification

- After code changes, run the normal Nx build exactly once.
- Do not run tests, linters, servers, watch tasks, or extra validation unless explicitly requested.
- Never start, restart, stop, or kill the user's processes.
