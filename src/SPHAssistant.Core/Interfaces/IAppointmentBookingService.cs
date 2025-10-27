using SPHAssistant.Core.Models.Booking;
using SPHAssistant.Core.Models.Booking.Result;

namespace SPHAssistant.Core.Interfaces;

/// <summary>
/// Defines the contract for a service that handles the appointment booking process.
/// </summary>
public interface IAppointmentBookingService
{
    /// <summary>
    /// Asynchronously attempts to book an appointment using the provided details.
    /// </summary>
    /// <param name="request">The data required for the booking attempt.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a BookingStatus indicating the outcome.</returns>
    Task<BookingStatus> BookAppointmentAsync(BookingRequest request);
}
