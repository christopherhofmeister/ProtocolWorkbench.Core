using System.Reflection;

namespace ProtocolWorkbench.Core.Enums.Helpers
{
    public static class CTypesHelper
    {
        private static readonly IReadOnlyList<string> _allCTypeNames =
            Enum.GetValues<CTypes>()
                .Select(GetName)
                .ToArray();

        public static IReadOnlyList<string> AllNames => _allCTypeNames;

        public static string GetName(CTypes ctype)
        {
            var member = typeof(CTypes).GetMember(ctype.ToString()).First();
            var attr = member.GetCustomAttribute<CTypeNameAttribute>();
            return attr?.Name ?? ctype.ToString().ToLowerInvariant();
        }

        public static bool TryParse(string? text, out CTypes ctype)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                ctype = default;
                return false;
            }

            foreach (var value in Enum.GetValues<CTypes>())
            {
                if (string.Equals(GetName(value), text, StringComparison.OrdinalIgnoreCase))
                {
                    ctype = value;
                    return true;
                }
            }

            ctype = default;
            return false;
        }
    }
}
