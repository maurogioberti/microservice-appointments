﻿using AutoFixture;
using Microservice.Appointments.Application.UseCases.Mappers;
using Microservice.Appointments.Domain.Enums;
using Microservice.Appointments.Domain.Models;
using Xunit;

namespace Microservice.Appointments.UnitTests.Application.UseCases.Mappers;

public class AppointmentMapperTests
{
    private const int DaysInPast = -1;
    private const int DaysInFuture = 1;

    private readonly Fixture _fixture = new();

    private static AppointmentDomain CreateDomain(Fixture fixture)
        => AppointmentDomain.Hydrate(
            fixture.Create<int>(),
            fixture.Create<string>(),
            DateTime.UtcNow.AddDays(DaysInPast),
            DateTime.UtcNow.AddDays(DaysInFuture),
            fixture.Create<string>(),
            fixture.Create<AppointmentStatus>()
        );

    [Fact]
    public void Given_Appointment_Domain_When_To_Dto_Is_Called_Then_Returns_Correct_Appointment_Dto()
    {
        // Arrange
        var mapper = new AppointmentMapper();
        var domain = CreateDomain(_fixture);

        // Act
        var dto = mapper.ToDto(domain);

        // Assert
        Assert.Equal(domain.Id, dto.Id);
        Assert.Equal(domain.Title, dto.Title);
        Assert.Equal(domain.StartTime, dto.StartTime);
        Assert.Equal(domain.EndTime, dto.EndTime);
        Assert.Equal(domain.Description, dto.Description);
        Assert.Equal(domain.Status, dto.Status);
    }

    [Fact]
    public void Given_Appointment_Domain_When_To_Appointment_Created_Message_Is_Called_Then_Returns_Correct_Event()
    {
        // Arrange
        var mapper = new AppointmentMapper();
        var domain = CreateDomain(_fixture);

        // Act
        var eventMessage = mapper.ToCreatedMessage(domain);

        // Assert
        Assert.Equal(domain.Id, eventMessage.AppointmentId);
        Assert.Equal(domain.Title, eventMessage.Title);
        Assert.Equal(domain.StartTime, eventMessage.StartTime);
        Assert.Equal(domain.EndTime, eventMessage.EndTime);
        Assert.Equal(domain.Description, eventMessage.Description);
        Assert.Equal(domain.Status, eventMessage.Status);
    }

    [Fact]
    public void Given_Appointment_Domain_When_To_Appointment_Changed_Message_Is_Called_Then_Returns_Correct_Event()
    {
        // Arrange
        var mapper = new AppointmentMapper();
        var domain = CreateDomain(_fixture);

        // Act
        var eventMessage = mapper.ToChangedMessage(domain);

        // Assert
        Assert.Equal(domain.Id, eventMessage.AppointmentId);
        Assert.Equal(domain.Title, eventMessage.Title);
        Assert.Equal(domain.StartTime, eventMessage.StartTime);
        Assert.Equal(domain.EndTime, eventMessage.EndTime);
        Assert.Equal(domain.Description, eventMessage.Description);
        Assert.Equal(domain.Status, eventMessage.Status);
    }

    [Fact]
    public void Given_Appointment_Domain_When_To_Appointment_Deleted_Message_Is_Called_Then_Returns_Correct_Event()
    {
        // Arrange
        var mapper = new AppointmentMapper();
        var domain = CreateDomain(_fixture);

        // Act
        var eventMessage = mapper.ToDeletedMessage(domain);

        // Assert
        Assert.Equal(domain.Id, eventMessage.AppointmentId);
        Assert.Equal(domain.Title, eventMessage.Title);
        Assert.Equal(domain.StartTime, eventMessage.StartTime);
        Assert.Equal(domain.EndTime, eventMessage.EndTime);
        Assert.Equal(domain.Description, eventMessage.Description);
        Assert.Equal(domain.Status, eventMessage.Status);
    }
}