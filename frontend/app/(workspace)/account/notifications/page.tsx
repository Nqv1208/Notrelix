"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useMounted } from "@/hooks/use-mounted"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { toast } from "sonner"
import { Mail, Smartphone, Radio } from "lucide-react"

export default function NotificationsPage() {
  const mounted = useMounted()
  const t = useTranslations("account.notifications")
  
  // Notification Preferences States
  const [emailMentions, setEmailMentions] = useState(true)
  const [emailComments, setEmailComments] = useState(true)
  const [emailUpdates, setEmailUpdates] = useState(false)
  
  const [pushMentions, setPushMentions] = useState(true)
  const [pushComments, setPushComments] = useState(true)
  const [pushSound, setPushSound] = useState(true)

  const [marketingNews, setMarketingNews] = useState(false)

  const [isSaving, setIsSaving] = useState(false)

  if (!mounted) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Thông báo</h1>
          <p className="text-muted-foreground text-sm">
            Quản lý tùy chọn thông báo của bạn trên toàn bộ ứng dụng và qua email.
          </p>
        </div>
        <Separator />
        <div className="h-64 flex items-center justify-center text-sm text-muted-foreground">
          Đang tải cấu hình thông báo...
        </div>
      </div>
    )
  }

  const handleSave = () => {
    setIsSaving(true)
    setTimeout(() => {
      setIsSaving(false)
      toast.success(t("save") + " thành công!")
    }, 800)
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground text-sm">{t("description")}</p>
      </div>

      <Separator />

      {/* Email Notifications */}
      <Card className="border border-border bg-card/40">
        <CardHeader>
          <div className="flex items-center gap-2">
            <Mail size={18} className="text-primary" />
            <CardTitle className="text-base font-semibold">{t("email")}</CardTitle>
          </div>
          <CardDescription className="text-xs text-muted-foreground">
            {t("emailDesc")}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between space-x-2">
            <div className="flex flex-col space-y-0.5">
              <Label htmlFor="email-mentions" className="text-sm font-medium">{t("emailMentions")}</Label>
              <span className="text-xs text-muted-foreground">{t("emailMentionsDesc")}</span>
            </div>
            <Switch
              id="email-mentions"
              checked={emailMentions}
              onCheckedChange={setEmailMentions}
            />
          </div>
          
          <Separator />
          
          <div className="flex items-center justify-between space-x-2">
            <div className="flex flex-col space-y-0.5">
              <Label htmlFor="email-comments" className="text-sm font-medium">{t("emailComments")}</Label>
              <span className="text-xs text-muted-foreground">{t("emailCommentsDesc")}</span>
            </div>
            <Switch
              id="email-comments"
              checked={emailComments}
              onCheckedChange={setEmailComments}
            />
          </div>

          <Separator />

          <div className="flex items-center justify-between space-x-2">
            <div className="flex flex-col space-y-0.5">
              <Label htmlFor="email-updates" className="text-sm font-medium">{t("emailUpdates")}</Label>
              <span className="text-xs text-muted-foreground">{t("emailUpdatesDesc")}</span>
            </div>
            <Switch
              id="email-updates"
              checked={emailUpdates}
              onCheckedChange={setEmailUpdates}
            />
          </div>
        </CardContent>
      </Card>

      {/* Push Notifications */}
      <Card className="border border-border bg-card/40">
        <CardHeader>
          <div className="flex items-center gap-2">
            <Smartphone size={18} className="text-primary" />
            <CardTitle className="text-base font-semibold">{t("push")}</CardTitle>
          </div>
          <CardDescription className="text-xs text-muted-foreground">
            {t("pushDesc")}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between space-x-2">
            <div className="flex flex-col space-y-0.5">
              <Label htmlFor="push-mentions" className="text-sm font-medium">{t("pushMentions")}</Label>
              <span className="text-xs text-muted-foreground">{t("pushMentionsDesc")}</span>
            </div>
            <Switch
              id="push-mentions"
              checked={pushMentions}
              onCheckedChange={setPushMentions}
            />
          </div>

          <Separator />

          <div className="flex items-center justify-between space-x-2">
            <div className="flex flex-col space-y-0.5">
              <Label htmlFor="push-comments" className="text-sm font-medium">{t("pushComments")}</Label>
              <span className="text-xs text-muted-foreground">{t("pushCommentsDesc")}</span>
            </div>
            <Switch
              id="push-comments"
              checked={pushComments}
              onCheckedChange={setPushComments}
            />
          </div>

          <Separator />

          <div className="flex items-center justify-between space-x-2">
            <div className="flex flex-col space-y-0.5">
              <Label htmlFor="push-sound" className="text-sm font-medium">{t("pushSound")}</Label>
              <span className="text-xs text-muted-foreground">{t("pushSoundDesc")}</span>
            </div>
            <Switch
              id="push-sound"
              checked={pushSound}
              onCheckedChange={setPushSound}
            />
          </div>
        </CardContent>
      </Card>

      {/* Tin tức & cập nhật sản phẩm */}
      <Card className="border border-border bg-card/40">
        <CardHeader>
          <div className="flex items-center gap-2">
            <Radio size={18} className="text-primary" />
            <CardTitle className="text-base font-semibold">{t("updates")}</CardTitle>
          </div>
          <CardDescription className="text-xs text-muted-foreground">
            {t("updatesDesc")}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between space-x-2">
            <div className="flex flex-col space-y-0.5">
              <Label htmlFor="marketing-news" className="text-sm font-medium">{t("newsletter")}</Label>
              <span className="text-xs text-muted-foreground">{t("newsletterDesc")}</span>
            </div>
            <Switch
              id="marketing-news"
              checked={marketingNews}
              onCheckedChange={setMarketingNews}
            />
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button onClick={handleSave} disabled={isSaving} className="px-6 shadow-sm shadow-primary/10">
          {isSaving ? t("saving") : t("save")}
        </Button>
      </div>
    </div>
  )
}
