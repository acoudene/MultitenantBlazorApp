# MultitenantBlazorApp

Just another example of OIDC integration Blazor WebAssembly but with a dynamic configuration depending on given tenant

_(in french)_

# Objectif

Le paramétrage OIDC utilisé côté client et server utilise la logique suivante :
- D'abord, il essaie de retrouver le paramétrage écrit en dur dans la configuration selon le tenant (mécanisme standard ordonné, chez nous, nous utilisons pour l'instant le fichier appsettings.json)
- Ensuite, s'il ne le trouve pas, il va essayer de retrouver le paramétrage de type modèle/template.

L'intérêt : on peut à la fois forcer une configuration en dur (pour le développement par exemple) sans être obligé d'en saisir une (de configuration) pour tous les nouveaux tenants générés par l'industrialisation.

# Instanciation du mécanisme

## Côté Serveur

Par IoC, il faut ajouter l'implémentation du fournisseur **IJwtBearerOptionsProvider** du paramétrage comme ici :

```
builder.Services.AddTransient<IJwtBearerOptionsProvider, ByTenantJwtBearerOptionsProvider>();
```

## Côté Client

Par IoC, il faut ajouter l'implémentation du fournisseur **IOidcProviderOptionsProvider** du paramétrage comme ici :

```
builder.Services.AddScoped<IOidcProviderOptionsProvider, ByTenantOidcProviderOptionsProvider>();
```

# Explication du mécanisme

## Principe général

Que ce soit du côté serveur ou client, le mécanisme est similaire.

L'implémentation du fournisseur va lire la configuration selon une clé correspondant au **tenant id** courant, s'il ne la trouve pas alors il va tenter de retrouver le **tenant id** modèle.

Dans la configuration OIDC du serveur ou du client, le **tenant id** modèle correspond à ${Template}.

```
string tenantConfigKey = GetTenantConfigKey(tenantId);
var tenantConfigSection = _configuration.GetSection(tenantConfigKey);
if (tenantConfigSection == null || ! tenantConfigSection.Exists())
{
  tenantConfigSection = _configuration.GetSection(GetTenantConfigKey(TemplateConfigKey));
  if (tenantConfigSection == null)
    throw new InvalidOperationException("Missing template config for all tenants");
}
```

Voici les modèles de section de fichiers appsettings.json pour le serveur et le client.

### Version serveur

```
"Oidc": {
  "${Template}": {
    "Authority": "https://tdkeycloak.azurewebsites.net/auth/realms/${TenantId}",
    "ClientId": "multitenantBlazorapp",
    "Audience": "account",
    "TargetUserRolesClaimName": "user_roles",
    "NameClaimType": "preferred_username",
    "RoleClaimTemplate": "resource_access.${ClientId}.roles",
    "CacheDelayInSec": 120
  }
```

_Remarque : la valeur supplémentaire nommée CacheDelayInSec sert de durée en secondes au cache de récupération des métadonnées OIDC depuis le fournisseur d'identité, elle peut être omise car sa valeur par défaut est de 120 secondes._

### Version client

```
"Oidc": {
  "${Template}": {
    "Authority": "https://tdkeycloak.azurewebsites.net/auth/realms/${TenantId}",
    "ClientId": "multitenantBlazorapp",
    "ResponseType": "code",
    "RoleClaimTemplate": "resource_access.${ClientId}.roles"
  }
```

A savoir, la transformation des valeurs de configuration variabilisées vers leur valeur concrète est effectuée par l'implémentation **ByTenantTemplateValueReplacer** de l'interface **ITemplateValueReplacer**

```
public interface ITemplateValueReplacer
{
    string Replace(string valueWithVar);

    void Store(string templateKey, string value);
}
```

A ce jour, le choix de séparer les reponsabilités a bien été effectué mais j'ai jugé inutile d'utiliser de l'IoC/DI pour découpler l'implémentation de son utilisation et afin d'éviter de rajouter de la complexité d'instanciation.

## Mécanisme spécifique côté client et serveur

### Côté Serveur

L'utilisation du provider **IJwtBearerOptionsProvider** est faite par le Handler de gestion des JwtBearer. En mode multitenant, nous utilisons notre **ByTenantJwtBearerHandler**. On va retrouver d'abord la récupération des options ici :

```
protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    string? token;
    try
    {
      // Add specific options from tenant
      _jwtBearerOptionsProvider.ConfigureOptions(Options);

[...]
```

Mais aussi ici pour récupérer et appliquer les métadonnées OIDC si le **tenant id** a changé ou si la date d'expiration du cache a changé :

```
protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
{
[...]

  var currentConfiguration = await _jwtBearerOptionsProvider.GetOpenIdConfigurationAsync(Options, Context.RequestAborted);
  var validationParameters = Options.TokenValidationParameters.Clone();
  if (currentConfiguration != null)
  {
    var issuers = new[] { currentConfiguration.Issuer };
    validationParameters.ValidIssuers = validationParameters.ValidIssuers?.Concat(issuers) ?? issuers;

    validationParameters.IssuerSigningKeys = validationParameters.IssuerSigningKeys?.Concat(currentConfiguration.SigningKeys) ??currentConfiguration.SigningKeys;
  }

[...]
```

Avec ce code de récupération de la métadonnée OIDC et de mise en cache (GetOpenIdConfigurationAsync) :

```
var ret = await _memoryCache.GetOrCreateAsync(cacheKey, async cacheEntry =>
{
  cacheEntry.AbsoluteExpirationRelativeToNow = _cacheDelayInSec;
  string metadataAddress = options.MetadataAddress;
  if (metadataAddress == null)
    throw new InvalidOperationException($"Missing metadata address for {authority}");

  // For debug purpose only:        
  //var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever() { RequireHttps = false });
  var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress, new OpenIdConnectConfigurationRetriever());
  var authorityConfiguration = await configurationManager.GetConfigurationAsync(cancellationToken);
  return authorityConfiguration;
});
```

### Côté Client

L'utilisation du provider **IOidcProviderOptionsProvider** va se retrouver dans la déclaration du paramétrage de l'IAM côté client :

```
builder.Services
    .AddOidcAuthentication(options =>
    {
      if (host == null) throw new InvalidOperationException("Missing host");

      var oidcProviderOptionsProvider = host.Services.GetRequiredService<IOidcProviderOptionsProvider>();
      if (oidcProviderOptionsProvider == null)
        throw new InvalidOperationException($"Missing {nameof(IOidcProviderOptionsProvider)} implementation");

      oidcProviderOptionsProvider.ConfigureOptions(options.ProviderOptions, options.UserOptions);       
    })
    .AddAccountClaimsPrincipalFactory<MyClaimsPrincipalFactory>();
```

