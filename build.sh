#!/usr/bin/env bash

bash --version 2>&1 | head -n 1

set -eo pipefail
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

###########################################################################
# CONFIGURATION
###########################################################################

BUILD_PROJECT_FILE="$SCRIPT_DIR/build/_build.csproj"
TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp"

DOTNET_GLOBAL_FILE="$SCRIPT_DIR/global.json"
DOTNET_INSTALL_URL="https://dot.net/v1/dotnet-install.sh"
DOTNET_CHANNEL="STS"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_MULTILEVEL_LOOKUP=0

###########################################################################
# EXECUTION
###########################################################################

function FirstJsonValue {
    perl -nle 'print $1 if m{"'"$1"'"\s*:\s*"([^"]+)"}' <<< "${@:2}"
}

# If dotnet CLI is installed globally and it's good enough, use it directly
if [ -x "$(command -v dotnet)" ]; then
    export DOTNET_EXE="$(command -v dotnet)"
else
    DOTNET_DIRECTORY="$TEMP_DIRECTORY/dotnet"
    export DOTNET_EXE="$DOTNET_DIRECTORY/dotnet"

    if [[ ! -f "$DOTNET_EXE" ]]; then
        mkdir -p "$DOTNET_DIRECTORY"

        if [[ -z "$DOTNET_VERSION" && -f "$DOTNET_GLOBAL_FILE" ]]; then
            DOTNET_VERSION=$(FirstJsonValue "version" "$(cat "$DOTNET_GLOBAL_FILE")")
            if [[ "$DOTNET_VERSION" == "" ]]; then
                unset DOTNET_VERSION
            fi
        fi

        curl -Lsfo "$TEMP_DIRECTORY/dotnet-install.sh" "$DOTNET_INSTALL_URL"
        chmod +x "$TEMP_DIRECTORY/dotnet-install.sh"

        if [[ -z "$DOTNET_VERSION" ]]; then
            "$TEMP_DIRECTORY/dotnet-install.sh" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --no-path
        else
            "$TEMP_DIRECTORY/dotnet-install.sh" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
        fi
    fi
fi

echo "Microsoft (R) .NET Core SDK version $("$DOTNET_EXE" --version)"

"$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
"$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
