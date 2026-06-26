using System.Text.RegularExpressions;
using WinterRose.Web.Utils;
using WinterRose.Web.Validation.Issues;
using WinterRose.Web.Validation.Rules;

namespace WinterRose.Web.Validation;

public static class PasswordRule
{
    private static List<string> commonPasswords = new(10_000);

    static readonly Regex PASSWORD_REGEX =
        new Regex(
            @"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s]).{8,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );
  

    static PasswordRule()
    {
        string[] names = typeof(PasswordRule).Assembly .GetManifestResourceNames();
        using Stream? resourceStream = typeof(PasswordRule).Assembly.GetManifestResourceStream(names[0]);
        
        resourceStream?.UseStreamReader(sr =>
        {
            string? line;
            while ((line = sr.ReadLine()) is not null)
                commonPasswords.Add(line);
        });
    }

    extension<T>(IRuleBuilder<T, string> builder)
    {
        public IRuleBuilder<T, string> NotCommonPassword()
        {
            return builder
                .Must(
                    async password =>
                    {
                        bool res = commonPasswords.Contains(password);
                        return !res;
                    },
                    field => ValidationIssues.CommonPassword(field),
                    "CommonPasswordsNotAllowed");
        }

        public IRuleBuilder<T, string> MustBeComplexPassword()
        {
            return builder.Must(
                async value => value != null && PASSWORD_REGEX.IsMatch(value),
                field => ValidationIssues.IsNotComplexPassword(field, ValidationSeverity.Error),
                "MustBeComplexPassword"
            );
               
        }
    }
}
