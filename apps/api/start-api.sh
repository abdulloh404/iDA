#!/usr/bin/env bash
set -euo pipefail

if [[ $# -gt 2 || "${1:-}" == '-h' || "${1:-}" == '--help' ]]; then
  printf 'Usage: bash start-api.sh [Local|Development|Production] [dev|serve|start]\n'
  exit 0
fi

if ! command -v dotnet >/dev/null 2>&1; then
  printf 'The .NET 9 SDK must be available in PATH.\n' >&2
  exit 127
fi

cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
export DOTNET_ENVIRONMENT="${1:-${DOTNET_ENVIRONMENT:-${ASPNETCORE_ENVIRONMENT:-Local}}}"
export ASPNETCORE_ENVIRONMENT="$DOTNET_ENVIRONMENT"
exec dotnet run --project Ida.Start.csproj --no-launch-profile -- --mode "${2:-dev}"
