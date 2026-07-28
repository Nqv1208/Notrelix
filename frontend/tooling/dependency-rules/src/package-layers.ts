export type PackageLayer = 'foundation' | 'feature' | 'product' | 'runtime' | 'app' | 'tooling';

export function getPackageLayer(packageName: string, relativePath: string): PackageLayer {
  if (relativePath.startsWith('apps/')) {
    return 'app';
  }
  if (relativePath.startsWith('tooling/')) {
    return 'tooling';
  }
  if (relativePath.startsWith('packages/foundation/')) {
    return 'foundation';
  }
  if (relativePath.startsWith('packages/features/')) {
    return 'feature';
  }
  if (relativePath.startsWith('packages/product/')) {
    return 'product';
  }
  if (relativePath.startsWith('packages/runtimes/')) {
    return 'runtime';
  }
  return 'tooling';
}
