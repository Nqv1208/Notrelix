"use client"

import { useRouter } from "next/navigation"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { Plus } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { createPageSchema } from "@/features/docs/schemas"
import { useCreatePage } from "@/features/docs/hooks/use-create-page"
import type { z } from "zod"

type FormValues = z.infer<typeof createPageSchema>

interface NewPageButtonProps {
  workspaceId: string
  workspaceSlug: string
  parentId?: string | null
  compact?: boolean
}

export function NewPageButton({ workspaceId, workspaceSlug, parentId = null, compact }: NewPageButtonProps) {
  const router = useRouter()
  const createPage = useCreatePage()
  const form = useForm<FormValues>({
    resolver: zodResolver(createPageSchema),
    defaultValues: { title: "", workspaceId, workspaceSlug, parentId },
  })

  async function onSubmit(values: FormValues) {
    const page = await createPage.mutateAsync({ ...values, workspaceId, workspaceSlug, parentId })
    toast.success("Page created")
    router.push(`/${workspaceSlug}/docs/${page.id}`)
  }

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button size={compact ? "icon-sm" : "sm"} className={compact ? "" : "rounded-full bg-primary text-primary-foreground hover:bg-primary/90"}>
          <Plus className="size-4" />
          {compact ? <span className="sr-only">New page</span> : "New page"}
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[440px]">
        <DialogHeader>
          <DialogTitle>Create page</DialogTitle>
          <DialogDescription>Start from a blank page. Templates can be applied from the overview.</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <Input placeholder="Page title" autoFocus {...form.register("title")} />
          {form.formState.errors.title ? (
            <p className="text-sm text-destructive">{form.formState.errors.title.message}</p>
          ) : null}
          <DialogFooter>
            <Button type="button" onClick={form.handleSubmit(onSubmit)} disabled={createPage.isPending}>
              {createPage.isPending ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </div>
      </DialogContent>
    </Dialog>
  )
}
