using Microsoft.Extensions.Logging;
using OpenCvSharp;
using SPHAssistant.Core.Interfaces;
using SixLabors.ImageSharp.Formats.Png;
using System.Text.RegularExpressions;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Exceptions;

namespace SPHAssistant.Core.Services;

/// <summary>
/// Implements the OCR service for recognizing captcha images.
/// </summary>
public class OcrService : IOcrService
{
    private readonly ILogger<OcrService> _logger;
    private const string TessDataPath = "./tessdata";
    private const string Language = "eng";
    private const string CharWhitelist = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public OcrService(ILogger<OcrService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Asynchronously recognizes text from a captcha image stream.
    /// </summary>
    public async Task<string> RecognizeCaptchaAsync(Stream captchaStream)
    {
        try
        {
            // Step 1: Decode the initial image stream using the reliable ImageSharp library.
            using var imageSharp = await SixLabors.ImageSharp.Image.LoadAsync(captchaStream);

            // Step 2: Convert the decoded image to a PNG byte array in memory.
            using var pngStream = new MemoryStream();
            await imageSharp.SaveAsync(pngStream, new PngEncoder());
            var pngBytes = pngStream.ToArray();

            // Step 3: Load the PNG data into an OpenCV Mat object for processing.
            using var src = Cv2.ImDecode(pngBytes, ImreadModes.Grayscale);
            if (src.Empty())
            {
                _logger.LogWarning("Failed to decode PNG data with OpenCV. Mat is empty.");
                return string.Empty;
            }

            // --- OpenCV Processing Pipeline ---
            using var inverted = new Mat();
            Cv2.BitwiseNot(src, inverted);
            
            using var binary = new Mat();
            Cv2.Threshold(inverted, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            using var cleaned = new Mat();
            Cv2.MorphologyEx(binary, cleaned, MorphTypes.Open, kernel);
            
            using var finalImage = new Mat();
            Cv2.BitwiseNot(cleaned, finalImage);
            // --- End of OpenCV Pipeline ---

            // Save the processed image for debugging
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = $"captcha_{timestamp}_processed.png";
            var backupDir = Path.Combine(AppContext.BaseDirectory, "captchas");
            Directory.CreateDirectory(backupDir);
            var filePath = Path.Combine(backupDir, fileName);
            finalImage.SaveImage(filePath);

            // Convert the final Mat to a byte array for Tesseract
            Cv2.ImEncode(".png", finalImage, out byte[] processedImageBytes);

            // Initialize and use Tesseract Engine
            using var engine = new Engine(TessDataPath, Language, EngineMode.LstmOnly);
            engine.SetVariable("tessedit_char_whitelist", CharWhitelist);

            using var pixImage = TesseractOCR.Pix.Image.LoadFromMemory(processedImageBytes);
            using var page = engine.Process(pixImage, PageSegMode.SingleLine);

            var result = Regex.Replace(page.Text, @"\s+", "").Trim();

            return result;
        }
        catch (TesseractException ex)
        {
            _logger.LogError(ex, "Tesseract OCR recognition failed.");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during captcha processing.");
            return string.Empty;
        }
    }
}
