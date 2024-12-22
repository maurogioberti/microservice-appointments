﻿using Microservice.Appointments.Application.Configuration;
using Microservice.Appointments.Application.Dtos.Appointments;
using Microservice.Appointments.Application.Helpers;
using Microservice.Appointments.Application.Repositories;
using Microservice.Appointments.Application.UseCases.Abstractions;
using Microservice.Appointments.Application.UseCases.Mappers.Abstractions;
using Microservice.Appointments.Domain.Enums;
using Microservice.Appointments.Domain.Events;
using Microservice.Appointments.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Microservice.Appointments.Application.UseCases;

public class UpdateAppointmentStatusUseCase(
    IAppointmentRepository appointmentRepository,
    IAppointmentMapper appointmentMapper,
    IEventBus eventBus,
    ILogger<UpdateAppointmentStatusUseCase> logger) : IUpdateAppointmentStatusUseCase
{
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
    private readonly IAppointmentMapper _appointmentMapper = appointmentMapper ?? throw new ArgumentNullException(nameof(appointmentMapper));
    private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    private readonly ILogger<UpdateAppointmentStatusUseCase> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private const string ValidationErrorMessage = "Validation error occurred while updating the appointment status.";

    public async Task<AppointmentDto> ExecuteAsync(int id, AppointmentStatus status)
    {
        try
        {
            var appointment = await _appointmentRepository.GetAsync(id);
            if (appointment is null)
                throw new NotFoundException($"Appointment with id '{id}' was not found.");

            appointment.UpdateStatus(status);

            var appointmentUpdated = await _appointmentRepository.UpdateAsync(appointment);

            var eventMessage = _appointmentMapper.ToChangedMessage(appointmentUpdated);

            var eventName = EventHelper.GetEventName<AppointmentChangedEvent>();
            await _eventBus.PublishAsync(eventMessage, eventName);

            return _appointmentMapper.ToDto(appointmentUpdated);
        }
        catch (DomainValidationException exception)
        {
            _logger.LogWarning(exception, ValidationErrorMessage);
            throw new BadRequestException(ValidationErrorMessage, exception);
        }
    }
}