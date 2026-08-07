export type RealtimeControlMessage =
  | {
      readonly type: 'ping';
      readonly sentAt: string;
    }
  | {
      readonly type: 'pong';
      readonly sentAt: string;
    }
  | {
      readonly type: 'subscribed';
      readonly subscriptionId: string;
    }
  | {
      readonly type: 'subscription-error';
      readonly subscriptionId?: string;
      readonly code: string;
      readonly message?: string;
    };
