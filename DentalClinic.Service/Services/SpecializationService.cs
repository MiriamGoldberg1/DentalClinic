using AutoMapper;
using DentalClinic.Core.DTOs.Common;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;

namespace DentalClinic.Service.Services;

public class SpecializationService : ISpecializationService
{
    private readonly ISpecializationRepository _specializations;
    private readonly IMapper _mapper;

    public SpecializationService(ISpecializationRepository specializations, IMapper mapper)
    {
        _specializations = specializations;
        _mapper = mapper;
    }

    /// <summary>שליפת כל תחומי ההתמחות.</summary>
    public async Task<List<SpecializationDto>> GetAllAsync(CancellationToken ct)
    {
        var specializations = await _specializations.GetAllAsync(ct);
        return _mapper.Map<List<SpecializationDto>>(specializations);
    }
}
