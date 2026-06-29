import { Suspense } from "react"
import { LoginForm } from "@/features/auth"

export default function SignInPage() {
  return (
    <Suspense fallback={<div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Loading form...</div>}>
      <LoginForm />
    </Suspense>
  )
}
