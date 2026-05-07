using MediatR;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Enums;
using BoardEntity = Notrelix.Domain.Entities.Boards.Board;

namespace Notrelix.Application.Features.Boards.Board.Commands.CreateBoardCommand
{
    public record CreateBoardCommand(Guid WorkspaceId, Guid CreatedByUser, string Title, string Description, BoardVisibility Visibility) : IRequest<Result<Guid>>;

    public class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, Result<Guid>>
    {
        private readonly IApplicationDbContext _context;
        public CreateBoardCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Result<Guid>> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
        {
            var board = BoardEntity.Create(request.WorkspaceId, request.CreatedByUser, request.Title, request.Description, request.Visibility);

            _context.Boards.Add(board);

            await _context.SaveChangesAsync(cancellationToken);
            
            return Result<Guid>.Success(board.Id);
        }
    }
}