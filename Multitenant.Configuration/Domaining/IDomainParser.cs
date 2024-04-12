// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

namespace Multitenant.Configuration.Domaining
{
    /// <summary>
    /// Domain parser
    /// </summary>
    public interface IDomainParser
    {
        /// <summary>
        /// Parse the DomainInfo from <paramref name="domain"/>.
        /// </summary>
        /// <param name="domain">The domain to parse.</param>
        /// <returns>DomainInfo object</returns>
        DomainInfo Parse(Uri domain);
    }
}