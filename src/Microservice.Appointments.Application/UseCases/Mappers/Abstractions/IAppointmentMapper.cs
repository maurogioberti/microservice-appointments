using Microservice.Appointments.Application.Dtos.Appointments;
using Microservice.Appointments.Domain.Models;

namespace Microservice.Appointments.Application.UseCases.Mappers.Abstractions;

public interface IAppointmentMapper
{
    AppointmentDto ToDto(Appointment appointment);
}