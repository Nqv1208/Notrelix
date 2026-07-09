import { Link } from '@tanstack/react-router';

export function ForgotPasswordPage() {
  return (
    <div className="min-h-screen flex items-center justify-center p-8">
      <div className="w-full max-w-md">
        <div className="flex flex-col gap-7">
          <div>
            <h1 className="text-2xl font-bold tracking-tight mb-1.5">Forgot password?</h1>
            <p className="text-muted-foreground text-[15px]">
              Enter your email and we&apos;ll send you a reset link
            </p>
          </div>
          <form className="space-y-4">
            <div className="space-y-2">
              <label htmlFor="email" className="text-sm font-medium">Email</label>
              <input
                id="email"
                type="email"
                placeholder="you@company.com"
                className="flex h-10 w-full rounded-md border border-input bg-input px-3 py-2 text-sm"
              />
            </div>
            <button
              type="submit"
              className="inline-flex items-center justify-center h-10 w-full rounded-md bg-primary text-primary-foreground hover:bg-primary/90 font-medium"
            >
              Send reset link
            </button>
          </form>
        </div>
        <p className="mt-8 text-center text-sm text-muted-foreground">
          Remember your password?{' '}
          <Link to="/sign-in" className="font-medium text-foreground hover:text-primary transition-colors underline-offset-4 hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
