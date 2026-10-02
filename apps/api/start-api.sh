#!/usr/bin/env bash
set -euo pipefail

api_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$api_directory"
exec dotnet run --project "$api_directory/Ida.Api.csproj" --configuration Release --no-launch-profile -- "$@"
