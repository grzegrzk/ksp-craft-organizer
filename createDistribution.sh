#!/bin/bash

rm -rf ./dist
mkdir -p ./dist/KspCraftOrganizerPlugin/Plugins
mkdir -p ./dist/KspCraftOrganizerPlugin/icons

cp -r ./icons/*.png ./dist/KspCraftOrganizerPlugin/icons/

xbuild /p:Configuration=Release
cp ./KspCraftOrganizerPlugin/bin/Release/KspCraftOrganizerPlugin.dll ./dist/KspCraftOrganizerPlugin/Plugins

pushd ./dist
zip -r KspCraftOrganizerPlugin.zip KspCraftOrganizerPlugin
popd