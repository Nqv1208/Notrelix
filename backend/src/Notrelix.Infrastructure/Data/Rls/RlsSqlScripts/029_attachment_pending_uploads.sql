-- Pending two-step attachment uploads (collab.attachment_pending_uploads). The row belongs to Collaboration, like the attachment it
-- becomes, and carries its account and workspace, so the standard business scope applies: the app reads and writes only rows of
-- workspaces it may access, the worker sees every row for the expiry sweep, and the support readonly role has no access by default.
SELECT ops.apply_scoped_business_policies('collab', 'attachment_pending_uploads', true);
