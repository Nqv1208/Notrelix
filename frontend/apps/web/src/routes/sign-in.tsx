import { Link } from '@tanstack/react-router';

export function SignInPage() {
  return (
    <div className="min-h-screen flex">
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-violet-600 to-indigo-700 items-center justify-center p-12">
        <div className="text-white max-w-md">
          <h1 className="text-4xl font-bold mb-4">Notrelix</h1>
          <p className="text-xl text-white/90">
            Write like Notion. Plan like Trello. Ship like a pro.
          </p>
        </div>
      </div>
      <div className="flex-1 flex items-center justify-center p-8">
        <div className="w-full max-w-md">
          <div className="flex flex-col gap-7">
            <div>
              <h1 className="text-2xl font-bold tracking-tight mb-1.5">Welcome back</h1>
              <p className="text-muted-foreground text-[15px]">
                Sign in to your Notrelix workspace
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
              <div className="space-y-2">
                <label htmlFor="password" className="text-sm font-medium">Password</label>
                <input
                  id="password"
                  type="password"
                  placeholder="Enter your password"
                  className="flex h-10 w-full rounded-md border border-input bg-input px-3 py-2 text-sm"
                />
              </div>
              <button
                type="submit"
                className="inline-flex items-center justify-center h-10 w-full rounded-md bg-primary text-primary-foreground hover:bg-primary/90 font-medium"
              >
                Sign in
              </button>
            </form>
          </div>
          <p className="mt-8 text-center text-sm text-muted-foreground">
            Don&apos;t have an account?{' '}
            <Link to="/sign-up" className="font-medium text-foreground hover:text-primary transition-colors underline-offset-4 hover:underline">
              Create one
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
