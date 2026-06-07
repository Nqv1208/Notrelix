"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useMounted } from "@/hooks/use-mounted"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { toast } from "sonner"
import { ShieldCheck, Smartphone, KeyRound, Monitor } from "lucide-react"

export default function SecurityPage() {
  const mounted = useMounted()
  const t = useTranslations("account.security")

  const [currentPassword, setCurrentPassword] = useState("")
  const [newPassword, setNewPassword] = useState("")
  const [confirmPassword, setConfirmPassword] = useState("")
  const [isChanging, setIsChanging] = useState(false)

  if (!mounted) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Bảo mật & Đăng nhập</h1>
          <p className="text-muted-foreground text-sm">
            Quản lý mật khẩu, xác thực hai yếu tố và các phiên đăng nhập đang hoạt động.
          </p>
        </div>
        <Separator />
        <div className="h-64 flex items-center justify-center text-sm text-muted-foreground">
          Đang tải cài đặt bảo mật...
        </div>
      </div>
    )
  }

  const handleChangePassword = (e: React.FormEvent) => {
    e.preventDefault()

    if (!currentPassword || !newPassword || !confirmPassword) {
      toast.error(t("passwordRequired"))
      return
    }

    if (newPassword.length < 6) {
      toast.error(t("passwordMinLength"))
      return
    }

    if (newPassword !== confirmPassword) {
      toast.error(t("passwordMismatch"))
      return
    }

    setIsChanging(true)

    // Simulate backend change password request
    setTimeout(() => {
      setIsChanging(false)
      toast.success(t("changePassword") + " thành công!")
      setCurrentPassword("")
      setNewPassword("")
      setConfirmPassword("")
    }, 1200)
  }

  const activeSessions = [
    {
      device: "MacBook Pro (Chrome - macOS)",
      location: "Hà Nội, Việt Nam",
      ip: "14.232.84.112",
      current: true,
    },
    {
      device: "iPhone 15 Pro (Safari - iOS)",
      location: "Hà Nội, Việt Nam",
      ip: "27.72.103.54",
      current: false,
    },
  ]

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground text-sm">{t("description")}</p>
      </div>

      <Separator />

      {/* Đổi mật khẩu */}
      <Card className="border border-border bg-card/40">
        <CardHeader>
          <div className="flex items-center gap-2">
            <KeyRound size={18} className="text-primary" />
            <CardTitle className="text-base font-semibold">{t("changePassword")}</CardTitle>
          </div>
          <CardDescription className="text-xs text-muted-foreground">
            {t("changePasswordDesc")}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleChangePassword} className="space-y-4 max-w-md">
            <div className="space-y-2">
              <Label htmlFor="current-password">{t("currentPassword")}</Label>
              <Input
                id="current-password"
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                placeholder="••••••••"
                className="bg-background/50 focus-visible:ring-primary"
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="new-password">{t("newPassword")}</Label>
              <Input
                id="new-password"
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="••••••••"
                className="bg-background/50 focus-visible:ring-primary"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirm-password">{t("confirmPassword")}</Label>
              <Input
                id="confirm-password"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                className="bg-background/50 focus-visible:ring-primary"
              />
            </div>

            <Button type="submit" disabled={isChanging} className="shadow-sm shadow-primary/10">
              {isChanging ? t("updating") : t("updatePassword")}
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* Xác thực 2 yếu tố */}
      <Card className="border border-border bg-card/40">
        <CardHeader>
          <div className="flex items-center gap-2">
            <Smartphone size={18} className="text-primary" />
            <CardTitle className="text-base font-semibold">{t("twoFactor")}</CardTitle>
          </div>
          <CardDescription className="text-xs text-muted-foreground">
            {t("twoFactorDesc")}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4">
          <div>
            <h4 className="text-sm font-medium">{t("twoFactorApp")}</h4>
            <p className="text-xs text-muted-foreground mt-0.5">
              {t("twoFactorAppDesc")}
            </p>
          </div>
          <Button variant="outline" size="sm">
            {t("activate")}
          </Button>
        </CardContent>
      </Card>

      {/* Các phiên đăng nhập */}
      <Card className="border border-border bg-card/40">
        <CardHeader>
          <div className="flex items-center gap-2">
            <Monitor size={18} className="text-primary" />
            <CardTitle className="text-base font-semibold">{t("sessions")}</CardTitle>
          </div>
          <CardDescription className="text-xs text-muted-foreground">
            {t("sessionsDesc")}
          </CardDescription>
        </CardHeader>
        <CardContent className="divide-y divide-border">
          {activeSessions.map((session, idx) => (
            <div key={idx} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
              <div className="flex items-start gap-3">
                <div className="rounded-lg bg-muted p-2 mt-0.5 text-muted-foreground">
                  <Monitor size={16} />
                </div>
                <div>
                  <div className="text-sm font-medium flex items-center gap-2">
                    {session.device}
                    {session.current && (
                      <span className="inline-flex items-center rounded-full bg-green-500/10 px-2 py-0.5 text-[10px] font-medium text-green-500 border border-green-500/20">
                        {t("currentSession")}
                      </span>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground mt-0.5">
                    {session.location} • IP: {session.ip}
                  </div>
                </div>
              </div>
              {!session.current && (
                <Button variant="ghost" size="sm" className="text-destructive hover:bg-destructive/10">
                  {t("logout")}
                </Button>
              )}
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  )
}
