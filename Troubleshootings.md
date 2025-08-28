# 🧭 Approche de troubleshooting complète

## 🔎 Étape 1 : Vérifier la configuration de la fédération Entra ID

- Assure-toi que Keycloak est bien configuré comme client OIDC ou SP SAML selon le mode choisi.
- Pour OIDC :
  - Vérifie l’URL de découverte (/.well-known/openid-configuration)
  - Vérifie que les JWKs sont bien accessibles pour la validation de signature 2
- Pour SAML :
  - Vérifie les attributs mappés (email, name, etc.) 3
  - Vérifie que les certificats sont valides et à jour

## 🧪 Étape 2 : Tester l’authentification manuellement

- Utilise un outil comme curl ou Postman pour simuler une authentification
- Observe les logs Keycloak (standalone/log/server.log) pour :
- Erreurs de signature
- Erreurs de mappage d’attributs
- Problèmes de session

## 🧰 Étape 3 : Activer le mode debug dans Keycloak

- Dans standalone.xml ou via CLI
- Et dans l’admin console
- Va dans Events > Config
- Active les logs pour LOGIN, LOGIN_ERROR, FEDERATED_IDENTITY_LINK

## 🧱 Étape 4 : Vérifier les erreurs courantes

- Erreur de signature de token : souvent liée à une mauvaise récupération des clés publiques Entra ID 2
- Utilisateur non mappé : si l’attribut email ou username n’est pas transmis ou mal mappé
- Redirection incorrecte : mauvaise redirect_uri ou reply URL dans Entra ID

# ✅ Checklist Technique : Diagnostic OIDC dans une architecture Blazor + Keycloak + Azure EntraID

## 🧭 Contexte
Architecture :
- Frontend : Blazor WebAssembly
- Backend : ASP.NET Core
- Authentification : Keycloak (OIDC)
- Fédération : Azure EntraID (OIDC Identity Provider)

---

## 🔐 1. Vérifications côté Azure EntraID

- [ ] Vérifier que l'application est bien enregistrée dans Azure EntraID (App Registration)
- [ ] Vérifier que les **optional claims** nécessaires sont ajoutés (email, upn, groups, etc.)
- [ ] Vérifier que les **groupes** sont bien inclus dans le token (attention : EntraID fournit les IDs, pas les noms)
- [ ] Vérifier que les **extension attributes** sont exposés via Microsoft Graph si utilisés
- [ ] Vérifier que l'authentification OIDC est activée et que les scopes sont corrects

---

## 🧩 2. Vérifications côté Keycloak (Identity Provider)

- [ ] Vérifier la configuration du fournisseur OIDC Azure dans Keycloak :
  - URL de découverte `.well-known/openid-configuration`
  - client_id et client_secret
  - Activation de "Access Token is JWT"
- [ ] Vérifier que les utilisateurs EntraID sont bien créés ou liés dans Keycloak
- [ ] Vérifier les logs Keycloak en niveau DEBUG pour les événements d’authentification

---

## 🧬 3. Vérifications des mappers Keycloak

- [ ] Vérifier que les **Protocol Mappers** sont bien configurés dans le client Keycloak utilisé par Blazor
- [ ] Vérifier que les mappers incluent les claims nécessaires :
  - email
  - upn
  - groups
  - roles
- [ ] Vérifier que les mappers sont inclus dans les **client scopes**
- [ ] Vérifier que les claims sont bien présents dans le `access_token`, `id_token`, et `userinfo`

---

## 🧪 4. Vérifications des tokens

- [ ] Décoder le token avec [jwt.io](https://jwt.io) ou un outil local
- [ ] Vérifier la présence des claims attendus
- [ ] Vérifier le champ `aud` (audience) correspond au client Blazor
- [ ] Vérifier la validité du token (exp, iss, iat)

---

## 🧰 5. Outils de supervision et debug

- [ ] Keycloak Admin Console (logs, mappers, utilisateurs)
- [ ] Microsoft Graph Explorer (vérification des attributs Azure)
- [ ] Postman (test des endpoints OIDC)
- [ ] ASP.NET Core logs (claims reçus côté serveur)
- [ ] JWT.io (décodage des tokens)
- [ ] Grafana / Prometheus (si métriques Keycloak activées)

---

## 🛡️ 6. Bonnes pratiques

- [ ] Documenter les claims attendus et les rôles avec le client final
- [ ] Utiliser des **group IDs** pour le contrôle d’accès
- [ ] Activer les logs d’audit dans Keycloak
- [ ] Automatiser les tests d’authentification pour chaque nouveau tenant

---

# ✅ Claims à valider côté API :
- aud : Doit correspondre à l’ID de votre API (client ID ou URI).
- iss : Doit être l’URL du tenant Azure Entra ID.
- exp / nbf / iat : Dates d’expiration et de validité.
- scp / roles : Scopes ou rôles autorisés.
- tid / oid / sub : Identifiants du tenant et de l’utilisateur.
- azp / appid : Identifiant de l’application cliente.