"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import {
  CheckCircle2,
  Loader2,
  Mail,
  ShieldAlert,
  Sparkles,
  ArrowRight,
  LogOut,
  Home,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Alert, AlertTitle, AlertDescription } from "@/components/ui/alert"
import { useAuthUser, useLogout } from "@/features/auth"
import { useInvitationByToken, useAcceptInvitation } from "@/features/workspace"

interface InviteClientPageProps {
  token: string
}

export function InviteClientPage({ token }: InviteClientPageProps) {
  const router = useRouter()
  const { user: currentUser, isAuthenticated, isLoading: authLoading } = useAuthUser()
  const logoutMutation = useLogout()
  
  const { data: invitation, isLoading: inviteLoading, error } = useInvitationByToken(token)
  const acceptMutation = useAcceptInvitation()

  const loading = authLoading || inviteLoading

  // ── 1. Màn hình Loading ──────────────────────────────────────────
  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-background via-background to-muted/30 px-4">
        <div className="flex flex-col items-center gap-3 text-center">
          <Loader2 className="size-10 animate-spin text-primary" />
          <p className="text-sm font-medium text-muted-foreground">Đang tải thông tin lời mời...</p>
        </div>
      </div>
    )
  }

  // ── 2. Màn hình lỗi (lời mời không tồn tại, hết hạn, hoặc bị lỗi) ──
  const isExpired = invitation?.isExpired
  const isAccepted = invitation?.isAccepted
  const hasError = error || !invitation || isExpired || isAccepted

  if (hasError) {
    let errorTitle = "Lời mời không hợp lệ"
    let errorDesc = "Liên kết lời mời này không tồn tại hoặc đã bị hủy bởi người quản trị."

    if (isExpired) {
      errorTitle = "Lời mời đã hết hạn"
      errorDesc = "Thời hạn của lời mời này đã kết thúc. Vui lòng liên hệ với người quản trị để nhận lời mời mới."
    } else if (isAccepted) {
      errorTitle = "Lời mời đã được chấp nhận"
      errorDesc = "Lời mời này đã được chấp nhận trước đây. Bạn không thể sử dụng lại liên kết này."
    }

    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-background via-background/80 to-muted/20 px-4">
        <Card className="w-full max-w-md border-border/60 bg-card/60 shadow-2xl backdrop-blur-md transition-all duration-300">
          <CardHeader className="text-center pb-2">
            <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-2xl bg-destructive/10 text-destructive">
              <ShieldAlert className="size-6" />
            </div>
            <CardTitle className="text-xl font-bold tracking-tight text-foreground">{errorTitle}</CardTitle>
            <CardDescription className="text-sm leading-relaxed mt-1.5">{errorDesc}</CardDescription>
          </CardHeader>
          <CardFooter className="pt-4 flex justify-center">
            <Button asChild className="rounded-xl w-full gap-2">
              <Link href="/">
                <Home className="size-4" />
                Về trang chủ
              </Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    )
  }

  // ── 3. Lời mời hợp lệ nhưng CHƯA đăng nhập ───────────────────────────
  if (!isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-background via-background/80 to-muted/20 px-4">
        <Card className="w-full max-w-md border-border/60 bg-card/60 shadow-2xl backdrop-blur-md">
          <CardHeader className="text-center pb-2">
            <div className="mx-auto mb-4 flex size-14 items-center justify-center rounded-2xl bg-primary/10 text-primary animate-pulse">
              <Mail className="size-6 text-primary" />
            </div>
            <div className="space-y-1.5">
              <span className="inline-block text-xs font-semibold uppercase tracking-wider text-primary bg-primary/10 px-2.5 py-0.5 rounded-full">
                Lời mời làm việc
              </span>
              <CardTitle className="text-2xl font-extrabold tracking-tight text-foreground">
                Tham gia Workspace
              </CardTitle>
            </div>
            <div className="text-sm text-muted-foreground mt-4 leading-relaxed bg-muted/40 p-4 rounded-xl border border-border/40 text-left">
              <p>
                <strong>{invitation.inviterName}</strong> đã mời bạn tham gia workspace{" "}
                <strong className="text-foreground">{invitation.workspaceName}</strong> với vai trò{" "}
                <strong className="text-foreground">{invitation.role}</strong>.
              </p>
              <p className="mt-2 text-xs text-muted-foreground">
                Vui lòng đăng nhập hoặc đăng ký tài khoản mới sử dụng địa chỉ email được mời (<strong>{invitation.email}</strong>) để tiếp tục.
              </p>
            </div>
          </CardHeader>
          <CardContent className="pt-4 space-y-3">
            <Button asChild className="w-full rounded-xl py-5 font-semibold gap-2 shadow-lg shadow-primary/20">
              <Link href={`/sign-in?redirect=/invite/${token}`}>
                Đăng nhập để tiếp tục
                <ArrowRight className="size-4" />
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full rounded-xl py-5 bg-card/50">
              <Link href={`/sign-up?redirect=/invite/${token}`}>
                Đăng ký tài khoản mới
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  // ── 4. Lời mời hợp lệ nhưng SAI email tài khoản đang đăng nhập ─────────
  const currentUserEmail = currentUser?.email || ""
  const isEmailMatching = currentUserEmail.trim().toLowerCase() === invitation.email.trim().toLowerCase()

  if (!isEmailMatching) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-background via-background/80 to-muted/20 px-4">
        <Card className="w-full max-w-md border-border/60 bg-card/60 shadow-2xl backdrop-blur-md">
          <CardHeader className="text-center pb-2">
            <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-2xl bg-warning/10 text-amber-500">
              <ShieldAlert className="size-6" />
            </div>
            <CardTitle className="text-xl font-bold tracking-tight text-foreground">Sai tài khoản người dùng</CardTitle>
            <CardDescription className="text-sm leading-relaxed mt-1.5">
              Lời mời này dành cho email <strong className="text-foreground font-semibold">{invitation.email}</strong>. 
              Tuy nhiên, bạn đang đăng nhập với tài khoản <strong className="text-foreground font-semibold">{currentUserEmail}</strong>.
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-4 space-y-3">
            <div className="p-4 rounded-xl bg-destructive/10 border border-destructive/20 text-xs text-destructive leading-relaxed">
              Vui lòng đăng xuất tài khoản hiện tại và đăng nhập bằng tài khoản sử dụng email được mời để có thể tham gia vào Workspace.
            </div>
          </CardContent>
          <CardFooter className="flex-col gap-2 pt-2">
            <Button 
              onClick={() => logoutMutation.mutate()} 
              disabled={logoutMutation.isPending} 
              className="w-full rounded-xl py-5 gap-2"
              variant="destructive"
            >
              {logoutMutation.isPending ? <Loader2 className="size-4 animate-spin" /> : <LogOut className="size-4" />}
              Đăng xuất & dùng tài khoản khác
            </Button>
            <Button asChild variant="ghost" className="w-full rounded-xl py-5">
              <Link href="/">Quay lại trang chủ</Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    )
  }

  // ── 5. Lời mời hợp lệ & Đúng tài khoản ────────────────────────────────
  const handleAccept = () => {
    if (acceptMutation.isPending) return
    acceptMutation.mutate(token, {
      onSuccess: (data) => {
        if (data && data.workspaceSlug) {
          router.push(`/${data.workspaceSlug}`)
        } else {
          router.push("/")
        }
      }
    })
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-background via-background/80 to-muted/20 px-4">
      <Card className="w-full max-w-md border-border/60 bg-card/60 shadow-2xl backdrop-blur-md relative overflow-hidden transition-all duration-300">
        <div className="absolute top-0 right-0 p-3 text-primary/10">
          <Sparkles className="size-20 animate-pulse" />
        </div>
        <CardHeader className="text-center pb-2">
          <div className="mx-auto mb-4 flex size-14 items-center justify-center rounded-2xl bg-primary/10 text-primary">
            <CheckCircle2 className="size-7" />
          </div>
          <div className="space-y-1">
            <span className="inline-block text-xs font-semibold uppercase tracking-wider text-emerald-500 bg-emerald-500/10 px-2.5 py-0.5 rounded-full">
              Sẵn sàng kết nối
            </span>
            <CardTitle className="text-2xl font-extrabold tracking-tight text-foreground pt-1">
              Chấp nhận lời mời
            </CardTitle>
          </div>
          <CardDescription className="text-sm mt-3 leading-relaxed">
            Xin chào <strong className="text-foreground font-semibold">{currentUser?.name || currentUserEmail}</strong>, bạn đã sẵn sàng tham gia workspace mới chưa?
          </CardDescription>
        </CardHeader>
        <CardContent className="pt-2">
          <div className="bg-muted/40 border border-border/40 rounded-2xl p-5 leading-relaxed text-sm space-y-2">
            <p className="text-muted-foreground">
              Bạn được mời bởi <strong className="text-foreground font-semibold">{invitation.inviterName}</strong>
            </p>
            <p className="text-muted-foreground">
              Tham gia Workspace: <strong className="text-foreground font-semibold">{invitation.workspaceName}</strong>
            </p>
            <p className="text-muted-foreground">
              Vai trò công việc: <strong className="text-foreground font-semibold capitalize">{invitation.role}</strong>
            </p>
          </div>

          {acceptMutation.isError && (
            <Alert variant="destructive" className="mt-4 border-destructive/20 bg-destructive/5 text-destructive rounded-xl">
              <ShieldAlert className="size-4" />
              <AlertTitle className="font-bold">Lời mời không hợp lệ</AlertTitle>
              <AlertDescription className="text-xs leading-relaxed mt-1">
                {(acceptMutation.error as any)?.response?.data?.detail || 
                 acceptMutation.error?.message || 
                 "Liên kết lời mời này không tồn tại hoặc đã bị hủy bởi người quản trị."}
              </AlertDescription>
            </Alert>
          )}
        </CardContent>
        <CardFooter className="pt-4">
          <Button 
            onClick={handleAccept} 
            disabled={acceptMutation.isPending} 
            className="w-full rounded-xl py-6 text-base font-semibold gap-2 shadow-lg shadow-primary/20 hover:shadow-primary/30 transition-all duration-200"
          >
            {acceptMutation.isPending ? (
              <>
                <Loader2 className="size-5 animate-spin" />
                Đang xử lý tham gia...
              </>
            ) : (
              <>
                Chấp nhận lời mời và tham gia
                <ArrowRight className="size-5" />
              </>
            )}
          </Button>
        </CardFooter>
      </Card>
    </div>
  )
}
