namespace SPHAssistant.Core.Models.Internal;

/// <summary>
/// Represents the state of an ASP.NET Web Forms page, including hidden fields required for postbacks.
/// </summary>
/// <param name="ViewState">The __VIEWSTATE value.</param>
/// <param name="ViewStateGenerator">The __VIEWSTATEGENERATOR value.</param>
/// <param name="EventValidation">The __EVENTVALIDATION value.</param>
public record WebFormsState(string ViewState, string ViewStateGenerator, string EventValidation);
