using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SPHAssistant.Core.Interfaces;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Exceptions;

namespace SPHAssistant.Core.Services;

/// <summary>
/// Implements the OCR service for recognizing captcha images.
/// </summary>
public class OcrService : IOcrService
{
    // The Tesseract engine requires language data files (.traineddata).
    private const string TessDataPath = "./tessdata";
    private const string Language = "eng";
    // Whitelist for characters to recognize, as per requirements.
    private const string CharWhitelist = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Asynchronously recognizes text from a captcha image stream.
    /// </summary>
    /// <param name="captchaStream">The stream containing the captcha image.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains the recognized text, or an empty string if recognition fails.
    /// </returns>
    public async Task<string> RecognizeCaptchaAsync(Stream captchaStream)
    {
        try
        {
            // Initialize the Tesseract engine with LSTM mode for potentially better accuracy.
            using var engine = new Engine(TessDataPath, Language, EngineMode.LstmOnly);
            engine.SetVariable("tessedit_char_whitelist", CharWhitelist);

            // Pre-process the image using SixLabors.ImageSharp
            using var image = await Image.LoadAsync<Rgba32>(captchaStream);

            // Image pre-processing pipeline for optimization.
            image.Mutate(x =>
                x.Resize(image.Width * 2, image.Height * 2)
                    .Grayscale()
                    .GaussianBlur(0.7f)
                    .BinaryThreshold(0.55f)
            );

            // Convert the processed image into a format readable by Tesseract
            using var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms);
            ms.Position = 0;

            // Save a copy of the processed image for debugging purposes
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = $"captcha_{timestamp}_processed.png";
            var backupDir = Path.Combine(AppContext.BaseDirectory, "captchas");
            Directory.CreateDirectory(backupDir);
            var filePath = Path.Combine(backupDir, fileName);
            await image.SaveAsPngAsync(filePath);

            // Load the image into Tesseract's internal format
            using var pixImage = TesseractOCR.Pix.Image.LoadFromMemory(ms);
            // Process the image using SingleLine page segmentation mode.
            using var page = engine.Process(pixImage, PageSegMode.SingleLine);

            // Post-processing: Clean the result.
            var result = page.Text.Trim();

            return result;
        }
        catch (TesseractException ex)
        {
            Console.WriteLine($"Tesseract OCR recognition failed: {ex.Message}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred during captcha processing: {ex.Message}");
            return string.Empty;
        }
    }
}
