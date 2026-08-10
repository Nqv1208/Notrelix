export function isPublicExportViolation(importPath: string): boolean {
  return /^@notrelix\/[^/]+\/src\//.test(importPath);
}
