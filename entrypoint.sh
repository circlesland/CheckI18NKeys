#!/bin/sh -l 
/app/CheckI18NKeys \
  --source-dir /github/workspace/ \
  --master-json ${1} \
  --file-types ${2} \
  --default-language-prefix ${3}