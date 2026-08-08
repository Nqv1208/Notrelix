import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptPath = join(dirname(fileURLToPath(import.meta.url)), '../../../../scripts/freeze-audit.mjs');
const source = readFileSync(scriptPath, 'utf8');

describe('FREEZE freeze:audit script contract', () => {
  it('defines the mandatory gates in the spec order', () => {
    const order = [
      'codegen:check',
      'check:architecture',
      'check:architecture-docs',
      'typecheck',
      'lint',
      'test:node',
      'test:web',
      'test:mobile',
      'test:integration',
      'test:generators',
      'test:fanout',
      'build',
      'e2e',
    ];

    for (const script of order) {
      const index = source.indexOf(`script: '${script}'`);
      expect(index, `missing gate ${script}`).toBeGreaterThan(-1);
    }

    const positions = order.map((s) => source.indexOf(`script: '${s}'`));
    for (let i = 1; i < positions.length; i++) {
      expect(positions[i], `gate ${order[i]} out of order`).toBeGreaterThan(positions[i - 1]);
    }
  });

  it('exits non-zero on failure and missing gates', () => {
    expect(source).toMatch(/process\.exit\(1\)/);
    expect(source).toMatch(/VERDICT: NOT FROZEN/);
    expect(source).toMatch(/MISSING/);
  });

  it('prints PASS/FAIL per gate and a final verdict', () => {
    expect(source).toMatch(/status: 'PASS'/);
    expect(source).toMatch(/status: 'FAIL'/);
    expect(source).toMatch(/VERDICT: FROZEN/);
  });

  it('creates no hidden artifact, certificate, docs or git mutation', () => {
    expect(source).not.toMatch(/writeFileSync|mkdirSync|appendFileSync|createWriteStream/);
    expect(source).not.toMatch(/\.freeze-artifacts|last-audit-result\.json|cert\.json/i);
    expect(source).not.toMatch(/git (add|commit|checkout|reset)/);
  });
});
