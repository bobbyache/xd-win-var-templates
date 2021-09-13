#!/bin/bash

dotnet publish -c release
rm -rf /C/Programs/XdTemplatesConsole/*
cp -rf XdTemplatesConsole/bin/Release/net5.0/publish/* /C/Programs/XdTemplatesConsole/