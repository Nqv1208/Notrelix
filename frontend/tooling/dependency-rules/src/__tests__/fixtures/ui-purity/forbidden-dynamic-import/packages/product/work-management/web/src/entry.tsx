import { Button } from "@notrelix/ui-web";

export function DynamicLoader() {
  return <Button onClick={() => void import("@notrelix/work-management-state")}>Load</Button>;
}
