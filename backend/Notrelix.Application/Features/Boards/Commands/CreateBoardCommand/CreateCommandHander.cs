using MediatR;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Entities.Boardss;
using Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boardss.Commands.CreateBoardCommand
{
    public record CreateBoardCommand(Guid WorkspaceId, Guid CreatedByUser, string Title, string Description, BoardVisibility Visibility) : IRequest<Result<Guid>>;

    public class CreateCommandHander : IRequestHandler<CreateBoardCommand, Result<Guid>>
    {
        private readonly IApplicationDbContext _context;
        public CreateCommandHander(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Result<Guid>> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
        {
            var board = Board.Create(request.WorkspaceId, request.CreatedByUser, request.Title, request.Description, request.Visibility);

            _context.Boards.Add(board);

            await _context.SaveChangesAsync(cancellationToken);
            
            return Result<Guid>.Success(board.Id);
        }
    }
}