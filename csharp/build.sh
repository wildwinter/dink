#!/bin/bash

rm -rf ./dist/*

version=$npm_package_version
targets=("osx-arm64" "osx-x64" "win-x86" "win-x64")

# Set APPLE_CODESIGN_ID to enable macOS code signing, e.g.:
#   export APPLE_CODESIGN_ID="Developer ID Application: Your Name (TEAMID)"
APPLE_CODESIGN_ID=${APPLE_CODESIGN_ID:-""}

cd Dink
dotnet publish -c Release -o ../dist/dll
cd ..

for target in "${targets[@]}"; do
    cd DinkCompiler
    dotnet publish -c Release -r ${target} -o ../dist/${target}
    cd ..

    cd DinkVoiceExport
    dotnet publish -c Release -r ${target} -o ../dist/${target}
    cd ..

    cd DinkViewer
    dotnet publish -c Release -r ${target} -o ../dist/${target}
    cd ..

    rm ./dist/${target}/*.pdb
    cp ./dist/dll/Dink.dll ./dist/${target}
    cp ../LICENSE ./dist/${target}
    cp ../README.md ./dist/${target}

    if [[ "${target}" == osx-* ]] && [[ -n "${APPLE_CODESIGN_ID}" ]]; then
        echo "Signing macOS binaries for ${target}..."
        for binary in DinkCompiler DinkVoiceExport DinkViewer; do
            codesign --sign "${APPLE_CODESIGN_ID}" \
                     --entitlements entitlements.plist \
                     --options runtime \
                     --timestamp \
                     --force \
                     "./dist/${target}/${binary}"
        done
        echo "Verifying signatures..."
        for binary in DinkCompiler DinkVoiceExport DinkViewer; do
            codesign --verify --strict "./dist/${target}/${binary}"
        done
    fi

    cd ./dist/${target}
    zip -r "../Dink-${target}-${version}".zip .
    cd ../..

done

cd ../..