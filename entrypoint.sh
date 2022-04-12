#!/bin/sh -l
chmod +x ./app/CheckI18NKeys 
/app/CheckI18NKeys \
  --source-dir ${GITHUB_WORKSPACE} \
  --master-json ${GITHUB_WORKSPACE}${1} \
  --file-types ${2} \
  --default-language-prefix ${3}