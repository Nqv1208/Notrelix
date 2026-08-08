import { Outlet } from '@tanstack/react-router';
import { RealtimeLifecycle } from '../providers/realtime-lifecycle';

export function RootLayout() {
  return (
    <div className="min-h-screen bg-background">
      <RealtimeLifecycle>
        <Outlet />
      </RealtimeLifecycle>
    </div>
  );
}
