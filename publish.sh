#!/usr/bin/env bash

# Exit on error
set -e

if [ "$#" -lt 2 ]; then
    echo "Usage: ./publish.sh <Version> <ApiKey> [Configuration]"
    exit 1
fi

VERSION=$1
API_KEY=$2
CONFIGURATION=${3:-"Release"}
PLUGIN_ID="ReSharperPlugin.AutoMapper.FindUsage"
OUTPUT_DIR="$(pwd)/output"
SOLUTION="ReSharperPlugin.AutoMapper.FindUsage.sln"

# Extract latest changelog section (simplistic version)
CHANGELOG=$(sed -n '/^## \[/,/^## \[/{ /^## \[/!p; }' CHANGELOG.md | head -n 20)

echo "Publishing $PLUGIN_ID version $VERSION..."

dotnet build "$SOLUTION" \
  -c "$CONFIGURATION" \
  -p:PackageVersion="$VERSION" \
  -p:PackageOutputPath="$OUTPUT_DIR" \
  -p:PackageReleaseNotes="$CHANGELOG" \
  /t:Restore,Rebuild,Pack \
  -v minimal

PACKAGE_FILE=$(ls $OUTPUT_DIR/$PLUGIN_ID.$VERSION.nupkg)

if [ -f "$PACKAGE_FILE" ]; then
    echo "Pushing $PACKAGE_FILE..."
    dotnet nuget push "$PACKAGE_FILE" -s "https://plugins.jetbrains.com/api/v2/package" -k "$API_KEY"
else
    echo "Error: Package file $PACKAGE_FILE not found."
    exit 1
fi
