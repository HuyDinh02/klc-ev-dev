using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KCharge.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AuditLogging;

namespace KCharge.AuditLogs;

[Authorize(KChargePermissions.AuditLogs.Default)]
public class AuditLogAppService : KChargeAppService, IAuditLogAppService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogAppService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResultDto<AuditLogListDto>> GetListAsync(GetAuditLogListDto input)
    {
        HttpStatusCode? statusCode = input.HttpStatusCode.HasValue
            ? (HttpStatusCode)input.HttpStatusCode.Value
            : null;

        var totalCount = await _auditLogRepository.GetCountAsync(
            startTime: input.StartTime,
            endTime: input.EndTime,
            httpMethod: input.HttpMethod,
            url: input.Url,
            userName: input.UserName,
            minExecutionDuration: input.MinExecutionDuration,
            maxExecutionDuration: input.MaxExecutionDuration,
            hasException: input.HasException,
            httpStatusCode: statusCode
        );

        var logs = await _auditLogRepository.GetListAsync(
            sorting: "ExecutionTime DESC",
            maxResultCount: input.MaxResultCount,
            skipCount: 0,
            startTime: input.StartTime,
            endTime: input.EndTime,
            httpMethod: input.HttpMethod,
            url: input.Url,
            userName: input.UserName,
            minExecutionDuration: input.MinExecutionDuration,
            maxExecutionDuration: input.MaxExecutionDuration,
            hasException: input.HasException,
            httpStatusCode: statusCode
        );

        var dtos = logs.Select(log => new AuditLogListDto
        {
            Id = log.Id,
            UserName = log.UserName,
            ExecutionTime = log.ExecutionTime,
            HttpMethod = log.HttpMethod ?? string.Empty,
            Url = log.Url ?? string.Empty,
            HttpStatusCode = log.HttpStatusCode,
            ExecutionDuration = log.ExecutionDuration
        }).ToList();

        return new PagedResultDto<AuditLogListDto>(totalCount, dtos);
    }

    public async Task<AuditLogDto> GetAsync(Guid id)
    {
        var log = await _auditLogRepository.GetAsync(id);

        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = log.UserName,
            ImpersonatorUserId = log.ImpersonatorUserId?.ToString(),
            ImpersonatorUserName = log.ImpersonatorUserName,
            ExecutionTime = log.ExecutionTime,
            ExecutionDuration = log.ExecutionDuration,
            ClientIpAddress = log.ClientIpAddress,
            ClientName = log.ClientName,
            BrowserInfo = log.BrowserInfo,
            HttpMethod = log.HttpMethod ?? string.Empty,
            Url = log.Url ?? string.Empty,
            HttpStatusCode = log.HttpStatusCode,
            Comments = log.Comments
        };
    }

    public async Task<PagedResultDto<EntityChangeDto>> GetEntityChangesAsync(GetEntityChangesDto input)
    {
        // Get entity changes from audit logs
        var logs = await _auditLogRepository.GetListAsync(
            sorting: "ExecutionTime DESC",
            maxResultCount: input.MaxResultCount,
            skipCount: 0,
            startTime: input.StartTime,
            endTime: input.EndTime,
            includeDetails: true
        );

        var entityChanges = logs
            .SelectMany(log => log.EntityChanges.Select(ec => new { Log = log, Change = ec }))
            .Where(x => string.IsNullOrEmpty(input.EntityTypeFullName) ||
                        x.Change.EntityTypeFullName.Contains(input.EntityTypeFullName))
            .Where(x => string.IsNullOrEmpty(input.EntityId) ||
                        x.Change.EntityId == input.EntityId)
            .Where(x => string.IsNullOrEmpty(input.ChangeType) ||
                        x.Change.ChangeType.ToString() == input.ChangeType)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = entityChanges.Select(x => new EntityChangeDto
        {
            Id = x.Change.Id,
            AuditLogId = x.Log.Id,
            ChangeTime = x.Change.ChangeTime,
            ChangeType = x.Change.ChangeType.ToString(),
            EntityTypeFullName = x.Change.EntityTypeFullName,
            EntityId = x.Change.EntityId
        }).ToList();

        return new PagedResultDto<EntityChangeDto>(entityChanges.Count, dtos);
    }

    public async Task<List<EntityPropertyChangeDto>> GetPropertyChangesAsync(Guid entityChangeId)
    {
        // Find the entity change across all audit logs
        var logs = await _auditLogRepository.GetListAsync(
            sorting: "ExecutionTime DESC",
            maxResultCount: 1000,
            skipCount: 0,
            includeDetails: true
        );

        var entityChange = logs
            .SelectMany(log => log.EntityChanges)
            .FirstOrDefault(ec => ec.Id == entityChangeId);

        if (entityChange == null)
        {
            return new List<EntityPropertyChangeDto>();
        }

        return entityChange.PropertyChanges.Select(prop => new EntityPropertyChangeDto
        {
            Id = prop.Id,
            EntityChangeId = prop.EntityChangeId,
            PropertyName = prop.PropertyName,
            OriginalValue = prop.OriginalValue,
            NewValue = prop.NewValue,
            PropertyTypeFullName = prop.PropertyTypeFullName
        }).ToList();
    }

    [Authorize(KChargePermissions.AuditLogs.Export)]
    public async Task<byte[]> ExportToCsvAsync(GetAuditLogListDto input)
    {
        HttpStatusCode? statusCode = input.HttpStatusCode.HasValue
            ? (HttpStatusCode)input.HttpStatusCode.Value
            : null;

        var logs = await _auditLogRepository.GetListAsync(
            sorting: "ExecutionTime DESC",
            maxResultCount: 10000,
            skipCount: 0,
            startTime: input.StartTime,
            endTime: input.EndTime,
            httpMethod: input.HttpMethod,
            url: input.Url,
            userName: input.UserName,
            minExecutionDuration: input.MinExecutionDuration,
            maxExecutionDuration: input.MaxExecutionDuration,
            hasException: input.HasException,
            httpStatusCode: statusCode
        );

        var sb = new StringBuilder();
        sb.AppendLine("Id,UserName,ExecutionTime,HttpMethod,Url,HttpStatusCode,ExecutionDuration,ClientIpAddress");

        foreach (var log in logs)
        {
            sb.AppendLine($"\"{log.Id}\",\"{log.UserName}\",\"{log.ExecutionTime:yyyy-MM-dd HH:mm:ss}\",\"{log.HttpMethod}\",\"{EscapeCsv(log.Url)}\",{log.HttpStatusCode},{log.ExecutionDuration},\"{log.ClientIpAddress}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}
