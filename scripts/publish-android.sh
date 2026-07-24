#!/usr/bin/env bash
set -Eeuo pipefail

# Run this from a clone containing .github/workflows/android.yml and the Unity build files.
# Set SET_UNITY_SECRETS=1 when the repository does not already have Unity secrets.

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"
REPO="$(gh repo view --json nameWithOwner --jq '.nameWithOwner')"
BRANCH="arena/019f9292-racing-game"
WORKFLOW="android.yml"

command -v gh >/dev/null || { echo "GitHub CLI (gh) is required." >&2; exit 1; }
gh auth status
[[ -f ".github/workflows/$WORKFLOW" ]] || {
    echo "Missing .github/workflows/$WORKFLOW. Copy the prepared Android workflow into this clone first." >&2
    exit 1
}

git switch "$BRANCH" 2>/dev/null || git switch -c "$BRANCH"

if [[ "${SET_UNITY_SECRETS:-0}" == "1" ]]; then
    : "${UNITY_EMAIL:?Export UNITY_EMAIL before running with SET_UNITY_SECRETS=1}"
    : "${UNITY_PASSWORD:?Export UNITY_PASSWORD before running with SET_UNITY_SECRETS=1}"

    LICENSE_FILE="${UNITY_LICENSE_FILE:-}"
    if [[ -z "$LICENSE_FILE" ]]; then
        for candidate in \
            "$HOME/.local/share/unity3d/Unity/Unity_lic.ulf" \
            "$HOME/.config/unity3d/Unity/Unity_lic.ulf"; do
            if [[ -f "$candidate" ]]; then LICENSE_FILE="$candidate"; break; fi
        done
    fi
    [[ -n "$LICENSE_FILE" && -f "$LICENSE_FILE" ]] || {
        echo "Unity_lic.ulf was not found. Set UNITY_LICENSE_FILE to its path." >&2
        exit 1
    }

    printf '%s' "$UNITY_EMAIL" | gh secret set UNITY_EMAIL --repo "$REPO"
    printf '%s' "$UNITY_PASSWORD" | gh secret set UNITY_PASSWORD --repo "$REPO"
    gh secret set UNITY_LICENSE --repo "$REPO" < "$LICENSE_FILE"
    echo "Uploaded Unity Actions secrets to $REPO."
else
    echo "Using existing UNITY_EMAIL, UNITY_PASSWORD, and UNITY_LICENSE repository secrets."
fi

git diff --check
git push --set-upstream origin "$BRANCH"

HEAD_SHA="$(git rev-parse HEAD)"
RUN_ID=""
for attempt in {1..36}; do
    RUN_ID="$(gh run list --repo "$REPO" --workflow "$WORKFLOW" --branch "$BRANCH" \
        --commit "$HEAD_SHA" --event push --limit 1 --json databaseId --jq '.[0].databaseId // empty')"
    [[ -n "$RUN_ID" ]] && break
    sleep 5
done

[[ -n "$RUN_ID" ]] || {
    echo "The push succeeded, but no Android workflow run appeared." >&2
    exit 1
}

echo "Watching Android workflow run $RUN_ID"
gh run watch "$RUN_ID" --repo "$REPO" --exit-status

mkdir -p artifacts/android
rm -rf artifacts/android/*
gh run download "$RUN_ID" --repo "$REPO" --name velocity-rush-android --dir artifacts/android

echo "APK downloaded to $ROOT/artifacts/android"

PR_URL="$(gh pr list --repo "$REPO" --head "$BRANCH" --base main --state open --json url --jq '.[0].url // empty')"
if [[ -z "$PR_URL" ]]; then
    PR_URL="$(gh pr create --repo "$REPO" --base main --head "$BRANCH" \
        --title "Set up Android CI build" \
        --body $'Adds a reproducible Unity Android APK workflow. The workflow generates the prototype content in batch mode, builds with Unity 2022.3.62f1, uploads the APK, and only reaches this PR step after a successful build.')"
fi

echo "Pull request: $PR_URL"
