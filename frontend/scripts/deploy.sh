#!/bin/bash

# Notrelix Frontend Deploy Script
# Usage: ./scripts/deploy.sh [environment]

set -e

ENVIRONMENT=${1:-staging}

echo "🚀 Notrelix Frontend Deploy"
echo "==========================="
echo "Environment: $ENVIRONMENT"
echo ""

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(staging|production)$ ]]; then
  echo "❌ Invalid environment: $ENVIRONMENT"
  echo "   Use: staging or production"
  exit 1
fi

# Run validation first
echo "🔍 Running pre-deploy validation..."
pnpm validate

# Build apps
echo ""
echo "📦 Building apps..."
pnpm build

# Deploy based on environment
case $ENVIRONMENT in
  staging)
    echo ""
    echo "📤 Deploying to staging..."
    # Vercel deployment
    cd apps/marketing && vercel --prod --yes && cd ../..
    cd apps/web && vercel --prod --yes && cd ../..
    ;;
  production)
    echo ""
    echo "📤 Deploying to production..."
    # Vercel deployment
    cd apps/marketing && vercel --prod --yes && cd ../..
    cd apps/web && vercel --prod --yes && cd ../..
    ;;
esac

echo ""
echo "✅ Deploy complete!"
