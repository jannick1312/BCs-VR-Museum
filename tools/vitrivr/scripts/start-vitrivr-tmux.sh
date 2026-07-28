#!/bin/bash
set -euo pipefail

SESSION="vitrivr"
ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"

DESCRIPTOR_DIR="$ROOT_DIR/vitrivr-python-descriptor-server"
ENGINE_DIR="$ROOT_DIR/vitrivr-engine"

if tmux has-session -t "$SESSION" 2>/dev/null; then
    echo "tmux session '$SESSION' already exists."
    echo "Attach with:"
    echo "  tmux attach -t $SESSION"
    exit 0
fi

echo "Starting descriptor server in tmux session '$SESSION'..."

tmux new-session -d -s "$SESSION" -n descriptor \
    "cd '$DESCRIPTOR_DIR' && bash startup.sh; echo 'Descriptor exited'; exec bash"

echo "Waiting for descriptor server health check..."

until curl -sf http://localhost:8888/health >/dev/null; do
    sleep 1
done

echo "Descriptor server is ready."

echo "Starting virtual display..."

if ! DISPLAY=:1 xdpyinfo >/dev/null 2>&1; then
    Xvfb :1 -screen 0 1280x720x24 >/tmp/vitrivr-xvfb.log 2>&1 &
    sleep 2
fi

DISPLAY=:1 xdpyinfo >/dev/null

echo "Starting vitrivr-engine..."

tmux new-window -t "$SESSION" -n engine \
    "cd '$ENGINE_DIR' && ./vitrivr-engine-server/build/install/vitrivr-engine-server/bin/vitrivr-engine-server; echo 'Engine exited'; exec bash"

echo ""
echo "Started."
echo "Attach with:"
echo "  tmux attach -t $SESSION"
echo ""
echo "Detach inside tmux with:"
echo "  Ctrl+B then D"
echo ""

tmux attach -t "$SESSION"