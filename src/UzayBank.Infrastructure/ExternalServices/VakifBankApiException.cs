namespace UzayBank.Infrastructure.ExternalServices;

/// <summary>
/// VakıfBank API'sinden başarısız bir cevap geldiğinde fırlatılır.
/// HttpRequestException'dan farklı olarak ham cevap gövdesini taşır —
/// böylece hata kodu (ACBH000202, ACBG000001 vb.) okunabilir.
/// </summary>
public class VakifBankApiException : Exception
{
    public string RawResponse { get; }
    public System.Net.HttpStatusCode StatusCode { get; }

    public VakifBankApiException(System.Net.HttpStatusCode statusCode, string rawResponse)
        : base($"VakıfBank API hatası ({statusCode}): {rawResponse}")
    {
        StatusCode = statusCode;
        RawResponse = rawResponse;
    }

    /// <summary>VakıfBank hata kodu cevap gövdesinde geçiyor mu?</summary>
    public bool HasErrorCode(string code) =>
        RawResponse.Contains(code, StringComparison.OrdinalIgnoreCase);
}