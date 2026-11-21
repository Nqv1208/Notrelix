import { useParams } from "@tanstack/react-router";
import { PublicFormPage } from "@notrelix/work-management-web";

/**
 * A public form, opened from its link by someone with no account: outside the auth guard and the app shell,
 * so the page is only the form. The key is the whole address; nothing here reads the session.
 */
export function PublicFormRoutePage() {
  const { publicKey } = useParams({ from: "/f/$publicKey" });
  return <PublicFormPage publicKey={publicKey} />;
}
