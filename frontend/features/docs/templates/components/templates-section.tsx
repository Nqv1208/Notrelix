"use client"

import { useRouter } from "next/navigation"
import { toast } from "sonner"
import { useCreatePage } from "../../pages/hooks/mutations/use-create-page"
import type { PageTemplate } from "../types/template.types"

interface TemplatesSectionProps {
  templates: PageTemplate[]
  workspaceId: string
}

export function TemplatesSection({ templates, workspaceId }: TemplatesSectionProps) {
  const router = useRouter()
  const createPage = useCreatePage()

  async function handleTemplate(template: PageTemplate) {
    const page = await createPage.mutateAsync({
      title: template.name,
      workspaceId,
      templateId: template.id,
    })
    toast.success("Template page created")
    router.push(`/${workspaceId}/docs/${page.id}`)
  }

  return (
    <section className="rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.22)_0px_2px_24px]">
      <div className="mb-4 flex items-center justify-between">
        <div>
          <h2 className="text-sm font-semibold text-foreground">Templates</h2>
          <p className="text-xs text-muted-foreground">Reusable structures for high-signal docs</p>
        </div>
      </div>
      <div className="grid gap-3 md:grid-cols-3">
        {templates.map((template) => (
          <button
            key={template.id}
            type="button"
            onClick={() => handleTemplate(template)}
            className="rounded-xl border border-border p-4 text-left transition hover:-translate-y-0.5 hover:shadow-[rgba(205,208,223,0.35)_0px_2px_24px]"
          >
            <div className="mb-4 flex items-center justify-between">
              <span className="flex size-10 items-center justify-center rounded-xl text-lg" style={{ backgroundColor: template.accent }}>
                {template.icon}
              </span>
              <span className="rounded-md px-2 py-1 text-xs font-medium text-primary">Use</span>
            </div>
            <h3 className="text-sm font-semibold text-foreground">{template.name}</h3>
            <p className="mt-1 text-xs leading-5 text-muted-foreground">{template.description}</p>
          </button>
        ))}
      </div>
    </section>
  )
}
