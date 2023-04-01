#!/bin/bash

if [ -z "$1" ]; then
    echo "Tag not supplied. Image won't be published."

    docker buildx build --platform linux/amd64,linux/arm64 -t faas-idler .
else
    docker buildx build --push --platform linux/amd64,linux/arm64 -t goncalooliveira/faas-idler:$1 .
fi
