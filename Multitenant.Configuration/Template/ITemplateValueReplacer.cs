// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

namespace Multitenant.Configuration;

public interface ITemplateValueReplacer
{
    string Replace(string valueWithVar);

    void Store(string templateKey, string value);
}