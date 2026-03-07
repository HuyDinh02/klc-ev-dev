using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.MobileUsers;
using KLC.Permissions;
using KLC.Support;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace KLC.Feedback;

[Authorize(KLCPermissions.Feedback.Default)]
public class FeedbackAdminAppService : KLCAppService, IFeedbackAdminAppService
{
    private readonly IRepository<UserFeedback, Guid> _feedbackRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;

    public FeedbackAdminAppService(
        IRepository<UserFeedback, Guid> feedbackRepository,
        IRepository<AppUser, Guid> appUserRepository)
    {
        _feedbackRepository = feedbackRepository;
        _appUserRepository = appUserRepository;
    }

    public async Task<CursorPagedResultDto<FeedbackListDto>> GetListAsync(GetFeedbackListDto input)
    {
        var pageSize = input.PageSize;
        if (pageSize <= 0 || pageSize > 50) pageSize = 20;

        var query = await _feedbackRepository.GetQueryableAsync();
        query = query.Where(f => !f.IsDeleted);

        if (input.Status.HasValue) query = query.Where(f => f.Status == input.Status.Value);
        if (input.Type.HasValue) query = query.Where(f => f.Type == input.Type.Value);

        if (input.Cursor.HasValue)
        {
            var cursorFeedback = await _feedbackRepository.FirstOrDefaultAsync(f => f.Id == input.Cursor.Value);
            if (cursorFeedback != null)
                query = query.Where(f => f.CreationTime < cursorFeedback.CreationTime);
        }

        query = query.OrderByDescending(f => f.CreationTime);

        var items = await AsyncExecuter.ToListAsync(query.Take(pageSize + 1));
        var hasMore = items.Count > pageSize;
        var data = hasMore ? items.Take(pageSize).ToList() : items;

        // Resolve user names
        var userIds = data.Select(f => f.UserId).Distinct().ToList();
        var users = await _appUserRepository.GetListAsync(u => userIds.Contains(u.IdentityUserId));
        var userNameMap = users.ToDictionary(u => u.IdentityUserId, u => u.FullName);

        var dtos = data.Select(f => new FeedbackListDto
        {
            Id = f.Id,
            UserId = f.UserId,
            UserName = userNameMap.GetValueOrDefault(f.UserId),
            Type = f.Type,
            Subject = f.Subject,
            Status = f.Status,
            AdminResponse = f.AdminResponse,
            RespondedAt = f.RespondedAt,
            RespondedBy = f.RespondedBy,
            CreatedAt = f.CreationTime
        }).ToList();

        return new CursorPagedResultDto<FeedbackListDto>
        {
            Data = dtos,
            Pagination = new CursorPaginationDto
            {
                HasMore = hasMore,
                PageSize = pageSize
            }
        };
    }

    public async Task<FeedbackDetailDto> GetAsync(Guid id)
    {
        var feedback = await _feedbackRepository.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (feedback == null)
            throw new BusinessException(KLCDomainErrorCodes.Feedback.NotFound);

        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == feedback.UserId);

        return new FeedbackDetailDto
        {
            Id = feedback.Id,
            UserId = feedback.UserId,
            UserName = user?.FullName,
            Type = feedback.Type,
            Subject = feedback.Subject,
            Message = feedback.Message,
            Status = feedback.Status,
            AdminResponse = feedback.AdminResponse,
            RespondedAt = feedback.RespondedAt,
            RespondedBy = feedback.RespondedBy,
            CreatedAt = feedback.CreationTime
        };
    }

    [Authorize(KLCPermissions.Feedback.Respond)]
    public async Task RespondAsync(Guid id, RespondToFeedbackDto input)
    {
        var feedback = await _feedbackRepository.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (feedback == null)
            throw new BusinessException(KLCDomainErrorCodes.Feedback.NotFound);

        feedback.Resolve(input.Response, CurrentUser.GetId());
        await _feedbackRepository.UpdateAsync(feedback);
    }

    [Authorize(KLCPermissions.Feedback.Respond)]
    public async Task CloseAsync(Guid id)
    {
        var feedback = await _feedbackRepository.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (feedback == null)
            throw new BusinessException(KLCDomainErrorCodes.Feedback.NotFound);

        feedback.Close();
        await _feedbackRepository.UpdateAsync(feedback);
    }
}
