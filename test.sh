#! /bin/bash
echo HEAD_BRANCH=${HEAD_BRANCH}
if ! [[ ${HEAD_BRANCH} =~ ^v[0-9]+.[0-9]+.[0-9]+(-[[:alnum:].-]+)?(\+[[:alnum:].-]+)?$ ]]; then
    echo "::error::Revision ref doesn't match semantic version format for tags: v{MAJOR}.{MINOR}.{PATCH} or v{MAJOR}.{MINOR}.{PATCH}-{TAG}+{BUILD}."
    exit 1
fi