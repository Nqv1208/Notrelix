namespace Notrelix.Domain.Common.Exceptions;

/// <summary>
/// Exception khi không tìm thấy entity
/// </summary>
public class NotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public NotFoundException(string entityName, object entityId)
        : base($"Entity '{entityName}' với ID '{entityId}' không tồn tại.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public static NotFoundException For<TEntity>(object id)
    {
        return new NotFoundException(typeof(TEntity).Name, id);
    }
}
