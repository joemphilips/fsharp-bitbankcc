#!/usr/bin/env bash
set -u

readonly VERSION="1.0"
if [[ "$(uname)" == 'Darwin' ]]; then
  readonly SCRIPT_DIR_PATH=$(dirname $(greadlink -f $0))
else
  readonly SCRIPT_DIR_PATH=$(dirname $(readlink -f $0))
fi

cd $SCRIPT_DIR_PATH/BitBankApi

dotnet pack -c Release --include-symbols -p:SymbolPackageFormat=snupkg
dotnet nuget push bin/Release/BitBankApi.1.0.2.nupkg -k ${NUGET_API_KEY_BITBANK} -s https://api.nuget.org/v3/index.json
