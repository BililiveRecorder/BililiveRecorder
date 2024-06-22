#!/bin/bash

set -e

umask $UMASK

usermod -u $PUID user
groupmod -g $PGID users

chown -R user:users /app
chown -R user:users /rec

export HOME=/home/user

exec /usr/local/bin/gosu user dotnet /app/BililiveRecorder.Cli.dll $@
