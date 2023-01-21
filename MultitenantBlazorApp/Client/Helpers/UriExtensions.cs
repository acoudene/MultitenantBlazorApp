// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace MultitenantBlazorApp.Client.Helpers
{
    public static class UriExtensions
    {
        public static string? GetSubdomain(this Uri uri, params string[] tldRuleDatas)
        {
            IEnumerable<TldRule>? tldRules = default;
            if (tldRuleDatas != null)
                tldRules = tldRuleDatas.Select(r => new TldRule(r));

            var domainParser = new DomainParser(tldRules);
            var domainInfo = domainParser.Parse(uri);
            if (domainInfo == null)
                return null;

            return domainInfo.SubDomain;
        }

        public static Uri AddOrReplaceQueryString(this Uri uri, string key, string value)
        {
            Guard.IsTrue(uri.IsAbsoluteUri);

            Uri cleanedUri = uri.RemoveQueryString(key);
            if (cleanedUri == null)
                throw new InvalidOperationException("An uri is expected");

            var newUri = cleanedUri.IsAbsoluteUri ? cleanedUri.AbsoluteUri : cleanedUri.ToString();

            var newUriWithQueryString = QueryHelpers.AddQueryString(newUri, key, value);
            return new Uri(newUriWithQueryString);
        }

        public static Uri RemoveQueryString(this Uri uri, string key)
        {
            Guard.IsTrue(uri.IsAbsoluteUri);

            var query = uri.IsAbsoluteUri ? uri.Query : uri.ToString();

            // this gets all the query string key value pairs as a collection
            var newQueryStringKv = QueryHelpers
                .ParseQuery(query);

            if (newQueryStringKv == null) throw new InvalidOperationException("Problem while getting query string");

            // this removes the key if exists
            newQueryStringKv.Remove(key);

            var newQueryString = newQueryStringKv
                .SelectMany(kv => kv.Value, (kv, c) => KeyValuePair.Create(kv.Key, c))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // this gets the page path from root without QueryString
            var pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);
            var newAbsoluteUriWithQueryString = QueryHelpers.AddQueryString(pagePathWithoutQueryString, newQueryString);

            return new Uri(newAbsoluteUriWithQueryString);
        }


        public static bool TryGetQueryString<T>(this Uri uri, string key, out T? value)
        {
            var query = uri.IsAbsoluteUri ? uri.Query : uri.ToString();

            if (string.IsNullOrWhiteSpace(query))
            {
                value = default;
                return false;
            }
            return TryGetQueryStringInternal(query, key, out value);
        }

        private static bool TryGetQueryStringInternal<T>(string queryString, string key, out T? value)
        {
            Guard.IsNotNullOrWhiteSpace(key);

            if (QueryHelpers.ParseQuery(queryString).TryGetValue(key, out var valueFromQueryString))
            {
                if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
                {
                    value = (T)(object)valueAsInt;
                    return true;
                }

                if (typeof(T) == typeof(string))
                {
                    value = (T)(object)valueFromQueryString.ToString();
                    return true;
                }

                if (typeof(T) == typeof(decimal) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
                {
                    value = (T)(object)valueAsDecimal;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
