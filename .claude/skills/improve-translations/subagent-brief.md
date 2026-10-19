# Translation review brief

You are reviewing the localized strings of the 0install-dotnet libraries (`ZeroInstall.*`) for ONE language. Zero Install is a decentralized software installation system. These strings appear in:
- CLI output and option help (`0install --help`);
- exception and log messages;
- progress/task names;
- questions to the user;
- GUI dialogs shown by 0install-win.

**Do not edit any repository file.** Only read files and write your proposals JSON to the path given in your prompt.

Your prompt gives you:
- the language;
- the paths of the context file and the output file;
- the user's decision on the stray-space artifact (see "Scope decisions" below).

## Inputs
- **Context file:** one block per string. Read the whole file in chunks; it is about 200 KB and 4,400 lines. Each block has:
  - a header `## <project>: <key>`;
  - `en:`, the English source;
  - `used:`, the call sites with surrounding code;
  - `<lang> [auto|manual|missing]: <translation>`.
- **Repository:** the current working directory. If a usage snippet is not enough to understand a string (e.g. it is passed through several layers or concatenated with other strings), open the referenced file. Strings with no direct references may be looked up by reflection or used from other repositories (e.g. 0install-win uses `ZeroInstall.Commands` resources); judge them by their key name and wording.

## Status meaning
- `auto` = DeepL output. Most likely to be wrong; review carefully.
- `manual` = human translation. Only propose changes for CLEAR errors: wrong meaning, a translation of an older English text, broken placeholders, garbage like `XXX`, a translated product name, a translated literal. Do not restyle human translations.
- `missing` = no translation. Propose one (use `"old": ""`).

## What to check
- **Meaning in context:** wrong sense of ambiguous words. Zero Install terms:
  - "store" is the implementation cache (a noun, not a shop);
  - "feed" is the XML file describing an app, not a news feed;
  - "implementation" is a specific version of an app in the store, not "application" or "instance";
  - "catalog" is a list of apps, not a print catalog;
  - "desktop integration" is not "desktop shortcut";
  - also watch "run", "interface", "app list", "digest" and "selections".
- **Terminology consistency:** build a glossary from this language's `manual` entries for recurring Zero Install concepts and align `auto` entries with it. If you change a term, fix the same term in this language's other strings too.
- **Keep literal:**
  - command names (`0install run`, `add`, `integrate`, ...) and options (`--batch`);
  - usage metavariables in help texts (`DIRECTORY`, `DOMAIN`, `FINGERPRINT`, `PATTERN`, `TARGET`, `{CATEGORY}`, `{MODE}`), because the usage line printed next to them is not translated;
  - literal values such as the `--reset` modes `none`/`client`/`server` and the `START..!END` range syntax;
  - XML names and values (`<implementation>`, `stability`, `testing`, the attribute `uri`);
  - file names, environment variables (`%ProgramFiles%`) and URLs;
  - product names: Zero Install and 0install are never translated or transliterated (not 零安装, ゼロインストール or 제로 인스톨).
- **Access keys:** `&` marks a keyboard access key in strings shown as GUI labels; `&&` is a literal ampersand.
  - If the source has exactly one `&`, the translation must have exactly one, on a letter in the translated word.
  - For Japanese, Korean and Chinese, append `(&X)` with the English access-key letter.
  - If the source has no `&`, the translation must have none.
- **Placeholders and formatting:**
  - preserve every `{0}`, `{1}` and format specifier like `{0:0.00}`;
  - preserve line breaks and leading/trailing whitespace exactly (strings may be concatenated or column-aligned in CLI help);
  - use the language's quote marks around placeholders.
- **Grammar with placeholders:** the sentence must remain grammatical once `{0}` is substituted (gender/case, word order for a file path vs. a name; counts vs. names).
- **Register and tone:** keep formal/informal address consistent with the manual entries; count both forms there before changing register. Status messages and option help must not be first-person ("I resolved...", "Choosing first...") or instructions ("Please open..."). Short labels must not be overly literal or verbose.
- **Untranslated or mixed-language text**, and incomplete or plainly wrong translations.

Recurring errors from earlier runs:
- **Count placeholders read as names:** "the {0} checked element(s)".
- **Usage metavariables translated** (see above).
- **Literal values translated:** `0install central` became "centrale"; the `--reset` modes, `START..!END`, `%ProgramFiles%` and `uri` were translated too.
- **Wrong word sense:**
  - "Releases" in `OptionUnpin` became "launches" or "publishes";
  - "decimal" (version parts) became "fractional";
  - "store" became a shop; "implementation" became "application" or "instance";
  - "arguments" became debate arguments;
  - "exclusive" (version range end) became "special" or "monopolize";
  - "desktop integration" became "desktop shortcut".
- **Inverted meaning:** "case insensitive" became "case sensitive"; "{0} of {1}" became "{1} of {0}".
- **Product name translated.**
- **First-person or instruction phrasing.**
- **Stale manual entries** made from an older English source. Compare meaning, not just wording.
- **DeepL artifacts:**
  - backticks around placeholders, `l'{0} e`;
  - missing spaces like `{0}angefordert`;
  - a lowercase sentence start or a capital mid-sentence after a placeholder.
- **Garbage** like `XXX`.

## Scope decisions
Your prompt states whether to fix the **stray-space artifact** everywhere or only in entries you are changing for another reason. The artifact is a stray space between a placeholder and a quote mark or particle (`«{0} »`, `„{0} “`, `「{0} 」`, `{0} を`). If it says "only in changed entries", do not create proposals that only fix a stray space.

## Output
Write a JSON array (UTF-8) to the path given in your prompt:

```json
[
  {"project": "Commands", "key": "SomeKey", "status": "auto",
   "old": "<current translation copied EXACTLY from the context file>", "new": "<proposed>", "reason": "<short English reason>"}
]
```
- `project` is the project name from the `## <project>: <key>` header, e.g. `Commands` or `Services`.
- `old` must equal the current value exactly, character for character, including trailing line breaks (encode line breaks as `\n` in JSON). For `missing` entries, use `""`.
- Values are plain text, NOT XML-escaped (write `&Help`, not `&amp;Help`).
- `new` must differ from `old`. It must keep the same placeholders, the same number of line breaks, the same leading/trailing whitespace and the source's access-key count.
- If nothing needs changing, write `[]`.

Write the JSON with the Write tool, which preserves UTF-8. Then check it by running `Apply-Translations.ps1 -WhatIf` with the path and language from your prompt. It reports `old` mismatches and proposals that fail validation against the English source without changing any file:

```powershell
.\.claude\skills\improve-translations\scripts\Apply-Translations.ps1 -Path <output file> -Language <lang> -WhatIf
```

Fix every warning it prints. The only exception is an intended difference, such as restoring a line break the translation was missing; mention that in your reply.

## Reply
Reply briefly with:
- the number of proposals per project, and how many touch manual entries;
- the key terminology you used (English term -> translation, about 10 lines);
- systematic issues you saw but left unfixed (e.g. many stray spaces, mixed register).
