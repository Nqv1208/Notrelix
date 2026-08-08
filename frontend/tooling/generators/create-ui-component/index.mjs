#!/usr/bin/env node

/**
 * UI Component Generator
 *
 * Creates a new shadcn-style component in packages/ui/web.
 *
 * Usage: node index.mjs <component-name>
 */

import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = process.env.GENERATOR_ROOT ?? join(__dirname, '../../../..');

const componentName = process.argv[2];

if (!componentName) {
  console.error('Usage: node index.mjs <component-name>');
  console.error('Example: node index.mjs alert');
  process.exit(1);
}

const componentDir = join(rootDir, `packages/ui/web/src/components/ui`);
const componentFile = join(componentDir, `${componentName}.tsx`);

if (existsSync(componentFile)) {
  console.error(`Component "${componentName}" already exists at ${componentFile}`);
  process.exit(1);
}

console.log(`Creating component: ${componentName}`);

mkdirSync(componentDir, { recursive: true });

const componentContent = `import * as React from "react"
import { cn } from "../../lib/cn"

const ${componentName.charAt(0).toUpperCase() + componentName.slice(1)} = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn("${componentName}", className)}
    {...props}
  />
))
${componentName.charAt(0).toUpperCase() + componentName.slice(1)}.displayName = "${componentName.charAt(0).toUpperCase() + componentName.slice(1)}"

export { ${componentName.charAt(0).toUpperCase() + componentName.slice(1)} }
`;

writeFileSync(componentFile, componentContent);

console.log(`\nCreated component at: ${componentFile}`);
console.log('\nNext steps:');
console.log(`1. Add props and variants as needed`);
console.log(`2. Export from packages/ui/web/src/index.ts`);
console.log(`3. Add to packages/ui/web/package.json exports if needed`);
