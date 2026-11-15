# Stream Deck AI Usage Monitor

[Tiếng Việt (Vietnamese Guide)](README.vi.md)

A local-first Windows x64 collector and **Elgato Stream Deck** plugin *(installed in Stream Deck as **AI Usage Deck**)* for monitoring **remaining** subscription usage quotas (`5H` & `7D`) and reset times across **Claude Code**, **OpenAI Codex (ChatGPT)**, and **Antigravity (`agy`)** — with zero model prompts, zero token burn, and zero cloud backend.

---

## Features

- **Shows remaining quota (`%`)**, not used quota, across `5H` (5-hour) and `7D` (weekly) windows.
- **Zero model turns / zero token burn:**
  - **Claude:** Reads the local Claude Code OAuth usage/profile endpoints in memory after verifying account and organization identity match your local CLI metadata.
  - **Codex:** Spawns local `codex app-server --stdio` and calls only `initialize`, `account/read` (`refreshToken: false`), and `account/rateLimits/read`.
  - **Antigravity:** Runs `agy -p /usage --output-format json` and rejects any output where `num_turns != 0` or `total_tokens != 0`. Independent quota groups (e.g., *Gemini Models*, *Claude and GPT models*) stay strictly separated.
- **Key interactions:**
  - **Short press:** Shows live reset countdown (`Xd Yh`, `Xh Ym`, or `Xm`) for 5 seconds (calculated locally with zero backend requests).
  - **Hold ($\ge 700\text{ ms}$) and release:** Triggers a manual refresh for that provider (deduplicated and gated by a 15s cooldown and server `Retry-After`).
- **Privacy & OS isolation:**
  - Runs strictly on your machine under your Windows user account.
  - No telemetry, no cloud sync, no listening TCP/LAN ports.
  - Plugin communicates with the collector exclusively via a current-user-only Windows named pipe (`\\.\pipe\AIUsageDeck.v1`).
  - Cached snapshot (`%LOCALAPPDATA%\AIUsageDeck\snapshot.json`) stores only sanitized quota fields inside a directory protected with a current-user-only NTFS ACL.

---

## Key Statuses & Colors

| Status | Meaning |
| --- | --- |
| `LIVE` (shows local `HH:mm` reset time) | Freshly verified quota; color scales with remaining `%`: green (`> 30%`), amber (`10–30%`), red (`< 10%`). |
| `OFFLINE` | Stream Deck plugin is waiting for or reconnecting to `AIUsageDeck.Collector`. |
| `LOGIN` | CLI credentials are missing or expired. Re-authenticate in your CLI (`claude`, `codex`, or `agy`); this tool never performs auto-login or credential writes. |
| `ACCOUNT` | Active CLI account/organization changed or failed verification; stale numbers from the previous account are cleared. |
| `OLD` | Cached observation is stale (> 2 poll intervals) or the latest refresh failed; preserves original `observedAt`. |
| `WAIT` | Provider rate-limited (`429` / backoff); manual hold cannot bypass `Retry-After`. |
| `RESET` | Reported reset timestamp has passed and the collector is waiting for server confirmation (never assumes `100%`). |
| `N/A` | The provider or selected quota group does not expose that time window. |

---

## Requirements

### To run the plugin
- **OS:** Windows 10 / Windows 11 (x64)
- **Elgato Stream Deck:** Software `7.1+` (verified on `7.6.1`)
- **AI CLIs (any or all of):**
  - Claude Code CLI (logged in)
  - OpenAI Codex CLI (logged in with ChatGPT subscription)
  - Antigravity CLI (`agy`, logged in)

### To build from source
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) (22+ recommended) and `npm`

---

## Quick Start (Build & Install from Source)

1. **Clone the repository:**
   ```powershell
   git clone https://github.com/hyperxnez/stream-deck-ai-usage-monitor.git
   cd stream-deck-ai-usage-monitor
   ```

2. **Build, test, and package a local release:**
   ```powershell
   ./scripts/build.ps1
   ```
   This runs the `.NET` unit/integration tests, builds the self-contained Windows x64 `AIUsageDeck.Collector.exe`, installs npm dependencies offline-safely (`npm ci --ignore-scripts`), bundles the Stream Deck plugin, runs Node tests, generates `AI Usage Deck.streamDeckProfile`, packs `com.local.aiusagedeck.streamDeckPlugin`, and writes SHA-256 checksums into `artifacts/release/`.

   *(Optional)* Run the live named-pipe smoke test **while the production collector is not running**:
   ```powershell
   node scripts/smoke.mjs artifacts/collector/AIUsageDeck.Collector.exe
   ```

3. **Install into Stream Deck:**
   - **Option A (Recommended):** Double-click `artifacts/release/com.local.aiusagedeck.streamDeckPlugin` to install via Stream Deck.
   - **Option B (PowerShell script):** Close Stream Deck, run `./artifacts/release/install.ps1`, then reopen Stream Deck. Any previous `com.local.aiusagedeck.sdPlugin` install is automatically moved to `%LOCALAPPDATA%\AIUsageDeck\Backups\`.

4. **Place keys on your Stream Deck:**
   - **Any Stream Deck model (15-key, XL, Mini, Neo, etc.):** Drag any of the six actions under the **AI Usage Deck** category onto a page, and pick `Provider`, `Window` (`5H` / `7D`), and `Quota Group` in the Property Inspector.
   - **Stream Deck Mini (3×2, model `20GAI9901`):** You can optionally double-click `artifacts/release/AI Usage Deck.streamDeckProfile` to import a dedicated 6-key page without overwriting your existing profiles.

   Suggested 3×2 layout:

   | Row \ Col | Col 1 | Col 2 | Col 3 |
   | --- | --- | --- | --- |
   | **Top** | Claude (`5H`) | Codex (`5H`) | Antigravity (`5H`) |
   | **Bottom** | Claude (`7D`) | Codex (`7D`) | Antigravity (`7D`) |

   > **Tip for Antigravity:** Configure both your `5H` and `7D` Antigravity keys to the same quota group (e.g., `Gemini Models`). Independent groups are never summed together.

---

## Configuration & CLI Discovery

- **Environment variables:** The collector honors `CLAUDE_CONFIG_DIR` (defaults to `%USERPROFILE%\.claude`) and `CODEX_HOME` (defaults to `%USERPROFILE%\.codex`).
- **CLI lookup order:** `IdentityTools.Executable` in [src/AIUsageDeck.Collector/AdapterPaths.cs](src/AIUsageDeck.Collector/AdapterPaths.cs) checks standard Windows install locations (`%LOCALAPPDATA%\agy\bin\agy.exe`, npm global `@openai/codex`, `%USERPROFILE%\.local\bin`, and `PATH`).
- **Polling intervals:** `120s` while Stream Deck is connected to the pipe; `300s` when no subscribers are connected. The plugin spawns `AIUsageDeck.Collector.exe` hidden (`windowsHide: true`) on demand and does not register a Windows Startup entry.

---

## Architecture

```text
stream-deck-ai-usage-monitor/
├── src/
│   ├── AIUsageDeck.Core/          # Shared quota models, parsers, CollectorEngine (scheduler/backoff/identity)
│   └── AIUsageDeck.Collector/     # Windows x64 service: adapters, ACL snapshot store, named-pipe NDJSON server
├── plugin/
│   ├── src/                       # TypeScript Stream Deck plugin, SVG renderer, pipe client, press tracker
│   ├── tests/                     # Node built-in test runner suites
│   └── com.local.aiusagedeck.sdPlugin/  # Manifest, generated icons, Property Inspector UI
├── tests/AIUsageDeck.Tests/       # .NET test runner (parser, engine, IPC, adapter fixtures)
└── scripts/                       # build.ps1, create-profile.ps1, install.ps1, uninstall.ps1, smoke.mjs
```

### Pipe Protocol (`v1`)

Transport: `\\.\pipe\AIUsageDeck.v1` (UTF-8 NDJSON, max `65536` bytes per line, `CurrentUserOnly`).

- Requests: `{"version":1,"id":"...","method":"get"|"subscribe"|"refresh","provider":"claude"|"codex"|"antigravity"}`
- Responses: `{"version":1,"id":"...","snapshot":{...}}`

---

## Running Tests

```powershell
dotnet run --project tests/AIUsageDeck.Tests -c Release
cd plugin
npm ci --ignore-scripts --no-fund --no-audit
npm run build
npm test
```

Diagnostic one-shot collector probe (prints sanitized snapshot JSON to stdout and exits):

```powershell
.\artifacts\collector\AIUsageDeck.Collector.exe --once
```

---

## Extending with Another AI Provider

1. Implement `IUsageAdapter` (`Name`, `ReadIdentityAsync`, `FetchAsync`) in `src/AIUsageDeck.Collector/`.
2. Return `ProviderRead` with a hashed/non-sensitive identity and normalized `QuotaBucket` entries (leave `RemainingPercent` as `null` when unknown — never fabricate `0%` or `100%`).
3. Register the adapter in `src/AIUsageDeck.Collector/Program.cs`, add the provider ID to `PipeProtocol.cs`, and register it in `plugin/src/` and `ui/settings.html`.
4. Add unit tests covering normalization, identity swaps, and rate-limit/auth failures.

---

## Uninstall

1. Close Stream Deck.
2. Run `uninstall.ps1` (moves only `com.local.aiusagedeck.sdPlugin` into a recoverable backup under `%LOCALAPPDATA%\AIUsageDeck\Uninstalled\` and leaves your Stream Deck profiles untouched), or uninstall from the Stream Deck UI.

---

## Security & Limitations

- **Claude OAuth endpoint:** Uses the CLI's read-only OAuth usage/profile endpoints, which are not a formally guaranteed public API and may require adapter updates if Anthropic changes them. Tokens are read from Claude Code's credential store into memory only and never logged or written back.
- **Antigravity identity:** `agy /usage` output does not include a Google account identifier; the collector tracks the active ambient CLI session under label `CLI`.
- **Same-user boundary:** Restricts pipe and cache access to the current Windows user account (`CurrentUserOnly` + user-only NTFS ACL). Another program running under the same Windows user account shares OS user privilege by design.

---

## License

[MIT](LICENSE)
