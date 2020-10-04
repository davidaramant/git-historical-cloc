#!/usr/bin/env bash
set -euo pipefail
dotnet run --project git-historical-cloc/git-historical-cloc -- "$@"