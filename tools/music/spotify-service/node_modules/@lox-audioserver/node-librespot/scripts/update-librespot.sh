#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_dir="${script_dir}/../librespot-dev"
repo_url="https://github.com/librespot-org/librespot.git"

if [[ ! -d "${repo_dir}/.git" ]]; then
  mkdir -p "${repo_dir}"
  git clone "${repo_url}" "${repo_dir}"
else
  git -C "${repo_dir}" pull --ff-only
fi
