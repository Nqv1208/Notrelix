namespace Notrelix.Infrastructure.Auth.Csrf;

/// <summary>
/// Resolves whether a request is subject to browser CSRF validation per ADR-005.
/// Classification is evidence-based: only requests relying on ambient browser
/// credentials (no explicit Authorization credential) are within the browser
/// CSRF threat model. Route strings are never an input.
/// </summary>
public interface ICsrfApplicabilityClassifier
{
    bool IsBrowserCsrfApplicable(HttpRequest request);
}

/// <inheritdoc />
public sealed class CsrfApplicabilityClassifier : ICsrfApplicabilityClassifier
{
    public bool IsBrowserCsrfApplicable(HttpRequest request)
    {
        if (!IsUnsafeMethod(request.Method))
        {
            return false;
        }

        // An explicitly presented Authorization credential (canonical API-token
        // bearer secrets or any other explicit bearer credential used by
        // native/non-browser clients) cannot be attached by a cross-site
        // attacker through the victim's browser, so such requests are outside
        // the ambient-browser CSRF threat model.
        return !request.Headers.ContainsKey("Authorization");
    }

    private static bool IsUnsafeMethod(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
}
