#!/bin/bash

BASE_PLUGIN_DIR=./dist/KspCraftOrganizer

rm -rf ./dist
mkdir -p $BASE_PLUGIN_DIR/Plugins
mkdir -p $BASE_PLUGIN_DIR/icons

cp -r ./icons/*.png $BASE_PLUGIN_DIR/icons/

xbuild /p:Configuration=Release
cp ./KspCraftOrganizerPlugin/bin/Release/KspCraftOrganizerPlugin.dll $BASE_PLUGIN_DIR/Plugins

cp ./LICENSE $BASE_PLUGIN_DIR/LICENSE.txt

pushd ./dist
zip -r -X KspCraftOrganizer.zip KspCraftOrganizer
popd