// Common
global using Notrelix.Domain.Common;
global using Notrelix.Domain.Common.Exceptions;

// SharedKernel
global using Notrelix.Domain.SharedKernel;

// Bounded Contexts - Identity
global using Notrelix.Domain.Identity;
global using Notrelix.Domain.Identity.Users;
global using Notrelix.Domain.Identity.Users.Events;
global using Notrelix.Domain.Identity.Profiles;
global using Notrelix.Domain.Identity.Profiles.Events;
global using Notrelix.Domain.Identity.Sessions;
global using Notrelix.Domain.Identity.Sessions.Events;
global using Notrelix.Domain.Identity.OAuth;
global using Notrelix.Domain.Identity.OAuth.Events;
global using Notrelix.Domain.Identity.Security;
global using Notrelix.Domain.Identity.Security.Events;
global using Notrelix.Domain.Identity.Mfa;
global using Notrelix.Domain.Identity.Mfa.Events;
global using Notrelix.Domain.Identity.Tokens;
global using Notrelix.Domain.Identity.Tokens.Events;

// Bounded Contexts - Workspaces
global using Notrelix.Domain.Workspaces;
global using Notrelix.Domain.Workspaces.Workspaces;
global using Notrelix.Domain.Workspaces.Members;
global using Notrelix.Domain.Workspaces.Invitations;
global using Notrelix.Domain.Workspaces.Spaces;
global using Notrelix.Domain.Workspaces.Teams;
global using Notrelix.Domain.Workspaces.Rules;

// Bounded Contexts - Governance
global using Notrelix.Domain.Governance;
global using Notrelix.Domain.Governance.Permissions;
global using Notrelix.Domain.Governance.ShareLinks;
global using Notrelix.Domain.Governance.Roles;
global using Notrelix.Domain.Governance.Policies;
global using Notrelix.Domain.Governance.Audit;
global using Notrelix.Domain.Governance.Security;
global using Notrelix.Domain.Governance.Templates;

// Bounded Contexts - WorkManagement
global using Notrelix.Domain.WorkManagement;
global using Notrelix.Domain.WorkManagement.Boards;
global using Notrelix.Domain.WorkManagement.BoardGroups;
global using Notrelix.Domain.WorkManagement.Fields;
global using Notrelix.Domain.WorkManagement.Items;
global using Notrelix.Domain.WorkManagement.Views;
global using Notrelix.Domain.WorkManagement.Checklists;
global using Notrelix.Domain.WorkManagement.Labels;
global using Notrelix.Domain.WorkManagement.Templates;
global using Notrelix.Domain.WorkManagement.Relations;
global using Notrelix.Domain.WorkManagement.Formulas;
global using Notrelix.Domain.WorkManagement.Workload;
global using Notrelix.Domain.WorkManagement.Approvals;

// Bounded Contexts - Documents
global using Notrelix.Domain.Documents;
global using Notrelix.Domain.Documents.Pages;
global using Notrelix.Domain.Documents.Blocks;
global using Notrelix.Domain.Documents.Versions;
global using Notrelix.Domain.Documents.ResourceLinks;
global using Notrelix.Domain.Documents.Templates;
global using Notrelix.Domain.Documents.Rules;

// Bounded Contexts - Collaboration
global using Notrelix.Domain.Collaboration;
global using Notrelix.Domain.Collaboration.Comments;
global using Notrelix.Domain.Collaboration.Reactions;
global using Notrelix.Domain.Collaboration.Mentions;
global using Notrelix.Domain.Collaboration.Notifications;
global using Notrelix.Domain.Collaboration.Activity;
global using Notrelix.Domain.Collaboration.Attachments;
global using Notrelix.Domain.Collaboration.Watchers;
global using Notrelix.Domain.Collaboration.Presence;

// Bounded Contexts - Integrations
global using Notrelix.Domain.Integrations;
global using Notrelix.Domain.Integrations.Connections;
global using Notrelix.Domain.Integrations.Webhooks;
global using Notrelix.Domain.Integrations.Calendar;
global using Notrelix.Domain.Integrations.Sync;

// Bounded Contexts - Automation
global using Notrelix.Domain.Automation;
global using Notrelix.Domain.Automation.Rules;
global using Notrelix.Domain.Automation.Triggers;
global using Notrelix.Domain.Automation.Conditions;
global using Notrelix.Domain.Automation.Actions;
global using Notrelix.Domain.Automation.Executions;
global using Notrelix.Domain.Automation.Scheduled;
global using Notrelix.Domain.Automation.Templates;
global using Notrelix.Domain.Automation.RulesEngine;

// Bounded Contexts - Billing
global using Notrelix.Domain.Billing;
global using Notrelix.Domain.Billing.Plans;
global using Notrelix.Domain.Billing.Subscriptions;
global using Notrelix.Domain.Billing.Payments;
global using Notrelix.Domain.Billing.Usage;
global using Notrelix.Domain.Billing.Entitlements;

// Bounded Contexts - Analytics
global using Notrelix.Domain.Analytics;
