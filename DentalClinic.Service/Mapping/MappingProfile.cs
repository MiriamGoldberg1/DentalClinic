using AutoMapper;
using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.DTOs.Common;
using DentalClinic.Core.DTOs.Doctors;
using DentalClinic.Core.DTOs.Patients;
using DentalClinic.Core.Entities;

namespace DentalClinic.Service.Mapping;

/// <summary>מיפוי AutoMapper בין הישויות של Core לבין ה-DTOs שיוצאים ונכנסים דרך ה-API.</summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Address, AddressDto>().ReverseMap();
        CreateMap<Specialization, SpecializationDto>();

        // מטופלים
        CreateMap<Patient, PatientResponseDto>();
        CreateMap<Patient, PatientSummaryDto>();
        CreateMap<Patient, PatientDetailsDto>()
            .ForMember(d => d.History, o => o.MapFrom(s => s.Appointments
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.Time)));
        CreateMap<PatientRequestDto, Patient>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.Appointments, o => o.Ignore());

        // רופאים
        CreateMap<Doctor, DoctorResponseDto>()
            .ForMember(d => d.Specializations, o => o.MapFrom(s => s.Specializations
                .OrderBy(x => x.Name)
                .Select(x => x.Name)));
        CreateMap<Doctor, DoctorSummaryDto>();
        CreateMap<DoctorRequestDto, Doctor>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Appointments, o => o.Ignore())
            .ForMember(d => d.Specializations, o => o.Ignore());

        // תורים
        CreateMap<Appointment, AppointmentResponseDto>();
        CreateMap<Appointment, AppointmentSummaryDto>()
            .ForMember(d => d.DoctorName, o => o.MapFrom(s => s.Doctor.Name));
    }
}
