import * as React from "react"

import { Button } from "../ui/button"
import { Spinner } from "../ui/spinner"

export interface SubmitStateProps extends React.ComponentProps<typeof Button> {
  pending?: boolean
  pendingLabel?: string
}

export function SubmitState({
  pending = false,
  pendingLabel = "Saving...",
  children,
  disabled,
  ...props
}: SubmitStateProps) {
  return (
    <Button disabled={disabled || pending} {...props}>
      {pending && <Spinner className="mr-2 h-4 w-4" />}
      {pending ? pendingLabel : children}
    </Button>
  )
}
