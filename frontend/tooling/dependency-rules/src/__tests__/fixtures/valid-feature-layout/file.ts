import { AppError } from "@notrelix/kernel";

// Valid pure core file
export function validateWorkspaceId(id: string): boolean {
  if (!id) throw new AppError("Invalid workspace ID");
  return true;
}
