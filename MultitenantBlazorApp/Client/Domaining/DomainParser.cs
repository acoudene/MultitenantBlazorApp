// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using MultitenantBlazorApp.Client.Extensions;

namespace MultitenantBlazorApp.Client
{
    /// <summary>
    /// Domain parser
    /// </summary>
    public class DomainParser : IDomainParser
    {
        private DomainDataStructure? _domainDataStructure;
        private readonly TldRule _rootTldRule = new TldRule("*");

        /// <summary>
        /// Creates and initializes a DomainParser
        /// </summary>
        /// <param name="rules">The list of rules.</param>
        /// <param name="domainNormalizer">An <see cref="IDomainNormalizer"/>.</param>
        public DomainParser(IEnumerable<TldRule>? rules = null)
        {
            if (rules == null)
                rules = new List<TldRule>();
             
            AddRules(rules);
        }

        /// <summary>
        /// Creates a DomainParser based on an already initialzed tree.
        /// </summary>
        /// <param name="initializedDataStructure">An already initialized tree.</param>
        /// <param name="domainNormalizer">An <see cref="IDomainNormalizer"/>.</param>
        public DomainParser(DomainDataStructure initializedDataStructure)
        {
            _domainDataStructure = initializedDataStructure;
        }

        public DomainInfo Parse(Uri domain)
        {
            var partlyNormalizedDomain = domain.Host;
            var normalizedHost = domain.GetComponents(UriComponents.NormalizedHost, UriFormat.UriEscaped); //Normalize punycode

            var parts = normalizedHost
                .Split('.')
                .Reverse()
                .ToList();

            return GetDomainFromParts(partlyNormalizedDomain, parts);
        }

        private void AddRules(IEnumerable<TldRule> tldRules)
        {
            _domainDataStructure = _domainDataStructure ?? new DomainDataStructure("*", _rootTldRule);

            _domainDataStructure.AddRules(tldRules);
        }

        private DomainInfo GetDomainFromParts(string? domain, List<string>? parts)
        {
            if (parts == null || parts.Count == 0 || parts.Any(x => x.Equals(string.Empty)))
            {
                throw new ParseException("Invalid domain part detected");
            }

            var structure = _domainDataStructure;
            var matches = new List<TldRule>();
            FindMatches(parts, structure, matches);

            //Sort so exceptions are first, then by biggest label count (with wildcards at bottom) 
            var sortedMatches = matches.OrderByDescending(x => x.Type == TldRuleType.WildcardException ? 1 : 0)
                .ThenByDescending(x => x.LabelCount)
                .ThenByDescending(x => x.Name);

            var winningRule = sortedMatches.FirstOrDefault();

            //Domain is TLD
            if (parts.Count == winningRule?.LabelCount)
            {
                parts.Reverse();
                var tld = string.Join(".", parts);

                if (winningRule.Type == TldRuleType.Wildcard)
                {
                    if (tld.EndsWith(winningRule.Name.Substring(1)))
                    {
                        throw new ParseException("Domain is a TLD according publicsuffix", winningRule);
                    }
                }
                else
                {
                    if (tld.Equals(winningRule.Name))
                    {
                        throw new ParseException("Domain is a TLD according publicsuffix", winningRule);
                    }
                }

                throw new ParseException($"Unknown domain {domain}");
            }

            return new DomainInfo(domain, winningRule);
        }

        private void FindMatches(IEnumerable<string> parts, DomainDataStructure? structure, List<TldRule> matches)
        {
            if (structure == null)
                return;

            if (structure.TldRule != null)
            {
                matches.Add(structure.TldRule);
            }

            var part = parts.FirstOrDefault();
            if (string.IsNullOrEmpty(part))
            {
                return;
            }

            if (structure!.Nested.TryGetValue(part, out DomainDataStructure? foundStructure))
            {
                FindMatches(parts.Skip(1), foundStructure, matches);
            }

            if (structure.Nested.TryGetValue("*", out foundStructure))
            {
                FindMatches(parts.Skip(1), foundStructure, matches);
            }
        }
    }
}
