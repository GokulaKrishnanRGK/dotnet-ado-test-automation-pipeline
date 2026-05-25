#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 2 ]; then
    echo "Usage: $0 <env-file> <command> [args...]" >&2
    exit 2
fi

env_file="$1"
shift

if [ ! -f "$env_file" ]; then
    echo "Environment file '$env_file' was not found." >&2
    exit 1
fi

while IFS= read -r line || [ -n "$line" ]; do
    line="${line%$'\r'}"

    if [[ "$line" =~ ^[[:space:]]*$ ]] || [[ "$line" =~ ^[[:space:]]*# ]]; then
        continue
    fi

    if [[ "$line" =~ ^[[:space:]]*export[[:space:]]+(.+)$ ]]; then
        line="${BASH_REMATCH[1]}"
    fi

    if ! [[ "$line" =~ ^[[:space:]]*([A-Za-z_][A-Za-z0-9_]*)[[:space:]]*=[[:space:]]*(.*)$ ]]; then
        echo "Invalid dotenv line in '$env_file': $line" >&2
        exit 1
    fi

    key="${BASH_REMATCH[1]}"
    value="${BASH_REMATCH[2]}"

    if [[ "$value" =~ ^\"(.*)\"$ ]]; then
        value="${BASH_REMATCH[1]}"
    elif [[ "$value" =~ ^\'(.*)\'$ ]]; then
        value="${BASH_REMATCH[1]}"
    fi

    export "$key=$value"
done < "$env_file"

exec "$@"
