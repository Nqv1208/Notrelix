"use client"

import * as React from "react"
import Link from "next/link"
import { Mail, ArrowLeft, ArrowRight, Loader2, CheckCircle2 } from "lucide-react"

import { cn } from "@/lib/utils"
import { routes } from "@/lib/routes"
import { Button } from "@/registry/new-york-v4/ui/button"
import { Input } from "@/registry/new-york-v4/ui/input"
import { Label } from "@/registry/new-york-v4/ui/label"

export function ForgotPasswordForm() {
  const [email, setEmail] = React.useState("")
  const [error, setError] = React.useState("")
  const [isLoading, setIsLoading] = React.useState(false)
  const [isSent, setIsSent] = React.useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")

    if (!email.trim()) {
      setError("Please enter your email address.")
      return
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      setError("Please enter a valid email address.")
      return
    }

    setIsLoading(true)
    // Simulate API call — replace with real API later
    await new Promise((resolve) => setTimeout(resolve, 1500))
    setIsLoading(false)
    setIsSent(true)
  }

  if (isSent) {
    return (
      <div className="flex flex-col items-center text-center gap-6">
        <div className="flex items-center justify-center size-16 rounded-2xl bg-emerald-500/10">
          <CheckCircle2 className="size-8 text-emerald-600" />
        </div>

        <div>
          <h1 className="text-2xl font-bold tracking-tight mb-2">Check your email</h1>
          <p className="text-muted-foreground text-[15px] leading-relaxed max-w-sm">
            We&apos;ve sent a password reset link to{" "}
            <span className="font-medium text-foreground">{email}</span>.
            Click the link in the email to reset your password.
          </p>
        </div>

        <div className="w-full space-y-3">
          <Button
            variant="outline"
            className="w-full h-10"
            onClick={() => {
              setIsSent(false)
              setEmail("")
            }}
          >
            Try a different email
          </Button>

          <Link href={routes.auth.signIn} className="block">
            <Button
              variant="ghost"
              className="w-full h-10 text-muted-foreground"
            >
              <ArrowLeft className="size-4 mr-2" />
              Back to sign in
            </Button>
          </Link>
        </div>

        <p className="text-xs text-muted-foreground">
          Didn&apos;t receive an email? Check your spam folder or{" "}
          <button
            onClick={() => {
              setIsSent(false)
            }}
            className="text-foreground underline-offset-4 hover:underline"
          >
            try again
          </button>
          .
        </p>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-7">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold tracking-tight mb-1.5">
          Reset your password
        </h1>
        <p className="text-muted-foreground text-[15px] leading-relaxed">
          Enter the email address associated with your account and we&apos;ll send
          you a link to reset your password.
        </p>
      </div>

      {/* Field */}
      <div className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="email">Email address</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
            <Input
              id="email"
              type="email"
              placeholder="you@company.com"
              autoComplete="email"
              autoFocus
              value={email}
              onChange={(e) => {
                setEmail(e.target.value)
                if (error) setError("")
              }}
              className={cn("pl-9 h-10", error && "border-destructive focus-visible:ring-destructive")}
            />
          </div>
          {error && <p className="text-xs text-destructive">{error}</p>}
        </div>
      </div>

      {/* Submit */}
      <Button
        type="submit"
        className="w-full h-10 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20"
        disabled={isLoading}
      >
        {isLoading ? (
          <>
            <Loader2 className="size-4 mr-2 animate-spin" />
            Sending link...
          </>
        ) : (
          <>
            Send reset link
            <ArrowRight className="size-4 ml-2" />
          </>
        )}
      </Button>

      {/* Back */}
      <Link href={routes.auth.signIn}>
        <Button variant="ghost" type="button" className="w-full h-10 text-muted-foreground">
          <ArrowLeft className="size-4 mr-2" />
          Back to sign in
        </Button>
      </Link>
    </form>
  )
}
