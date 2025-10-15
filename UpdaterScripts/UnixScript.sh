#!/bin/sh

set -e

SCRIPT_PATH="$0"
UPDATER_PATH="$1"
UPDATER_DIR=$( dirname "$UPDATER_PATH" )
UPDATER_DIR_DIR=$( dirname "$UPDATER_DIR" )

"$UPDATER_PATH" "$@"

rm -rf "$UPDATER_DIR"
mv "$UPDATER_DIR_DIR/updater_new" "$UPDATER_DIR_DIR/updater"

rm -- "$SCRIPT_PATH"
