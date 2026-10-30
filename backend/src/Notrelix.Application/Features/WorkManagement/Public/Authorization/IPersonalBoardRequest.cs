namespace Notrelix.Application.Features.WorkManagement.Public.Authorization;

/// <summary>
/// Work Management-published marker for a write that only records one person's own choice about
/// a Board they can read and changes nothing the Board holds or anyone else sees: their view
/// preference (filter, sort, group, columns) and following or unfollowing an Item. Governance
/// reads it to let these through on an archived Board or in an archived Group, so a member can
/// still filter, sort and follow what stays readable (GOV-033). Reminders, comments and every
/// Item, field and view write reach other people or Board data and stay refused.
/// </summary>
public interface IPersonalBoardRequest;
