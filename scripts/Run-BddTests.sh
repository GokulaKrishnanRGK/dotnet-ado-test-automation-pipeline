#!/usr/bin/env bash
set -euo pipefail

configuration="${CONFIGURATION:-Debug}"
results_directory="${BDD_RESULTS_DIRECTORY:-artifacts/test-results/bdd}"
project_path="tests/OpsLedger.BddTests/OpsLedger.BddTests.csproj"
trx_file_name="OpsLedger.BddTests.trx"

mkdir -p "$results_directory"

if [ -n "${OPSLEDGER_BDD_ARTIFACT_NAME:-}" ]; then
    echo "BDD artifact: $OPSLEDGER_BDD_ARTIFACT_NAME"
fi

if [ -n "${OPSLEDGER_BDD_COMMIT_SHA:-}" ]; then
    echo "BDD artifact commit: $OPSLEDGER_BDD_COMMIT_SHA"
fi

dotnet test "$project_path" \
    --configuration "$configuration" \
    --logger "trx;LogFileName=$trx_file_name" \
    --results-directory "$results_directory"

echo "BDD test results written to $results_directory/$trx_file_name"
