# Check svelte-i18n keys

Find misspelled or undefined i18n-keys in a svelte-project.  

## Description
The program searches for occurrences of svelte-i18n keys in a source directory. 
Each key is checked against a master-json file and undefined keys are reported as text or json.

The default exit code is `0` but the program exits with code `99` if there are any undefined keys.

It can find occurrences of the following key usages:
* .svelte-Files:  
  ```html
  <script lang="js|ts">
    import { _ } from "svelte-i18n";
  
    let a = window.i18n("shared.apiConnection.errors.norMoreThanOneDefinitions");
    let b = i18n("shared.apiConnection.errors.norMoreThanOneDefinitions");
    throw new Error(window.i18n("shared.apiConnection.errors.connectionError", { values: { error: result.errors.map((o) => o.message).join("\n")}}));
  </script>
  <div class="mt-4 text-3xl font-heading text-dark">
    {$_("dapps.o-dashboard.pages.home.passport")}
  </div> 
  ```
* .ts-Files:
  ```ts
  let a = window.i18n("shared.apiConnection.errors.norMoreThanOneDefinitions");
  let b = i18n("shared.apiConnection.errors.norMoreThanOneDefinitions");
  throw new Error(window.i18n("shared.apiConnection.errors.connectionError", { values: { error: result.errors.map((o) => o.message).join("\n")}}));
  ```
* .js-Files:
  ```js
  let a = window.i18n("shared.apiConnection.errors.norMoreThanOneDefinitions");
  let b = i18n("shared.apiConnection.errors.norMoreThanOneDefinitions");
  throw new Error(window.i18n("shared.apiConnection.errors.connectionError", { values: { error: result.errors.map((o) => o.message).join("\n")}}));
  ```
  
## Use as github action
### Example
```yaml
name: check svelte-18n keys
on:
  push:
    branches: [ dev ]
  pull_request:
    branches: [ dev ]
jobs:
  check-svelte-i18n-keys:
    runs-on: ubuntu-latest
    steps:
    - name: checkout
      uses: actions/checkout@v1
    - name: Check svelte-i18n-keys
      uses: circlesland/CheckI18NKeys@v0.1.4-dev
      with:
        master_json: './i18n/lang/en.json'
        file_types: 'ts,svelte'
        default_language_prefix: 'en.'
```

## Use as cmd utility
```
Usage:
  CheckI18NKeys [options]

Options:
  -src, --source-dir <source-dir> (REQUIRED)                  The source code directory of the svelte-i18n app to examine.
  -master, --master-json <master-json> (REQUIRED)             The path to an authoritative json i18n file that contains all keys.
  -types, --file-types <file-types> (REQUIRED)                A comma separated list of file types to include. Options are: js,ts,svelte
  -lang, --default-language-prefix <default-language-prefix>  The language of the master json file followed by a dot. Example: 'en.' or 'de.'
  --fix                                                       Applies all suggested fixes if set.
  -json, --output-json                                        Output a json object with the results when the process finished.
  --version                                                   Show version information
  -?, -h, --help                                              Show help and usage information
```

### Examples
#### Master json
The master.json file can be structured in two ways. It can either be nested or flat.  
⚠️ Only one language is supported in the master json.

##### Nested
```json
{
  "en": {
    "common": {
      "trusted": "trusted",
      "untrusted": "untrusted",
      "you": "you",
      "tokens": "tokens",
      "date": "Date",
      "from": "from",
      "to": "to",
      "block": "Block",
      "yes": "Yes",
      "no": "No"
    },
    "dapps": {
      "o-banking": {
        ...
      }
    }
  }
}
```
##### Flat
```json

{
  "en.common.trusted": "trusted",
  "en.common.untrusted": "untrusted",
  "en.common.you": "you",
  "en.dapps.o-banking.transfer": "transfer",
  ...
}
```
#### Invocations
##### Find undefined keys and output suggested fixes
```shell
./CheckI18NKeys \
  --source-dir /home/user/src/my-project/src \
  --master-json /home/user/src/my-project/i18n/en.json \
  --default-language-prefix en. \
  -types ts,svelte
```

##### Output JSON
```shell
./CheckI18NKeys \
  --source-dir /home/user/src/my-project/src \
  --master-json /home/user/src/my-project/i18n/en.json \
  --default-language-prefix en. \
  -types svelte
  -json
```

##### Include/exclude file types
You can pass a list of the following file types to the `-types` option.  
Supported types are: `js`, `ts` and `svelte`.
```shell
./CheckI18NKeys \
  --source-dir /home/user/src/my-project/src \
  --master-json /home/user/src/my-project/i18n/en.json \
  --default-language-prefix en. \
  -types js,ts,svelte
```

##### Apply suggested fixes
⚠️ Passing the `--fix` switch will apply all suggested fixes in the file system.
```shell
./CheckI18NKeys \
  --source-dir /home/user/src/my-project/src \
  --master-json /home/user/src/my-project/i18n/en.json \
  --default-language-prefix en. \
  -types svelte \
  --fix
```

## License
The MIT License  

Copyright 2022 - Earth Circle DAO

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.