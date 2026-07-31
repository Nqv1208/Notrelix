#!/bin/bash

# Notrelix Frontend Setup Script
# This script sets up the development environment

set -e

echo "🚀 Notrelix Frontend Setup"
echo "=========================="

# Check Node.js version
NODE_VERSION=$(node -v | cut -d'v' -f2 | cut -d'.' -f1)
if [ "$NODE_VERSION" -lt 22 ]; then
  echo "❌ Node.js 22+ required. Current: $(node -v)"
  echo "   Run: nvm use 22 or fnm use 22"
  exit 1
fi
echo "✅ Node.js $(node -v)"

# Check pnpm
if ! command -v pnpm &> /dev/null; then
  echo "❌ pnpm not found. Installing..."
  npm install -g pnpm
fi
echo "✅ pnpm $(pnpm -v)"

# Enable corepack
corepack enable
echo "✅ Corepack enabled"

# Install dependencies
echo ""
echo "📦 Installing dependencies..."
pnpm install

# Run validation
echo ""
echo "🔍 Running validation..."
pnpm typecheck
pnpm lint
pnpm test
pnpm check:deps

echo ""
echo "✅ Setup complete!"
echo ""
echo "Next steps:"
echo "  pnpm dev:web        - Start web app (http://localhost:5173)"
echo "  pnpm dev:marketing  - Start marketing app (http://localhost:3000)"
echo "  pnpm build          - Build all apps"
echo "  pnpm test           - Run all tests"
