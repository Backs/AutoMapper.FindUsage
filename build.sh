#!/usr/bin/env bash

# Exit on error
set -e

VERSION=${1:-"9999.0.0"}
SOLUTION="ReSharperPlugin.AutoMapper.FindUsage.sln"
OUTPUT_DIR="$(pwd)/output"

echo "Building solution $SOLUTION with version $VERSION..."

dotnet build "$SOLUTION" \
  -c Release \
  -p:PackageVersion="$VERSION" \
  -p:PackageOutputPath="$OUTPUT_DIR" \
  /t:Restore,Rebuild,Pack \
  -v minimal

echo "Build finished. Artifacts are in $OUTPUT_DIR"
