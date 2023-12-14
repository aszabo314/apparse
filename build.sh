#!/bin/bash

dotnet tool restore
dotnet paket restore
dotnet build src/apparse/apparse.fsproj