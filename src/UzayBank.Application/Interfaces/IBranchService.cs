using UzayBank.Application.DTOs;

namespace UzayBank.Application.Interfaces;

public interface IBranchService
{
    Task<List<BranchDto>> GetNearestAsync(double latitude, double longitude, int distanceLimitKm);
}