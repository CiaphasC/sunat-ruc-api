// Métodos auxiliares para validar parámetros de entrada.
namespace SunatScraper.Domain.Validation;

/// <summary>
/// Métodos auxiliares para validar los parámetros de entrada.
/// </summary>
public static class InputValidators
{
    /// <summary>
    /// Valida que el tipo y número de documento cumplan con el formato esperado.
    /// </summary>
    public static bool IsValidDocumento(string tipdoc, string num) => tipdoc switch
    {
        "1" => num.Length == 8 && num.All(char.IsDigit),
        "4" => IsAlnum(num, 9, 12),
        "7" => IsAlnum(num, 6, 12),
        "A" => IsAlnum(num, 6, 12),
        _ => false
    };

    private static bool IsAlnum(string s, int min, int max) =>
        s.Length >= min && s.Length <= max && s.All(char.IsLetterOrDigit);

    private static readonly int[] W = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

    /// <summary>
    /// Comprueba la validez de un número de RUC.
    /// </summary>
    public static bool IsValidRuc(string r) =>
        r.Length == 11 && r.All(char.IsDigit) && Chk(r) == r[^1] - '0';

    private static int Chk(string r)
    {
        int s = 0;
        for (int i = 0; i < 10; i++)
            s += (r[i] - '0') * W[i];

        int m = 11 - (s % 11);
        return m == 10 ? 0 : m == 11 ? 1 : m;
    }

    /// <summary>
    /// Determina si el texto proporcionado cumple con las restricciones de búsqueda.
    /// </summary>
    public static bool IsValidTexto(string t) =>
        t.Length is >= 4 and <= 100 &&
        t.All(c => char.IsLetterOrDigit(c) || c is ' ' or '.' or ',' or '-');
}
