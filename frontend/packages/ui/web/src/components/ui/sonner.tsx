import { useOptionalTheme } from "../../theme/theme-provider";
import { Toaster as Sonner, type ToasterProps } from "sonner";
import {
  CheckIcon,
  CircleAlertIcon,
  InfoIcon,
  TriangleAlertIcon,
  Loader2Icon,
} from "lucide-react";

const Toaster = ({ ...props }: ToasterProps) => {
  const appearance = useOptionalTheme()?.appearance;
  const theme: ToasterProps["theme"] = appearance
    ? appearance === "dark"
      ? "dark"
      : "light"
    : "system";

  return (
    <Sonner
      theme={theme}
      className="toaster group"
      // Bottom left, three at most: older ones wait behind (the States mockup).
      position="bottom-left"
      visibleToasts={3}
      duration={6000}
      icons={{
        success: <CheckIcon className="size-4 text-inverse-success" />,
        info: <InfoIcon className="size-4 text-inverse-muted" />,
        warning: <TriangleAlertIcon className="size-4" />,
        error: <CircleAlertIcon className="size-4 text-inverse-danger" />,
        loading: <Loader2Icon className="size-4 animate-spin" />,
      }}
      style={
        {
          "--normal-bg": "var(--inverse-surface)",
          "--normal-text": "var(--inverse-foreground)",
          "--normal-border": "transparent",
          "--border-radius": "10px",
          zIndex: "var(--z-toast)",
        } as React.CSSProperties
      }
      toastOptions={{
        classNames: {
          toast: "cn-toast",
          description: "text-inverse-muted!",
          actionButton:
            "bg-transparent! font-semibold! text-inverse-foreground! underline underline-offset-2",
          closeButton:
            "bg-transparent! border-transparent! text-inverse-muted!",
        },
      }}
      {...props}
    />
  );
};

export { Toaster };
