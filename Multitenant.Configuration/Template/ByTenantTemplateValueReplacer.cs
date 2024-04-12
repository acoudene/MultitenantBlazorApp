// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

namespace Multitenant.Configuration;

public class ByTenantTemplateValueReplacer : ITemplateValueReplacer
{
  public const string TenantIdTemplateKey = "${TenantId}";
  public const string ClientIdTemplateKey = "${ClientId}";

  private Dictionary<string, string> _replacer = new Dictionary<string, string>();

  public string Replace(string valueWithVar)
  {
    if (string.IsNullOrWhiteSpace(valueWithVar))
      throw new ArgumentNullException(nameof(valueWithVar));

    string replacedValue = valueWithVar;

    _replacer
      .ToList()
      .ForEach(kv => replacedValue = replacedValue.Replace(kv.Key, kv.Value));

    return replacedValue;
  }

  public void Store(string templateKey, string value)
  {
    if (string.IsNullOrWhiteSpace(templateKey))
      throw new ArgumentNullException(nameof(templateKey));

    _replacer.Add(templateKey, value);
  }

  public void StoreTenantId(string tenantId)
  {
    if (string.IsNullOrWhiteSpace(tenantId))
      throw new ArgumentNullException(nameof(tenantId));
    
    Store(TenantIdTemplateKey, tenantId);
  }

  public void StoreClientId(string clientId)
  {
    if (string.IsNullOrWhiteSpace(clientId))
      throw new ArgumentNullException(nameof(clientId));

    Store(ClientIdTemplateKey, clientId);
  }
}