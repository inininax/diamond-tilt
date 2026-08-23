# Third-Party Notices

Runtime dependencies of `Assets/Scripts/Core/**`: **none** (plain C#, netstandard2.1 surface only).

## Dev/test-only (never shipped to players)

| Package | Version | License | Used by |
|---|---|---|---|
| NUnit | 3.14.0 | MIT | Tests/EditMode, Unity Test Framework compatible |
| NUnit3TestAdapter | 4.6.0 | MIT | Tests/DotNet harness |
| Microsoft.NET.Test.Sdk | 17.11.1 | MIT | Tests/DotNet harness |
| System.Text.Json | inbox (net10) / 8.x | MIT | Tests/DotNet/Adapters save JSON envelope |

## Unity-side (to be recorded on import)

Any Asset Store package, SDK (Unity IAP, ads mediation), or art/audio asset must be added here with its license BEFORE import, per `Docs/SECURITY.md` supply-chain rules.
