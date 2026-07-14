namespace ERP.Core.Manager.Api.Application.Commons.Utils
{
    public static class StringExtensions
    {
        public static string ToCapitalize(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return value.Trim().ToLower() switch
            {
                "" => string.Empty,
                string s => char.ToUpper(s[0]) + s[1..]
            };
        }

        public static string ToNumberToLetters(decimal amount)
        {
            long entero = (long)Math.Floor(amount);
            int decimales = (int)((amount - entero) * 100);

            return $"{ConvertirEntero(entero)} con {decimales:00}/100";
        }
        
        public static string? FormatWithNullWhenNoHasValue(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // Limpiamos espacios, pestañas Y también las comillas dobles literales
            string cleaned = text.Trim(' ', '\t', '"');

            // Volvemos a validar por si al quitar las comillas el string quedó vacío (ej: "\"   \"")
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        public static string ConvertirEntero(long numero)
        {
            if (numero == 0) return "cero";
            if (numero == 18) return "dieciocho";
            if (numero == 1000) return "mil";

            if (numero < 20)
            {
                string[] unidades = {
                    "", "uno", "dos", "tres", "cuatro", "cinco",
                    "seis", "siete", "ocho", "nueve", "diez",
                    "once", "doce", "trece", "catorce", "quince",
                    "dieciséis", "diecisiete", "dieciocho", "diecinueve"
                };
                return unidades[numero];
            }

            if (numero < 100)
            {
                string[] decenas = {
                    "", "", "veinte", "treinta", "cuarenta",
                    "cincuenta", "sesenta", "setenta", "ochenta", "noventa"
                };

                int d = (int)(numero / 10);
                int u = (int)(numero % 10);

                return u == 0 ? decenas[d] : $"{decenas[d]} y {ConvertirEntero(u)}";
            }

            if (numero < 1000)
            {
                string[] centenas = {
                    "", "ciento", "doscientos", "trescientos", "cuatrocientos",
                    "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"
                };

                if (numero == 100) return "cien";

                int c = (int)(numero / 100);
                int resto = (int)(numero % 100);

                return resto == 0 ? centenas[c] : $"{centenas[c]} {ConvertirEntero(resto)}";
            }

            if (numero < 1000000)
            {
                int miles = (int)(numero / 1000);
                int resto = (int)(numero % 1000);

                string milesTexto = miles == 1 ? "mil" : $"{ConvertirEntero(miles)} mil";

                return resto == 0 ? milesTexto : $"{milesTexto} {ConvertirEntero(resto)}";
            }

            return numero.ToString(); // fallback
        }
    }
}
