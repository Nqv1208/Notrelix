/**
 * Mock API server for testing.
 *
 * Can be used with MSW (Mock Service Worker) or similar.
 */

export interface MockEndpoint {
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  path: string;
  response: unknown;
  status?: number;
}

export function createMockServer(endpoints: MockEndpoint[]) {
  return {
    endpoints,
    getEndpoint(method: string, path: string) {
      return endpoints.find(
        (e) => e.method === method && e.path === path,
      );
    },
  };
}

export const mockEndpoints: MockEndpoint[] = [
  {
    method: 'GET',
    path: '/api/v1/auth/profile',
    response: {
      id: 'user-1',
      email: 'test@example.com',
      name: 'Test User',
      avatarUrl: null,
    },
  },
  {
    method: 'GET',
    path: '/api/v1/workspaces',
    response: [
      {
        id: 'ws-1',
        name: 'Test Workspace',
        slug: 'test-workspace',
        plan: 'free',
      },
    ],
  },
];
