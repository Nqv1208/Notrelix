import { useState, useMemo } from "react";
import { useParams } from "@tanstack/react-router";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import {
  createUseWorkspaceMembers,
  createUseWorkspaceInvitations,
  createUseCreateInvitation,
  createUseDeleteInvitation,
  createUseRemoveMember,
  createUseUpdateMemberRole,
} from "@notrelix/features-workspace";
import type {
  WorkspaceMember,
  WorkspaceInvitation,
} from "@notrelix/features-workspace/core";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  Button,
  Input,
  Avatar,
  AvatarFallback,
  AvatarImage,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@notrelix/ui-web";
import { toast } from "sonner";
import { Trash2, UserMinus, Mail } from "lucide-react";

export function MembersPage() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });
  const { api: runtimeClient } = useAppRuntime();
  const { workspace } = useWorkspaceContext();

  const useWorkspaceMembers = useMemo(
    () => createUseWorkspaceMembers({ api: runtimeClient.api }),
    [runtimeClient],
  );

  const useWorkspaceInvitations = useMemo(
    () =>
      createUseWorkspaceInvitations({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const useCreateInvitation = useMemo(
    () =>
      createUseCreateInvitation({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const useDeleteInvitation = useMemo(
    () =>
      createUseDeleteInvitation({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const useRemoveMember = useMemo(
    () => createUseRemoveMember({ api: runtimeClient.api }),
    [runtimeClient],
  );

  const useUpdateMemberRole = useMemo(
    () => createUseUpdateMemberRole({ api: runtimeClient.api }),
    [runtimeClient],
  );

  const { data: members = [], isLoading } = useWorkspaceMembers(workspaceId);
  const { data: invitations = [] } = useWorkspaceInvitations(workspaceId);
  const createInvitationMutation = useCreateInvitation(workspaceId);
  const deleteInvitationMutation = useDeleteInvitation(workspaceId);
  const removeMemberMutation = useRemoveMember(workspaceId);
  const updateRoleMutation = useUpdateMemberRole(workspaceId);

  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState("member");

  const handleInvite = async () => {
    if (!inviteEmail.trim()) {
      toast.error("Please enter an email address.");
      return;
    }
    try {
      await createInvitationMutation.mutateAsync({
        email: inviteEmail.trim(),
        role: inviteRole,
      });
      setInviteEmail("");
      toast.success("Invitation sent successfully.");
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to send invitation.",
      );
    }
  };

  const handleRoleChange = (
    userId: string,
    newRole: "member" | "owner" | "admin" | "guest",
  ) => {
    updateRoleMutation.mutate({ userId, role: newRole });
  };

  const handleRemoveMember = (userId: string, name: string) => {
    if (
      window.confirm(
        `Are you sure you want to remove ${name} from this workspace?`,
      )
    ) {
      removeMemberMutation.mutate(userId);
    }
  };

  const handleRevokeInvitation = (invitationId: string, email: string) => {
    if (
      window.confirm(
        `Are you sure you want to revoke the invitation to ${email}?`,
      )
    ) {
      deleteInvitationMutation.mutate(invitationId);
    }
  };

  return (
    <div className="p-8 max-w-4xl">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight mb-1">Members</h1>
        <p className="text-sm text-muted-foreground">
          Manage who has access to {workspace?.name ?? "this workspace"} and
          control their permission levels.
        </p>
      </div>

      {/* Invite Section */}
      <div className="p-5 rounded-xl border border-border/80 bg-muted/10 mb-8">
        <h3 className="text-sm font-semibold text-foreground mb-3">
          Invite new member
        </h3>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            handleInvite();
          }}
          className="flex flex-col sm:flex-row gap-3"
        >
          <div className="flex-1">
            <Input
              type="email"
              placeholder="Enter email address (name@example.com)"
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              className="rounded-lg bg-card"
            />
          </div>
          <div className="w-full sm:w-36">
            <Select value={inviteRole} onValueChange={setInviteRole}>
              <SelectTrigger className="rounded-lg bg-card">
                <SelectValue placeholder="Select role" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="member">Member</SelectItem>
                <SelectItem value="admin">Admin</SelectItem>
                <SelectItem value="guest">Guest</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <Button
            type="submit"
            disabled={createInvitationMutation.isPending || !inviteEmail.trim()}
            className="rounded-lg px-5"
          >
            <Mail className="size-4 mr-2" />
            {createInvitationMutation.isPending ? "Sending..." : "Send Invite"}
          </Button>
        </form>
      </div>

      {/* Members Table */}
      <div className="space-y-3 mb-8">
        <h3 className="text-sm font-semibold text-foreground">
          Active members ({members.length})
        </h3>
        <div className="rounded-xl border border-border/60 overflow-hidden bg-card">
          <Table>
            <TableHeader className="bg-muted/30">
              <TableRow className="hover:bg-transparent">
                <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">
                  Member
                </TableHead>
                <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">
                  Role
                </TableHead>
                <TableHead className="py-3 px-4 w-[80px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                [1, 2, 3].map((i) => (
                  <TableRow key={i}>
                    <TableCell colSpan={3}>
                      <div className="h-10 bg-muted rounded animate-pulse" />
                    </TableCell>
                  </TableRow>
                ))
              ) : members.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={3}
                    className="py-8 text-center text-sm text-muted-foreground"
                  >
                    No members yet.
                  </TableCell>
                </TableRow>
              ) : (
                members.map((member: WorkspaceMember) => (
                  <TableRow
                    key={member.id}
                    className="hover:bg-muted/10 transition-colors border-border/50"
                  >
                    <TableCell className="py-3 px-4">
                      <div className="flex items-center gap-3">
                        <Avatar className="size-8 ring-1 ring-border/20">
                          <AvatarImage
                            src={member.avatarUrl}
                            alt={member.name}
                          />
                          <AvatarFallback
                            className="text-[11px] font-semibold text-primary-foreground"
                            style={{ backgroundColor: member.color }}
                          >
                            {member.initials}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="font-semibold text-foreground text-sm flex items-center gap-1.5">
                            {member.name}
                            {member.role === "owner" && (
                              <Badge
                                variant="outline"
                                className="text-[9px] py-0 px-1.5 font-normal tracking-wide bg-primary/5 border-primary/20 text-primary rounded-md"
                              >
                                Owner
                              </Badge>
                            )}
                          </p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className="py-3 px-4">
                      {member.role === "owner" ? (
                        <Badge
                          variant="secondary"
                          className="capitalize text-[11px] font-medium bg-muted/65 rounded-md"
                        >
                          {member.role}
                        </Badge>
                      ) : (
                        <Select
                          defaultValue={member.role}
                          onValueChange={(val) =>
                            handleRoleChange(
                              member.userId,
                              val as "member" | "owner" | "admin" | "guest",
                            )
                          }
                        >
                          <SelectTrigger className="w-28 h-8 rounded-lg text-xs bg-card">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="member">Member</SelectItem>
                            <SelectItem value="admin">Admin</SelectItem>
                            <SelectItem value="guest">Guest</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    </TableCell>
                    <TableCell className="py-3 px-4 text-right">
                      {member.role !== "owner" && (
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label={`Remove ${member.name} from workspace`}
                          onClick={() =>
                            handleRemoveMember(member.userId, member.name)
                          }
                          className="rounded-lg hover:bg-destructive/5 text-muted-foreground hover:text-destructive transition size-8"
                        >
                          <UserMinus className="size-4" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </div>

      {/* Pending Invitations */}
      {invitations.length > 0 && (
        <div className="space-y-3 pt-4 border-t border-border/40">
          <h3 className="text-sm font-semibold text-foreground">
            Pending invitations ({invitations.length})
          </h3>
          <div className="rounded-xl border border-border/60 overflow-hidden bg-card">
            <Table>
              <TableHeader className="bg-muted/30">
                <TableRow className="hover:bg-transparent">
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">
                    Email
                  </TableHead>
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">
                    Role
                  </TableHead>
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">
                    Sent date
                  </TableHead>
                  <TableHead className="py-3 px-4 w-[80px]"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {invitations.map((inv: WorkspaceInvitation) => (
                  <TableRow
                    key={inv.id}
                    className="hover:bg-muted/10 transition-colors border-border/50"
                  >
                    <TableCell className="py-3 px-4 font-medium text-foreground text-sm">
                      {inv.email}
                    </TableCell>
                    <TableCell className="py-3 px-4">
                      <Badge
                        variant="secondary"
                        className="capitalize text-[11px] font-medium rounded-md"
                      >
                        {inv.role}
                      </Badge>
                    </TableCell>
                    <TableCell className="py-3 px-4 text-muted-foreground text-xs">
                      {new Date(inv.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell className="py-3 px-4 text-right">
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={`Revoke invitation to ${inv.email}`}
                        onClick={() =>
                          handleRevokeInvitation(inv.id, inv.email)
                        }
                        className="rounded-lg hover:bg-destructive/5 text-muted-foreground hover:text-destructive transition size-8"
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      )}
    </div>
  );
}
