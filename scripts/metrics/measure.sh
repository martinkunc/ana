#!/bin/sh

lizard ../../ana.react > react.txt

lizard ../../ana.Web -x"../../ana.Web/obj/*" -x"../../ana.Web/bin/*" > blazor.txt