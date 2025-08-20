#! /bin/bash

pushd ana.react > /dev/null

docker build -t ana-react -f Dockerfile.prod .

popd > /dev/null