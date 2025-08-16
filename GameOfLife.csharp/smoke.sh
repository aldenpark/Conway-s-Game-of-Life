#!/usr/bin/env bash
#
# smoke.sh — end-to-end smoke test for the Conway Life API
# Fix: tolerate different JSON casings for board id (boardId | board_id | id)
#
# Usage:
#   chmod +x smoke.sh
#   ./smoke.sh
#
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
H="${H:-10}"
W="${W:-10}"
DENSITY="${DENSITY:-0.25}"
WRAP="${WRAP:-true}"
N="${N:-5}"
MAX_ITERS="${MAX_ITERS:-10000}"

LAST_BODY=""
LAST_CODE=""

say() { printf "\n==> %s\n" "$*"; }
err() { printf "ERROR: %s\n" "$*" >&2; exit 1; }

call() {
  local method="$1"; shift
  local path="$1"; shift
  local data="${1-}"

  say "$method $path"
  local url="${BASE_URL}${path}"

  local resp
  if [[ -n "$data" ]]; then
    resp="$(curl -sS -X "$method" "$url" \
      -H 'Content-Type: application/json' \
      -d "$data" \
      -w $'\n__HTTP__%{http_code}')" || true
  else
    resp="$(curl -sS -X "$method" "$url" \
      -w $'\n__HTTP__%{http_code}')" || true
  fi

  LAST_CODE="${resp##*__HTTP__}"
  LAST_BODY="${resp%$'\n'__HTTP__*}"

  if command -v jq >/dev/null 2>&1; then
    echo "$LAST_BODY" | jq . 2>/dev/null || echo "$LAST_BODY"
  else
    echo "$LAST_BODY"
  fi
  echo "HTTP $LAST_CODE"
}

extract_board_id() {
  local body="$1"
  local id=""

  if command -v jq >/dev/null 2>&1; then
    # Try common casings: board_id, boardId, id
    id="$(echo "$body" | jq -r '.board_id // .boardId // .id // empty')"
  fi

  if [[ -z "$id" ]]; then
    # Fallback regex without jq
    id="$(echo "$body" | sed -n 's/.*"\(board_id\|boardId\|id\)"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\2/p' | head -n1)"
  fi

  echo -n "$id"
}

# --- start ---

say "Health check"
call GET /health
[[ "$LAST_CODE" == "200" ]] || err "Health check failed"

say "Create board (random)"
create_json=$(printf '{"height":%d,"width":%d,"density":%s,"wrap":%s}' "$H" "$W" "$DENSITY" "$WRAP")
call POST /boards "$create_json"
[[ "$LAST_CODE" == "200" || "$LAST_CODE" == "201" ]] || err "Create failed"

BOARD_ID="$(extract_board_id "$LAST_BODY")"
[[ -n "$BOARD_ID" ]] || err "Could not extract board_id from create response"
echo "BOARD_ID=$BOARD_ID"

say "Get current state"
call GET "/boards/${BOARD_ID}"
[[ "$LAST_CODE" == "200" ]] || err "GET /boards/{id} failed"

say "Next generation"
call GET "/boards/${BOARD_ID}/next"
[[ "$LAST_CODE" == "200" ]] || err "/next failed"

say "Advance N generations (n=$N)"
call GET "/boards/${BOARD_ID}/advance?n=${N}"
[[ "$LAST_CODE" == "200" ]] || err "/advance failed"

say "Final state (max_iters=$MAX_ITERS)"
call GET "/boards/${BOARD_ID}/final?max_iters=${MAX_ITERS}"
case "$LAST_CODE" in
  200) echo "Final reached (fixed or cycle)";;
  422) echo "No fixed/cycle conclusion within max_iters (valid outcome)";;
  *)   err "/final unexpected status: $LAST_CODE";;
esac

say "Done. Tested: /health, POST /boards, GET /boards/{id}, /next, /advance, /final"
