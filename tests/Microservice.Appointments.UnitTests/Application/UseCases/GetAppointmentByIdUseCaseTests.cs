﻿using AutoFixture;
using Microservice.Appointments.Application.Dtos.Appointments;
using Microservice.Appointments.Application.Repositories;
using Microservice.Appointments.Application.UseCases;
using Microservice.Appointments.Application.UseCases.Mappers.Abstractions;
using Microservice.Appointments.Domain.Enums;
using Microservice.Appointments.Domain.Exceptions;
using Microservice.Appointments.Domain.Models;
using Moq;
using Xunit;

namespace Microservice.Appointments.UnitTests.Application.UseCases;

public class GetAppointmentByIdUseCaseTests
{
    private const int InvalidId = -1;

    #region Builder

    private class Builder
    {
        public Fixture Fixture { get; } = new Fixture();
        public Mock<IAppointmentRepository> MockRepository { get; } = new();
        public Mock<IAppointmentMapper> MockMapper { get; } = new();

        public GetAppointmentByIdUseCase Build()
        {
            return new GetAppointmentByIdUseCase(MockRepository.Object, MockMapper.Object);
        }

        public AppointmentDomain BuildDomain()
            => AppointmentDomain.Hydrate(
                Fixture.Create<int>(),
                Fixture.Create<string>(),
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                Fixture.Create<string>(),
                Fixture.Create<AppointmentStatus>()
            );
    }

    #endregion Builder

    [Fact]
    public async Task Given_InvalidId_When_ExecuteAsync_Then_ThrowsBadRequestException()
    {
        // Arrange
        var builder = new Builder();
        var useCase = builder.Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            useCase.ExecuteAsync(InvalidId));

        Assert.Equal($"Appointment with id '{InvalidId}' is invalid.", exception.Message);
    }

    [Fact]
    public async Task Given_IdNotFound_When_ExecuteAsync_Then_ThrowsNotFoundException()
    {
        // Arrange
        var builder = new Builder();
        var validId = builder.Fixture.Create<int>();
        builder.MockRepository
            .Setup(repo => repo.GetAsync(validId))!
            .ReturnsAsync((AppointmentDomain)null!);

        var useCase = builder.Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(validId));

        Assert.Equal($"Appointment with id '{validId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task Given_ValidId_When_ExecuteAsync_Then_ReturnsAppointmentDto()
    {
        // Arrange
        var builder = new Builder();
        var validId = builder.Fixture.Create<int>();
        var appointmentDomain = builder.BuildDomain();
        var appointmentDto = builder.Fixture.Create<AppointmentDto>();

        builder.MockRepository
            .Setup(repo => repo.GetAsync(validId))
            .ReturnsAsync(appointmentDomain);

        builder.MockMapper
            .Setup(mapper => mapper.ToDto(appointmentDomain))
            .Returns(appointmentDto);

        var useCase = builder.Build();

        // Act
        var result = await useCase.ExecuteAsync(validId);

        // Assert
        Assert.Equal(appointmentDto, result);

        builder.MockRepository.Verify(repo => repo.GetAsync(validId), Times.Once);
        builder.MockMapper.Verify(mapper => mapper.ToDto(appointmentDomain), Times.Once);
    }

    [Fact]
    public void Given_NullDependencies_When_Constructed_Then_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetAppointmentByIdUseCase(null!, new Mock<IAppointmentMapper>().Object));

        Assert.Throws<ArgumentNullException>(() =>
            new GetAppointmentByIdUseCase(new Mock<IAppointmentRepository>().Object, null!));
    }
}