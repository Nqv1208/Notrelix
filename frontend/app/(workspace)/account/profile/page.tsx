"use client"

import { useState, useEffect } from "react"
import { useTranslations } from "next-intl"
import { useAuthUser } from "@/features/auth/hooks/useAuthUser"
import { useUpdateProfile } from "@/features/account/hooks/use-update-profile"
import { useMounted } from "@/hooks/use-mounted"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from "@/components/ui/select"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { toast } from "sonner"
import { Camera, ShieldAlert } from "lucide-react"

export default function ProfilePage() {
  const { user } = useAuthUser()
  const updateProfileMutation = useUpdateProfile()
  const mounted = useMounted()
  const t = useTranslations("account.profile")

  const [name, setName] = useState("")
  const [avatarUrl, setAvatarUrl] = useState("")
  
  // Simulated fields for visual completeness & future backend expansions
  const [title, setTitle] = useState("Developer")
  const [phone, setPhone] = useState("")
  const [timezone, setTimezone] = useState("Asia/Ho_Chi_Minh")
  const [language, setLanguage] = useState("vi")

  useEffect(() => {
    if (user) {
      setName(user.name || "")
      setAvatarUrl(user.avatarUrl || "")
    }
  }, [user])

  if (!mounted || !user) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Hồ sơ cá nhân</h1>
          <p className="text-muted-foreground text-sm">
            Quản lý thông tin hồ sơ và cài đặt tài khoản của bạn.
          </p>
        </div>
        <Separator />
        <div className="h-64 flex items-center justify-center text-sm text-muted-foreground">
          Đang tải thông tin cá nhân...
        </div>
      </div>
    )
  }

  const initials = user.name
    ? user.name.split(" ").map((n) => n[0]).join("").toUpperCase().slice(0, 2)
    : "U"

  const handleSave = () => {
    if (!name.trim()) {
      toast.error(t("nameRequired"))
      return
    }

    updateProfileMutation.mutate({
      name: name.trim(),
      avatar: avatarUrl.trim() || null,
    })
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground text-sm">{t("description")}</p>
      </div>

      <Separator />

      {/* Profile Header Card / Avatar Upload */}
      <Card className="border border-border bg-card/40 overflow-hidden">
        <CardContent className="pt-6">
          <div className="flex flex-col sm:flex-row items-center gap-6">
            <div className="relative group">
              <Avatar className="h-24 w-24 border-2 border-border shadow-sm">
                <AvatarImage src={avatarUrl} alt={user.name} />
                <AvatarFallback className="bg-primary text-xl font-bold text-primary-foreground">
                  {initials}
                </AvatarFallback>
              </Avatar>
              <div className="absolute inset-0 bg-black/60 rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-200 cursor-pointer">
                <Camera size={18} className="text-white" />
              </div>
            </div>
            
            <div className="text-center sm:text-left space-y-1">
              <h2 className="text-lg font-semibold">{user.name}</h2>
              <p className="text-sm text-muted-foreground">{user.email}</p>
              <div className="flex flex-wrap gap-2 justify-center sm:justify-start pt-1">
                <span className="inline-flex items-center rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                  {t("badgeMember")}
                </span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Profile Form Details */}
      <div className="space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="name">{t("name")}</Label>
            <Input 
              id="name" 
              value={name} 
              onChange={(e) => setName(e.target.value)} 
              placeholder={t("namePlaceholder")}
              className="bg-background/50 focus-visible:ring-primary"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="email">{t("email")}</Label>
            <Input 
              id="email" 
              value={user.email} 
              disabled 
              className="bg-muted text-muted-foreground cursor-not-allowed border-dashed"
            />
            <p className="text-[10px] text-muted-foreground">{t("emailHint")}</p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="avatar">{t("avatarUrl")}</Label>
            <Input 
              id="avatar" 
              value={avatarUrl} 
              onChange={(e) => setAvatarUrl(e.target.value)} 
              placeholder={t("avatarPlaceholder")}
              className="bg-background/50 focus-visible:ring-primary"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="title">{t("jobTitle")}</Label>
            <Input 
              id="title" 
              value={title} 
              onChange={(e) => setTitle(e.target.value)} 
              placeholder={t("jobTitlePlaceholder")}
              className="bg-background/50 focus-visible:ring-primary"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="phone">{t("phone")}</Label>
            <Input 
              id="phone" 
              value={phone} 
              onChange={(e) => setPhone(e.target.value)} 
              placeholder="+84..."
              className="bg-background/50 focus-visible:ring-primary"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="timezone">{t("timezone")}</Label>
            <Select value={timezone} onValueChange={setTimezone}>
              <SelectTrigger id="timezone" className="bg-background/50 focus-visible:ring-primary">
                <SelectValue placeholder={t("timezonePlaceholder")} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Asia/Ho_Chi_Minh">Asia/Ho_Chi_Minh (GMT+7)</SelectItem>
                <SelectItem value="Asia/Singapore">Asia/Singapore (GMT+8)</SelectItem>
                <SelectItem value="America/New_York">America/New_York (GMT-5)</SelectItem>
                <SelectItem value="Europe/London">Europe/London (GMT+0)</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="language">{t("language")}</Label>
            <Select value={language} onValueChange={setLanguage}>
              <SelectTrigger id="language" className="bg-background/50 focus-visible:ring-primary">
                <SelectValue placeholder={t("languagePlaceholder")} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="vi">Tiếng Việt</SelectItem>
                <SelectItem value="en">English</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="flex justify-end pt-4">
          <Button 
            onClick={handleSave} 
            disabled={updateProfileMutation.isPending}
            className="px-6 shadow-sm shadow-primary/10"
          >
            {updateProfileMutation.isPending ? t("saving") : t("saveChanges")}
          </Button>
        </div>
      </div>

      <Separator />

      {/* Danger Zone */}
      <Card className="border border-destructive/30 bg-destructive/5 rounded-xl">
        <CardHeader className="pb-3">
          <div className="flex items-center gap-2 text-destructive">
            <ShieldAlert size={18} />
            <CardTitle className="text-sm font-semibold uppercase tracking-wider">{t("dangerZone")}</CardTitle>
          </div>
          <CardDescription className="text-xs text-muted-foreground">
            {t("dangerZoneDesc")}
          </CardDescription>
        </CardHeader>
        <CardContent className="pb-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h4 className="text-sm font-medium">{t("deleteAccount")}</h4>
              <p className="text-xs text-muted-foreground">
                {t("deleteAccountDesc")}
              </p>
            </div>
            <Button variant="destructive" size="sm" className="sm:w-auto">
              {t("deleteAccount")}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
