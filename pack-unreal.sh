#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

VERSION="${npm_package_version}"
if [ -z "$VERSION" ]; then
    VERSION=$(node -p "require('${SCRIPT_DIR}/package.json').version")
fi

DIST_DIR="${SCRIPT_DIR}/dist/v${VERSION}"
UNREAL_DIR="${SCRIPT_DIR}/unreal"
TEMP_DIR="${SCRIPT_DIR}/dist/unreal_tmp"

PLATFORMS=("osx-arm64" "win-x64")

# Update version fields in the source .uplugin before copying
UPLUGIN="${SCRIPT_DIR}/unreal/Dink/Dink.uplugin"
MAJOR=$(echo "$VERSION" | cut -d. -f1)
MINOR=$(echo "$VERSION" | cut -d. -f2)
PATCH=$(echo "$VERSION" | cut -d. -f3)
VERSION_INT=$(( MAJOR * 10000 + MINOR * 100 + PATCH ))
echo "Updating ${UPLUGIN} to Version=${VERSION_INT}, VersionName=${VERSION}..."
sed -i '' "s/\"Version\": [0-9][0-9]*/\"Version\": ${VERSION_INT}/" "${UPLUGIN}"
sed -i '' "s/\"VersionName\": \"[^\"]*\"/\"VersionName\": \"${VERSION}\"/" "${UPLUGIN}"

for PLATFORM in "${PLATFORMS[@]}"; do
    echo "Creating Unreal zip for ${PLATFORM}..."

    PLATFORM_TEMP="${TEMP_DIR}/${PLATFORM}"
    rm -rf "${PLATFORM_TEMP}"
    mkdir -p "${PLATFORM_TEMP}"

    # Copy unreal folder contents
    cp -r "${UNREAL_DIR}/." "${PLATFORM_TEMP}/"

    # Create ThirdParty/Dink directory inside the Dink plugin folder
    THIRD_PARTY_DIR="${PLATFORM_TEMP}/Dink/ThirdParty/Dink"
    mkdir -p "${THIRD_PARTY_DIR}"

    # Extract platform-specific distribution files into ThirdParty/Dink
    PLATFORM_ZIP="${DIST_DIR}/Dink-${PLATFORM}-${VERSION}.zip"
    if [ ! -f "${PLATFORM_ZIP}" ]; then
        echo "Error: Platform zip not found: ${PLATFORM_ZIP}"
        exit 1
    fi
    unzip -q "${PLATFORM_ZIP}" -d "${THIRD_PARTY_DIR}"

    # Create the final Unreal zip
    OUTPUT_ZIP="${DIST_DIR}/Dink-unreal-${PLATFORM}-${VERSION}.zip"
    (cd "${PLATFORM_TEMP}" && zip -r "${OUTPUT_ZIP}" .)

    echo "Created: ${OUTPUT_ZIP}"
done

rm -rf "${TEMP_DIR}"
echo "Unreal zip creation complete."
