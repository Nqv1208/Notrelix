import { Navigate } from "@tanstack/react-router";
import { SIGN_IN_LANDING } from "../routing/sign-in-landing";

export function IndexPage() {
  return <Navigate {...SIGN_IN_LANDING} replace />;
}
