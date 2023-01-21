// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | MN-1198-Creation

namespace MultitenantBlazorApp.Client
{
    /// <summary>
    /// TldRule
    /// </summary>
    public class TldRule : IEquatable<TldRule>
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Type
        /// </summary>
        public TldRuleType Type { get; private set; }

        /// <summary>
        /// LabelCount
        /// </summary>
        public int LabelCount { get; private set; }

        /// <summary>
        /// TldRule
        /// </summary>
        /// <param name="ruleData"></param>
        public TldRule(string ruleData)
        {
            if (string.IsNullOrEmpty(ruleData))
            {
                throw new ArgumentException("RuleData is empty");
            }

            var parts = ruleData.Split('.').Select(x => x.Trim()).ToList();
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    throw new FormatException("Rule contains empty part");
                }

                if (part.Contains("*") && part != "*")
                {
                    throw new FormatException("Wildcard syntax not correct");
                }
            }

            if (ruleData.StartsWith("!", StringComparison.InvariantCultureIgnoreCase))
            {
                Type = TldRuleType.WildcardException;
                Name = ruleData.Substring(1).ToLower();
                LabelCount = parts.Count - 1; //Left-most label is removed for Wildcard Exceptions
                return;
            }

            if (ruleData.Contains("*"))
            {
                Type = TldRuleType.Wildcard;
                Name = ruleData.ToLower();
                LabelCount = parts.Count;
                return;
            }

            Type = TldRuleType.Normal;
            Name = ruleData.ToLower();
            LabelCount = parts.Count;
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <inheritdoc />
        public bool Equals(TldRule? other)
        {
            return Name == other?.Name;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((TldRule)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (Name == null)
            {
                return 0;
            }

            return Name.GetHashCode();
        }
    }
}
