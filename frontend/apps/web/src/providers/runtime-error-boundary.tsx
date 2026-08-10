import React, { Component, type ReactNode, type ErrorInfo } from "react";
import { useAppRuntime, type AppRuntime } from "@notrelix/runtime-web";

interface Props {
  readonly children: ReactNode;
  readonly runtime: AppRuntime;
}

interface State {
  readonly hasError: boolean;
  readonly error: unknown | null;
}

class InnerRuntimeErrorBoundary extends Component<Props, State> {
  public override state: State = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: unknown): State {
    return { hasError: true, error };
  }

  public override componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    this.props.runtime.telemetry.reportError(error, {
      componentStack: errorInfo.componentStack,
    });
  }

  public override render(): ReactNode {
    if (this.state.hasError) {
      const isProduction = this.props.runtime.env.isProduction;

      return (
        <div
          style={{
            padding: "2rem",
            textAlign: "center",
            fontFamily: "sans-serif",
          }}
        >
          <h2>Something went wrong</h2>
          <p>
            An unexpected error occurred. Please refresh the page or try again
            later.
          </p>
          {!isProduction && this.state.error instanceof Error && (
            <pre
              style={{
                textAlign: "left",
                background: "#f5f5f5",
                padding: "1rem",
                overflow: "auto",
              }}
            >
              {this.state.error.stack}
            </pre>
          )}
        </div>
      );
    }

    return this.props.children;
  }
}

export function RuntimeErrorBoundary({ children }: { children: ReactNode }) {
  const runtime = useAppRuntime();
  return (
    <InnerRuntimeErrorBoundary runtime={runtime}>
      {children}
    </InnerRuntimeErrorBoundary>
  );
}
