using AutoFixture;
using Microservice.Appointments.Api.Requests;
using Microservice.Appointments.Application.Dtos.Appointments;
using Microservice.Appointments.Domain.Enums;
using Microservice.Appointments.Domain.Exceptions;
using Microservice.Appointments.Domain.Models;
using Microservice.Appointments.FunctionalTests.Appointment.Base;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Microservice.Appointments.FunctionalTests.Appointment;

public class AppointmentUpdateStatusTests : AppointmentTestsBase
{
    private const string ValidationErrorMessage = "Validation error occurred while updating the appointment status.";

    [Fact]
    public async Task Given_Valid_Appointment_When_UpdateStatus_Called_Then_Returns_Ok()
    {
        // Arrange
        var controller = CreateController();
        var appointmentDomain = CreateDomain();
        var request = new UpdateAppointmentStatusRequest(AppointmentStatus.Completed);

        MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(appointmentDomain);

        MockRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<AppointmentDomain>()))
            .ReturnsAsync(appointmentDomain);

        // Act
        var response = await controller.UpdateStatus(appointmentDomain.Id, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<AppointmentDto>>(response);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<AppointmentDto>(okResult.Value);

        Assert.Equal(appointmentDomain.Id, result.Id);
        Assert.Equal(request.Status, result.Status);
    }

    [Fact]
    public async Task Given_Non_Existent_Appointment_When_UpdateStatus_Called_Then_Throws_NotFoundException()
    {
        // Arrange
        var controller = CreateController();
        var nonExistentId = Fixture.Create<int>();
        var request = new UpdateAppointmentStatusRequest(AppointmentStatus.Completed);

        MockRepository
            .Setup(repo => repo.GetAsync(nonExistentId))
            .ReturnsAsync((AppointmentDomain)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            controller.UpdateStatus(nonExistentId, request));

        Assert.Equal($"Appointment with id '{nonExistentId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task Given_Valid_Appointment_When_UpdateStatus_Called_With_Invalid_Status_Then_Throws_BadRequestException()
    {
        // Arrange
        var controller = CreateController();
        var appointmentDomain = CreateDomain();
        var request = new UpdateAppointmentStatusRequest((AppointmentStatus)999);

        MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(appointmentDomain);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            controller.UpdateStatus(appointmentDomain.Id, request));

        Assert.Equal(ValidationErrorMessage, exception.Message);
    }
}