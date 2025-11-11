#!/bin/bash

rm -rf ./dist/*

version="0.0.0.1"
targets=("osx-arm64" "osx-x64" "win-x86" "win-x64")

cd Dink
dotnet publish -c Release -o ../dist/dll
cd ..

for target in "${targets[@]}"; do
    cd DinkCompiler
    dotnet publish -c Release -r ${target} -o ../dist/${target}
    cd ..

    rm ./dist/${target}/*.pdb
    cp ./dist/dll/Dink.dll ./dist/${target}
    cp ../LICENSE ./dist/${target}
    cp ../README.md ./dist/${target}

    cd ./dist/${target}
    zip -r "../DinkCompiler-${target}-${version}".zip .
    cd ../..

done

cd ../..