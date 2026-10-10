using AutoMapper;
using DentalClinic.Core.Common;

namespace DentalClinic.Service.Mapping;

internal static class MapperExtensions
{
    /// <summary>ממפה תוצאת דפדוף של ישויות לתוצאת דפדוף של DTOs (שומר את נתוני הדפדוף).</summary>
    public static PagedResult<TDestination> MapPaged<TSource, TDestination>(this IMapper mapper, PagedResult<TSource> source)
    {
        var items = mapper.Map<List<TDestination>>(source.Items);
        return new PagedResult<TDestination>(items, source.Page, source.PageSize, source.TotalCount);
    }
}
