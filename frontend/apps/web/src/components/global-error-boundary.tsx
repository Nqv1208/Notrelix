import { Component, type ReactNode, type ErrorInfo } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { Button } from '@notrelix/ui-web';

interface Props {
  children: ReactNode;
  telemetry?: {
    reportError(error: unknown, context?: Record<string, unknown>): void;
  };
  releaseSha?: string;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class GlobalErrorBoundary extends Component<Props, State> {
  public override state: State = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public override componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    this.props.telemetry?.reportError(error, {
      componentStack: errorInfo.componentStack,
      releaseSha: this.props.releaseSha ?? 'unknown',
      route: typeof window !== 'undefined' ? window.location.pathname : 'unknown',
    });
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null });
    window.location.reload();
  };

  public override render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-background p-6">
          <div className="max-w-md w-full p-6 border rounded-xl shadow-lg bg-card text-card-foreground space-y-4 text-center">
            <div className="mx-auto w-12 h-12 rounded-full bg-destructive/10 text-destructive flex items-center justify-center">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <h2 className="text-xl font-semibold tracking-tight">Something went wrong</h2>
            <p className="text-sm text-muted-foreground">
              An unexpected application error occurred. We have logged the error details.
            </p>
            {this.state.error && (
              <pre className="text-xs bg-muted p-3 rounded-lg text-left overflow-auto max-h-32 font-mono">
                {import.meta.env.PROD
                  ? 'Error details are redacted in production. Check the browser console.'
                  : this.state.error.message}
              </pre>
            )}
            <Button onClick={this.handleReset} className="w-full flex items-center justify-center gap-2">
              <RefreshCw className="w-4 h-4" />
              Reload Application
            </Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
