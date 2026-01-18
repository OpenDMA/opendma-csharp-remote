using System;
using System.Collections.Generic;
using System.Text;
using OpenDMA.Api;

namespace OpenDMA.Remote.Utils
{
    /// <summary>
    /// Builds the include parameter for REST API requests
    /// </summary>
    public static class IncludeParameterBuilder
    {
        /// <summary>
        /// Builds an include parameter string from property names
        /// </summary>
        /// <param name="propertyNames">Array of property names to include</param>
        /// <param name="includeDefaults">Whether to include server-selected default properties</param>
        /// <returns>Include parameter string</returns>
        public static string Build(OdmaQName[]? propertyNames, bool includeDefaults = true)
        {
            if (propertyNames == null || propertyNames.Length == 0)
            {
                return "*:*";
            }

            var parts = new List<string>();
            
            foreach (var name in propertyNames)
            {
                parts.Add(Escape(name.ToString()));
            }

            if (includeDefaults)
            {
                parts.Add("default");
            }

            return string.Join(";", parts);
        }

        /// <summary>
        /// Builds an include parameter for fetching a specific page of a multi-value reference property
        /// </summary>
        /// <param name="propertyName">The property name to fetch</param>
        /// <param name="nextToken">The pagination token from the previous page</param>
        /// <returns>Include parameter string for pagination</returns>
        public static string BuildWithNextToken(OdmaQName propertyName, string nextToken)
        {
            // Format: nextToken@propertyName
            // Both components need to be escaped
            var escapedToken = Escape(nextToken);
            var escapedProperty = Escape(propertyName.ToString());
            return $"{escapedToken}@{escapedProperty}";
        }

        /// <summary>
        /// Escapes special characters in property names
        /// </summary>
        private static string Escape(string value)
        {
            var sb = new StringBuilder();
            foreach (var c in value)
            {
                if (c == '\\' || c == ';' || c == '@')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
