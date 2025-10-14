namespace SPHAssistant.Core.Interfaces;

/// <summary>
/// Defines the contract for an OCR service.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Asynchronously recognizes text from a captcha image stream.
    /// </summary>
    /// <param name="captchaStream">The stream containing the captcha image.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the recognized text.</returns>
    Task<string> RecognizeCaptchaAsync(Stream captchaStream);
}
