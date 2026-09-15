using LMS.Application.DTOs.MasterData;

namespace LMS.Application.Interfaces;

public interface IMasterDataService
{
    Task<SeedStatusDto> GetSeedStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> TriggerReseedAsync(string section, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemConfigDto>> GetSystemConfigsAsync(CancellationToken cancellationToken = default);
    Task<SystemConfigDto?> GetSystemConfigAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemConfigDto> UpdateSystemConfigAsync(string key, UpdateSystemConfigDto dto, CancellationToken cancellationToken = default);
}
