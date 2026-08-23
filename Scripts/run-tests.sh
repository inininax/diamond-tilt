#!/bin/sh
set -e
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
dotnet test "$SCRIPT_DIR/../Tests/DotNet/EditMode.Tests/EditMode.Tests.csproj" --nologo "$@"
