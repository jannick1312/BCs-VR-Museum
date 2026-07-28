#!/bin/bash
set -euo pipefail

SESSION="vitrivr"

echo "Stopping tmux session '$SESSION'..."

if tmux has-session -t "$SESSION" 2>/dev/null; then
    tmux kill-session -t "$SESSION"
    echo "Session stopped."
else
    echo "No tmux session '$SESSION' found."
fi

echo "Stopping Xvfb display :1..."

pkill -f "Xvfb :1" 2>/dev/null || true

echo "Resetting PostgreSQL..."

PGPASSWORD=password psql -h localhost -U postgres -d postgres <<EOF
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
CREATE EXTENSION IF NOT EXISTS vector;
EOF

echo "Reset done."