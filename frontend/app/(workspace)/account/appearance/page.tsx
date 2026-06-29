"use client"

import { useTheme } from "next-themes"
import { useTranslations } from "next-intl"
import { useColorTheme } from "@/lib/theme"
import { useMounted } from "@/hooks/use-mounted"
import { Sun, Moon, Laptop, Check } from "lucide-react"
import { cn } from "@/lib/utils"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"

export default function AppearancePage() {
  const { theme, setTheme } = useTheme()
  const { colorTheme, setColorTheme, themes } = useColorTheme()
  const mounted = useMounted()
  const t = useTranslations("account.appearance")

  if (!mounted) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Giao diện</h1>
          <p className="text-muted-foreground text-sm">
            Tùy chỉnh chế độ hiển thị và bảng màu cho workspace của bạn.
          </p>
        </div>
        <Separator />
        <div className="h-64 flex items-center justify-center text-sm text-muted-foreground">
          Đang tải cấu hình giao diện...
        </div>
      </div>
    )
  }

  const modes = [
    { id: "light", name: t("light"), icon: Sun, desc: t("lightDesc") },
    { id: "dark", name: t("dark"), icon: Moon, desc: t("darkDesc") },
    { id: "system", name: t("system"), icon: Laptop, desc: t("systemDesc") },
  ]

  const getThemeName = (id: string) => {
    if (id === "default") return t("defaultTheme")
    return t(id)
  }

  const getThemeDesc = (id: string) => {
    if (id === "default") return t("defaultThemeDesc")
    return t(`${id}Desc`)
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground text-sm">{t("description")}</p>
      </div>

      <Separator />

      {/* Chế độ hiển thị */}
      <div className="space-y-4">
        <div>
          <h2 className="text-base font-semibold">{t("displayMode")}</h2>
          <p className="text-muted-foreground text-xs">{t("displayModeDesc")}</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {modes.map((mode) => {
            const Icon = mode.icon
            const isActive = theme === mode.id

            return (
              <button
                key={mode.id}
                onClick={() => setTheme(mode.id)}
                className={cn(
                  "group relative flex flex-col items-start gap-2 rounded-xl border p-4 text-left transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-primary",
                  isActive
                    ? "border-primary bg-accent/40 shadow-sm"
                    : "border-border hover:border-muted-foreground/30 hover:bg-muted/30"
                )}
              >
                <div className="flex w-full items-center justify-between">
                  <div className="rounded-lg bg-primary/10 p-2 text-primary group-hover:scale-105 transition-transform">
                    <Icon size={18} />
                  </div>
                  {isActive && (
                    <div className="rounded-full bg-primary p-0.5 text-primary-foreground">
                      <Check size={12} />
                    </div>
                  )}
                </div>
                <div className="mt-2">
                  <div className="text-sm font-medium">{mode.name}</div>
                  <div className="text-xs text-muted-foreground">{mode.desc}</div>
                </div>
              </button>
            )
          })}
        </div>
      </div>

      <Separator />

      {/* Bảng màu */}
      <div className="space-y-4">
        <div>
          <h2 className="text-base font-semibold">{t("colorTheme")}</h2>
          <p className="text-muted-foreground text-xs">{t("colorThemeDesc")}</p>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {themes.map((tItem) => {
            const isActive = colorTheme === tItem.id

            // Generate theme preview backgrounds based on theme metadata properties
            return (
              <button
                key={tItem.id}
                onClick={() => setColorTheme(tItem.id)}
                className={cn(
                  "group relative flex flex-col items-stretch rounded-xl border p-3 text-left transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-primary",
                  isActive
                    ? "border-primary bg-accent/40 shadow-sm"
                    : "border-border hover:border-muted-foreground/30 hover:bg-muted/30"
                )}
              >
                {/* Visual Swatch Preview Card */}
                <div className="relative mb-3 aspect-[4/3] w-full rounded-lg overflow-hidden border border-border bg-background flex flex-col p-1.5 gap-1 select-none">
                  <div className="flex items-center justify-between w-full h-3 border-b border-border/60 pb-1">
                    <div className="flex gap-1 items-center">
                      <div className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: tItem.primaryColor }} />
                      <div className="h-1 w-6 rounded bg-foreground/10" />
                    </div>
                    {isActive && (
                      <div className="rounded-full bg-primary p-0.2 text-primary-foreground scale-75">
                        <Check size={8} />
                      </div>
                    )}
                  </div>
                  
                  <div className="flex flex-1 gap-1.5">
                    {/* Left Mini Sidebar */}
                    <div className="w-1/4 rounded bg-muted/60 flex flex-col p-0.5 gap-0.5 border border-border/40">
                      <div className="h-1 w-full rounded bg-foreground/10" />
                      <div className="h-1 w-3/4 rounded bg-foreground/10" />
                    </div>
                    {/* Right Mini Content */}
                    <div className="flex-1 flex flex-col gap-1 p-0.5">
                      <div className="h-1.5 w-3/4 rounded" style={{ backgroundColor: tItem.primaryColor }} />
                      <div className="h-1 w-full rounded bg-foreground/15" />
                      <div className="h-1 w-full rounded bg-foreground/10" />
                      <div className="h-1 w-2/3 rounded bg-foreground/10" />
                    </div>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div className="text-xs font-semibold">{getThemeName(tItem.id)}</div>
                  <div 
                    className="h-3 w-3 rounded-full border border-border"
                    style={{ backgroundColor: tItem.primaryColor }}
                  />
                </div>
                <div className="text-[10px] text-muted-foreground mt-0.5 line-clamp-1">{getThemeDesc(tItem.id)}</div>
              </button>
            )
          })}
        </div>
      </div>
    </div>
  )
}
