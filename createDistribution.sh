#!/bin/bash

rm -rf ./dist
mkdir -p ./dist/KspCraftOrganizer/Plugins
mkdir -p ./dist/KspCraftOrganizer/icons

cp -r ./icons/*.png ./dist/KspCraftOrganizer/icons/

xbuild /p:Configuration=Release
cp ./KspCraftOrganizerPlugin/bin/Release/KspCraftOrganizerPlugin.dll ./dist/KspCraftOrganizer/Plugins

pushd ./dist
zip -r KspCraftOrganizer.zip KspCraftOrganizer
popd