using AutoFixture;
using Microservice.Appointments.Domain.Enums;
using Microservice.Appointments.Domain.Exceptions;
using Microservice.Appointments.Domain.Models;
using Microservice.Appointments.FunctionalTests.Appointment.Base;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Microservice.Appointments.FunctionalTests.Appointment;

public class AppointmentDeleteTests : AppointmentTestsBase
{
    [Fact]
    public async Task Given_Existing_Appointment_When_Delete_Called_Then_Returns_No_Content()
    {
        // Arrange
        var controller = CreateController();
        var appointmentDomain = CreateDomain();

        MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(appointmentDomain);

        MockRepository
            .Setup(repo => repo.RemoveAsync(It.IsAny<AppointmentDomain>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await controller.Delete(appointmentDomain.Id);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task Given_Non_Existent_Appointment_When_Delete_Called_Then_Throws_NotFoundException()
    {
        // Arrange
        var controller = CreateController();
        var nonExistentId = Fixture.Create<int>();

        MockRepository
            .Setup(repo => repo.GetAsync(nonExistentId))
            .ReturnsAsync((AppointmentDomain)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            controller.Delete(nonExistentId));

        Assert.Equal($"Appointment with id '{nonExistentId}' not found.", exception.Message);
    }

    [Fact]
    public async Task Given_Completed_Appointment_When_Delete_Called_Then_Throws_BadRequestException()
    {
        // Arrange
        var controller = CreateController();
        var appointmentDomain = CreateDomain(AppointmentStatus.Completed);

        MockRepository
            .Setup(repo => repo.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(appointmentDomain);

        MockRepository
            .Setup(repo => repo.RemoveAsync(It.IsAny<AppointmentDomain>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            controller.Delete(appointmentDomain.Id));

        Assert.Equal("Validation error occurred while deleting the appointment.", exception.Message);
    }
}