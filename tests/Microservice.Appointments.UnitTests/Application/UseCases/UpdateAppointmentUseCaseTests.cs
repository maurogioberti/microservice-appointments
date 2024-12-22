using AutoFixture;
using Microservice.Appointments.Application.Configuration;
using Microservice.Appointments.Application.Dtos.Appointments;
using Microservice.Appointments.Application.Repositories;
using Microservice.Appointments.Application.UseCases;
using Microservice.Appointments.Application.UseCases.Mappers.Abstractions;
using Microservice.Appointments.Domain.Enums;
using Microservice.Appointments.Domain.Events;
using Microservice.Appointments.Domain.Exceptions;
using Microservice.Appointments.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microservice.Appointments.UnitTests.Application.UseCases;

public class UpdateAppointmentUseCaseTests
{
    private const string ValidationErrorMessage = "Validation error occurred while updating an appointment.";
    private const string EventBusFailureMessage = "Event bus failure";
    private const int DaysInPast = -1;
    private const int DaysInFuture = 1;
    private const int HoursToAdd = 1;

    #region Builder

    private class Builder
    {
        public Fixture Fixture { get; } = new Fixture();
        public Mock<IAppointmentRepository> MockRepository { get; } = new();
        public Mock<IAppointmentMapper> MockMapper { get; } = new();
        public Mock<IEventBus> MockEventBus { get; } = new();
        public Mock<ILogger<UpdateAppointmentUseCase>> MockLogger { get; } = new();

        public UpdateAppointmentUseCase Build()
        {
            return new UpdateAppointmentUseCase(
                MockRepository.Object,
                MockMapper.Object,
                MockEventBus.Object,
                MockLogger.Object);
        }

        public AppointmentDomain BuildDomain()
            => AppointmentDomain.Hydrate(
                Fixture.Create<int>(),
                Fixture.Create<string>(),
                DateTime.UtcNow.AddDays(DaysInPast),
                DateTime.UtcNow.AddDays(DaysInFuture),
                Fixture.Create<string>(),
                Fixture.Create<AppointmentStatus>()
            );
    }

    #endregion Builder

    [Fact]
    public async Task Given_ValidParameters_When_ExecuteAsync_Then_UpdatesAppointmentSuccessfully()
    {
        // Arrange
        var builder = new Builder();
        var domainAppointment = builder.BuildDomain();
        var updatedEntity = builder.BuildDomain();
        var appointmentDto = builder.Fixture.Create<AppointmentDto>();
        var eventMessage = builder.Fixture.Create<AppointmentChangedEvent>();

        builder.MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(domainAppointment);

        builder.MockRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<AppointmentDomain>()))
            .ReturnsAsync(updatedEntity);

        builder.MockMapper
            .Setup(mapper => mapper.ToChangedMessage(It.IsAny<AppointmentDomain>()))
            .Returns(eventMessage);

        builder.MockMapper
            .Setup(mapper => mapper.ToDto(It.IsAny<AppointmentDomain>()))
            .Returns(appointmentDto);

        builder.MockEventBus
            .Setup(bus => bus.PublishAsync(It.IsAny<AppointmentChangedEvent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var useCase = builder.Build();

        // Act
        var result = await useCase.ExecuteAsync(domainAppointment.Id, domainAppointment.Title, domainAppointment.StartTime, domainAppointment.EndTime, domainAppointment.Description, domainAppointment.Status);

        // Assert
        Assert.Equal(appointmentDto, result);

        builder.MockRepository.Verify(repo => repo.GetAsync(It.IsAny<int>()), Times.Once);
        builder.MockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<AppointmentDomain>()), Times.Once);
        builder.MockMapper.Verify(mapper => mapper.ToDto(It.IsAny<AppointmentDomain>()), Times.Once);
        builder.MockEventBus.Verify(bus => bus.PublishAsync(eventMessage, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Given_DomainValidationException_When_ExecuteAsync_Then_ThrowsBadRequestException()
    {
        // Arrange
        var builder = new Builder();
        var validationException = new DomainValidationException(builder.Fixture.Create<string>());

        builder.MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(builder.BuildDomain());

        builder.MockRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<AppointmentDomain>()))
            .Throws(validationException);

        var useCase = builder.Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            useCase.ExecuteAsync(builder.Fixture.Create<int>(), builder.Fixture.Create<string>(), DateTime.UtcNow, DateTime.UtcNow.AddHours(HoursToAdd), builder.Fixture.Create<string>(), builder.Fixture.Create<AppointmentStatus>()));

        Assert.Equal(ValidationErrorMessage, exception.Message);

        builder.MockLogger.Verify(logger =>
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(ValidationErrorMessage)),
                    validationException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Fact]
    public async Task Given_EventBusError_When_ExecuteAsync_Then_ThrowsException()
    {
        // Arrange
        var builder = new Builder();
        var updatedEntity = builder.BuildDomain();
        var eventBusException = new Exception(EventBusFailureMessage);

        builder.MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(builder.BuildDomain());

        builder.MockRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<AppointmentDomain>()))
            .ReturnsAsync(updatedEntity);

        builder.MockEventBus
            .Setup(bus => bus.PublishAsync(It.IsAny<AppointmentChangedEvent>(), It.IsAny<string>()))
            .Throws(eventBusException);

        var useCase = builder.Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            useCase.ExecuteAsync(builder.Fixture.Create<int>(), builder.Fixture.Create<string>(), DateTime.UtcNow, DateTime.UtcNow.AddHours(HoursToAdd), builder.Fixture.Create<string>(), builder.Fixture.Create<AppointmentStatus>()));

        Assert.Equal(EventBusFailureMessage, exception.Message);
    }

    [Fact]
    public void Given_NullDependencies_When_Constructed_Then_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UpdateAppointmentUseCase(null!, new Mock<IAppointmentMapper>().Object, new Mock<IEventBus>().Object, new Mock<ILogger<UpdateAppointmentUseCase>>().Object));

        Assert.Throws<ArgumentNullException>(() =>
            new UpdateAppointmentUseCase(new Mock<IAppointmentRepository>().Object, null!, new Mock<IEventBus>().Object, new Mock<ILogger<UpdateAppointmentUseCase>>().Object));

        Assert.Throws<ArgumentNullException>(() =>
            new UpdateAppointmentUseCase(new Mock<IAppointmentRepository>().Object, new Mock<IAppointmentMapper>().Object, null!, new Mock<ILogger<UpdateAppointmentUseCase>>().Object));

        Assert.Throws<ArgumentNullException>(() =>
            new UpdateAppointmentUseCase(new Mock<IAppointmentRepository>().Object, new Mock<IAppointmentMapper>().Object, new Mock<IEventBus>().Object, null!));
    }

    [Fact]
    public async Task Given_AppointmentNotFound_When_ExecuteAsync_Then_ThrowsNotFoundException()
    {
        // Arrange
        var builder = new Builder();
        var appointmentId = builder.Fixture.Create<int>();

        builder.MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))!
            .ReturnsAsync((AppointmentDomain)null!);

        var useCase = builder.Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(appointmentId, builder.Fixture.Create<string>(), DateTime.UtcNow, DateTime.UtcNow.AddHours(HoursToAdd), builder.Fixture.Create<string>(), builder.Fixture.Create<AppointmentStatus>()));

        Assert.Equal($"Appointment with id '{appointmentId}' was not found.", exception.Message);

        builder.MockRepository.Verify(repo => repo.GetAsync(It.IsAny<int>()), Times.Once);
    }
}