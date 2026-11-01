import { useState, type FormEvent } from "react";
import { Button, Input } from "@notrelix/ui-web";
import { operatorApi, setToken, type Session } from "../api";
import { ErrorNote, Field, messageOf } from "../parts";

export function SignIn({ onSignedIn }: { onSignedIn: (s: Session) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await operatorApi.signIn(email.trim(), password);
      setToken(session.accessToken);
      onSignedIn(session);
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="grid min-h-svh place-items-center bg-background p-6">
      <form
        onSubmit={(e) => void submit(e)}
        className="flex w-full max-w-[380px] flex-col gap-4 rounded-lg border bg-card p-6"
      >
        <h1 className="m-0 text-page-title">Operator console</h1>
        <p className="m-0 text-compact text-muted-foreground">
          Platform operators only. This is not a tenant sign-in.
        </p>
        <Field label="Email" htmlFor="operator-email">
          <Input
            id="operator-email"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </Field>
        <Field label="Password" htmlFor="operator-password">
          <Input
            id="operator-password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </Field>
        <ErrorNote message={error} />
        <Button type="submit" disabled={busy || !email || !password}>
          {busy ? "Signing in" : "Sign in"}
        </Button>
      </form>
    </main>
  );
}
