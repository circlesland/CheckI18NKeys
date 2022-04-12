#!/bin/sh -l
./CheckI18NKeys --source-dir $GITHUB_WORKSPACE --master-json $1 -types $2 --default-language-prefix $3