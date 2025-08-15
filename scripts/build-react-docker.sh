#! /bin/bash

pushd ana.react > /dev/null

docker build -t ana-react .

popd > /dev/null