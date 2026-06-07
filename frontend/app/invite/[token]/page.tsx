import { InviteClientPage } from "./invite-client"

interface InvitePageProps {
  params: Promise<{ token: string }>
}

export default async function InvitePage({ params }: InvitePageProps) {
  const { token } = await params
  return <InviteClientPage token={token} />
}
