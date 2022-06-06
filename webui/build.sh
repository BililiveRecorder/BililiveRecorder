#!/usr/bin/env bash
set -ex
SCRIPT_DIR="$( cd -- "$( dirname -- "${BASH_SOURCE[0]:-$0}"; )" &> /dev/null && pwd 2> /dev/null; )";
pushd "$SCRIPT_DIR/source"
export BASE_URL="/ui/"
export EMBEDED_BUILD="true"
npm ci && npx vite build
cp --recursive dist ../../BililiveRecorder.Web/embeded/ui
popd
