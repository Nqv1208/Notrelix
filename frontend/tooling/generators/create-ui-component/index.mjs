#!/usr/bin/env node

/**
 * UI Component Generator
 *
 * Creates a new UI primitive with public export, unit test, and (for web)
 * a Storybook story, honoring the UI system spec (07-UI-SYSTEM-SPEC):
 * public imports only in stories, component + a11y/unit test coverage.
 *
 *   --target web     (default) packages/ui/web/src/components/ui/<name>.tsx
 *                    + barrel export + unit test + Storybook story
 *   --target mobile  packages/ui/mobile/src/components/<name>.ts (contract)
 *                    + barrel export + unit test
 *
 * Usage: node index.mjs <component-name> [--target web|mobile]
 */

import { mkdirSync, writeFileSync, existsSync, readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = process.env.GENERATOR_ROOT ?? join(__dirname, "../../../..");

const args = process.argv.slice(2);
const componentName = args.find((a) => !a.startsWith("--"));
const targetIndex = args.indexOf("--target");
const target = targetIndex !== -1 ? args[targetIndex + 1] : "web";

if (!componentName) {
  console.error("Usage: node index.mjs <component-name> [--target web|mobile]");
  console.error("Example: node index.mjs alert --target web");
  process.exit(1);
}

if (!["web", "mobile"].includes(target)) {
  console.error(`Invalid --target "${target}"; expected web or mobile`);
  process.exit(1);
}

const PascalName = componentName
  .split("-")
  .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
  .join("");
console.log(`Creating UI component: ${componentName} (target: ${target})`);

function appendExport(indexPath, exportLine) {
  const source = existsSync(indexPath) ? readFileSync(indexPath, "utf8") : "";
  if (source.includes(exportLine)) return;
  const updated = source.replace(/\s*$/, "\n") + exportLine + "\n";
  writeFileSync(indexPath, updated);
}

if (target === "web") {
  const componentDir = join(rootDir, "packages/ui/web/src/components/ui");
  const componentFile = join(componentDir, `${componentName}.tsx`);
  const testFile = join(componentDir, "__tests__", `${componentName}.test.tsx`);
  const storyFile = join(
    rootDir,
    "tooling/storybook/web/stories",
    `${componentName}.stories.tsx`,
  );
  const indexPath = join(rootDir, "packages/ui/web/src/index.ts");

  if (existsSync(componentFile)) {
    console.error(
      `Component "${componentName}" already exists at ${componentFile}`,
    );
    process.exit(1);
  }

  mkdirSync(componentDir, { recursive: true });
  mkdirSync(join(componentDir, "__tests__"), { recursive: true });
  mkdirSync(dirname(storyFile), { recursive: true });

  writeFileSync(
    componentFile,
    `import * as React from "react"
import { cn } from "~/lib/cn"

const ${PascalName} = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn("${componentName}", className)}
    {...props}
  />
))
${PascalName}.displayName = "${PascalName}"

export { ${PascalName} }
`,
  );

  writeFileSync(
    testFile,
    `import { describe, expect, it } from 'vitest';
import { ${PascalName} } from '../${componentName}';

describe('${PascalName}', () => {
  it('is exported with a stable displayName', () => {
    expect(${PascalName}.displayName).toBe('${PascalName}');
  });

  it('is a renderable component', () => {
    expect(typeof ${PascalName}).toBe('object');
  });
});
`,
  );

  writeFileSync(
    storyFile,
    `import type { Meta, StoryObj } from '@storybook/react';
import { ${PascalName} } from '@notrelix/ui-web';

const meta = {
  title: 'Components/${PascalName}',
  component: ${PascalName},
  parameters: { layout: 'centered' },
} satisfies Meta<typeof ${PascalName}>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
};
`,
  );

  appendExport(
    indexPath,
    `export { ${PascalName} } from "./components/ui/${componentName}"`,
  );

  console.log(`\nCreated component at: ${componentFile}`);
  console.log(`Unit test: ${testFile}`);
  console.log(`Storybook story: ${storyFile}`);
  console.log(`Barrel export updated: ${indexPath}`);
} else {
  const componentDir = join(rootDir, "packages/ui/mobile/src/components");
  const componentFile = join(componentDir, `${componentName}.tsx`);
  const testFile = join(
    rootDir,
    "packages/ui/mobile/src/__tests__",
    `${componentName}.mobile.test.ts`,
  );
  const indexPath = join(rootDir, "packages/ui/mobile/src/index.ts");

  if (existsSync(componentFile)) {
    console.error(
      `Component "${componentName}" already exists at ${componentFile}`,
    );
    process.exit(1);
  }

  mkdirSync(componentDir, { recursive: true });
  mkdirSync(dirname(testFile), { recursive: true });

  writeFileSync(
    componentFile,
    `import React from "react";
import { View, Text, StyleSheet } from "react-native";

export interface ${PascalName}Props {
  readonly id?: string;
  readonly title?: string;
}

export function ${PascalName}({ title }: ${PascalName}Props): React.ReactNode {
  return (
    <View style={styles.container}>
      <Text style={styles.text}>{title ?? "${PascalName}"}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    padding: 12,
    backgroundColor: "#f3f4f6",
    borderRadius: 8,
  },
  text: {
    fontSize: 14,
    color: "#111827",
  },
});
`,
  );

  writeFileSync(
    testFile,
    `import { describe, expect, it } from 'vitest';
import { ${PascalName} } from '../components/${componentName}';

describe('${PascalName} component', () => {
  it('is a renderable React component function', () => {
    expect(typeof ${PascalName}).toBe('function');
  });
});
`,
  );

  appendExport(
    indexPath,
    `export { ${PascalName}, type ${PascalName}Props } from "./components/${componentName}"`,
  );

  console.log(`\nCreated mobile component at: ${componentFile}`);
  console.log(`Mobile test: ${testFile}`);
  console.log(`Barrel export updated: ${indexPath}`);
}
