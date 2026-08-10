#!/usr/bin/env node
/**
 * FE-RF-11: Production Startup Smoke Script
 * Validates that production build output boots cleanly without local environment fallbacks
 * and scans emitted dist bundle for forbidden localhost URLs.
 */

import { parseEnv } from "../packages/foundation/kernel/dist/index.js";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const distDir = join(__dirname, "../apps/web/dist");

function validateProductionConfig() {
  const prodEnv = {
    mode: "production",
    apiUrl: "https://api.notrelix.com/api/v1",
    realtimeUrl: "wss://realtime.notrelix.com/realtime",
    appUrl: "https://app.notrelix.com",
    releaseSha: "test-release-sha-12345",
    mockApi: false,
  };

  try {
    const resolved = parseEnv(prodEnv);
    if (!resolved.isProduction) {
      throw new Error("Expected isProduction to be true");
    }
    console.log("✅ Production startup config validation passed.");
  } catch (err) {
    console.error("❌ Production startup config validation failed:", err);
    process.exit(1);
  }
}

function scanDistBundle() {
  if (!existsSync(distDir)) {
    console.log(
      "ℹ️ dist directory not present yet, skipping bundle string scan.",
    );
    return;
  }

  const forbiddenStrings = ["localhost:5000", "localhost:8000"];
  let violationCount = 0;

  function walk(dir) {
    for (const entry of readdirSync(dir)) {
      const full = join(dir, entry);
      if (statSync(full).isDirectory()) {
        walk(full);
      } else if (full.endsWith(".js")) {
        const content = readFileSync(full, "utf8");
        for (const pattern of forbiddenStrings) {
          if (content.includes(pattern)) {
            console.error(
              `❌ Bundle scan violation in ${full}: found forbidden string "${pattern}"`,
            );
            violationCount++;
          }
        }
      }
    }
  }

  walk(distDir);

  if (violationCount > 0) {
    console.error(`❌ Bundle scan failed with ${violationCount} violation(s).`);
    process.exit(1);
  } else {
    console.log("✅ Production bundle scan clean.");
  }
}

validateProductionConfig();
scanDistBundle();
