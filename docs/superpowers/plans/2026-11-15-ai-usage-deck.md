# AI Usage Deck Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and locally install a Windows collector and six-key Stream Deck usage plugin for Claude Team, Codex Plus, and Antigravity Pro.

**Architecture:** A per-user .NET 10 collector owns credentials, polling and normalized quota snapshots. A TypeScript Stream Deck plugin reads sanitized snapshots through a current-user-only Windows named pipe. CLI sessions remain the credential authority.

**Tech Stack:** .NET 10 SDK already installed, built-in JSON/HTTP/pipes; TypeScript, official Elgato SDK, esbuild and Node built-in test runner. No cloud backend.

**Spec:** docs/superpowers/specs/2026-11-15-ai-usage-deck-design.md (approved by user: “Ổn rồi, thực thi đi thôi”).

## Global Constraints

- No model prompts, auto login, credential writes or account switching.
- Keep credentials only in collector memory and existing CLI-owned stores; no raw subprocess logs.
- Preserve provider/window/group identity, null data and original capture timestamps.
- User-scoped named pipe, bounded messages and subprocesses, one refresh per provider at once.
- Six-key layout: Claude/Codex/Antigravity columns, 5-hour/weekly rows; short press reset countdown, hold 700 ms refresh.
- No profile overwrite, administrator permission, cloud service or telemetry.

## Review Focus

- Account swaps mid-request must reject old results even with failed identity verification.
- Unknown windows and missing measurements must stay unavailable, never become zero or full.
- Failure/backoff and reset deadlines must not cause a tight polling loop or bypass Retry-After.
- Disconnects and partial/oversized pipe lines must fail boundedly and show offline.
- Long press, reconnect and six visible keys must not multiply backend usage checks.

### Task 1: Provider feasibility and normalization

**Files:** src/AIUsageDeck.Core/{Models,QuotaParser}.cs; tests/AIUsageDeck.Tests/{Program,ParserTests}.cs; probes/probe.mjs (ignored).

**Interfaces:** QuotaParser.ParseClaude/ParseCodex/ParseAntigravity(JsonElement) -> IReadOnlyList<QuotaBucket>; QuotaBucket(GroupId, GroupName, Window, RemainingPercent?, ResetAt?).

- [ ] Probe only read-only provider account/usage routes; print status and sanitized schema, never tokens/identity values. Expected: Antigravity JSON and Codex account methods work; record Claude Team feasibility truthfully.
- [ ] Create independent literal fixture tests for remaining percentages, missing values, nonstandard windows, reset timestamps and invalid values. Run test executable; Expected: fails for missing behavior.
- [ ] Implement normalization and run all tests; Expected: pass.
- [ ] Commit normalization and record evidence.

### Task 2: Collector adapters, scheduler and IPC

**Files:** src/AIUsageDeck.Collector/{Program,Adapters,ProcessRunner,CollectorEngine,PipeServer,SnapshotStore}.cs; tests/AIUsageDeck.Tests/{EngineTests,IpcTests}.cs.

**Interfaces:** IUsageAdapter.ReadIdentityAsync/FetchAsync -> identity string/ProviderRead; CollectorEngine.RefreshAsync(provider, force, cancellation), GetSnapshot(), TickAsync(); pipe NDJSON {version:1,id,method:get|refresh|subscribe,provider?} -> {version:1,id?,snapshot?,error?}.

- [ ] Write failing state tests: stale observation age, identity swap plus late completion, reset confirmation, refresh dedup, rate-limit backoff and malformed IPC. Expected: fail before implementation.
- [ ] Implement real source adapters, bounded process runner, per-provider scheduler, atomic sanitized snapshot cache with user ACL and pipe ACL. Preserve timestamps on failures; reject unknown protocol methods; no raw exception logging.
- [ ] Run state/IPC tests and live --once probes; Expected: no fake usage, no model turns; cache contains only whitelisted snapshot fields.
- [ ] Commit collector and record live limitations.

### Task 3: Stream Deck plugin and six-key UX

**Files:** plugin/{package.json,tsconfig.json,src/{plugin,pipe-client,render,settings}.ts,tests/*.test.mjs,com.local.aiusagedeck.sdPlugin/{manifest.json,ui/settings.html,imgs/*}}.

**Interfaces:** plugin consumes protocol v1; renderKey(snapshot, settings, now, mode) -> SVG data URL. Action settings contain provider/window/group only. No credentials/account IDs.

- [ ] Write failing Node tests for unavailable/stale/reset-pending rendering, XML escaping, correct group/window, and 700 ms hold vs short press. Expected: fail before implementation.
- [ ] Install pinned Elgato SDK/build tools; implement plugin, reconnecting pipe client, collector launch with windowsHide and property inspector; render SVGs without external assets/network.
- [ ] Build and run Node tests, validate manifest/package; Expected: pass and six-key profile available separately without overwriting existing profiles.
- [ ] Commit plugin.

### Task 4: Local deployment, verification and handover

**Files:** scripts/{build,install,uninstall}.ps1; README.md; outputs AIUsageDeck release directory and source archive.

**Interfaces:** build creates self-contained Windows x64 collector, bundled plugin and package; install copies only this plugin UUID and collector data with backups of this app's prior install; uninstall targets only this app.

- [ ] Build release and run full .NET/Node suites and real named-pipe smoke test. Expected: successful read-only live quotas or explicit source error; no public listening port.
- [ ] Install plugin locally using supported Stream Deck tooling; provide a separate optional six-key profile and preserve current profile. Verify actual plugin connection and rendered action updates; if physical placement requires user action, state exact handoff.
- [ ] Run fresh whole-project review, fix important findings with regression tests, and rerun affected suites.
- [ ] Package source, usage/extension guide, release and checksums under outputs; commit implementation and give concise verified status.

## Execution notes

User explicitly authorized execution; implement inline without another plan approval. New project has a dedicated repository/feature branch under work, so there is no existing branch to isolate. Platform shell is PowerShell; ledger and task briefs use native filesystem operations in place of POSIX helper scripts.
