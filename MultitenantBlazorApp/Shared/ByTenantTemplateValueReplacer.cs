// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;

namespace MultitenantBlazorApp.Shared;

public class ByTenantTemplateValueReplacer : ITemplateValueReplacer
{
  public const string TenantIdTemplateKey = "${TenantId}";
  public const string ClientIdTemplateKey = "${ClientId}";
  
  private Dictionary<string, string> _replacer = new Dictionary<string, string>();

  public string Replace(string valueWithVar)
  {
    Guard.IsNotNullOrWhiteSpace(valueWithVar);

    string replacedValue = valueWithVar;

    _replacer
      .ToList()
      .ForEach(kv => replacedValue = replacedValue.Replace(kv.Key, kv.Value));

    return replacedValue;    
  }

  public void Store(string templateKey, string value)
  {
    Guard.IsNotNullOrWhiteSpace(templateKey);

    _replacer.Add(templateKey, value);
  }

  public void StoreTenantId(string tenantId)
  {
    Guard.IsNotNullOrWhiteSpace(tenantId);

    Store(TenantIdTemplateKey, tenantId);
  }

  public void StoreClientId(string clientId)
  {
    Guard.IsNotNullOrWhiteSpace(clientId);

    Store(ClientIdTemplateKey, clientId);
  }
}