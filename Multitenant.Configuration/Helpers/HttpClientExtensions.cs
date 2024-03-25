// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Newtonsoft.Json;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace Multitenant.Configuration.Helpers;

public static class HttpClientExtensions
{
  /// <summary>
  /// Envoie une requête GET à l'URI spécifié avec une option d'achèvement HTTP sous forme d'opération asynchrone.
  /// Si la requete ne renvoie pas un StatusCode = Succes alors exe ResqponseException (ou ApiLegacyException selon les cas) est déclenchée
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="client"></param>
  /// <param name="requestUri"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async static Task<T?> GetObjectAsync<T>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
  {
    try
    {
      var response = await client.GetAsync(requestUri, cancellationToken);
      if (response == null)
        return default;

      response.EnsureSuccessStatusCode();

      using var content = response.Content;
      if (content == null)
        return default;

      var mediatype = content.Headers?.ContentType?.MediaType;
      if (mediatype == null)
        return default;

      var stringContent = await content.ReadAsStringAsync(cancellationToken);
      if (string.IsNullOrWhiteSpace(stringContent))
        return default;

      if (mediatype.Equals(MediaTypeNames.Application.Json))
        return JsonConvert.DeserializeObject<T>(stringContent);

      if (mediatype.Equals(MediaTypeNames.Text.Plain))
        return (T)(object)stringContent;
    }
    catch (OperationCanceledException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    return default;
  }

  /// <summary>
  /// Envoie une requête POST avec un jeton d'annulation sous forme d'opération asynchrone.
  /// Si la requete ne renvoie pas un StatusCode = Succes alors exe ResqponseException (ou ApiLegacyException selon les cas) est déclenchée 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="client"></param>
  /// <param name="obj"></param>
  /// <param name="requestUri"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async static Task<HttpResponseMessage?> PostObjectAsync<T>(this HttpClient client, T obj, string requestUri, CancellationToken cancellationToken = default)
  {
    try
    {
      if (obj == null)
        return default;

      var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
      request.Content = new StringContent(
                          JsonConvert.SerializeObject(obj),
                          Encoding.UTF8,
                          MediaTypeNames.Application.Json);
      var response = await client.SendAsync(request, cancellationToken);
      if (response == null)
        return default;

      response.EnsureSuccessStatusCode();
      return response;
    }
    catch (OperationCanceledException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    return default;
  }

  /// <summary>
  /// Envoie une requête POST avec un jeton d'annulation sous forme d'opération asynchrone.
  /// Si la requete ne renvoie pas un StatusCode = Succes alors exe ResqponseException (ou ApiLegacyException selon les cas) est déclenchée 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="client"></param>
  /// <param name="obj"></param>
  /// <param name="requestUri"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async static Task<TResult?> PostObjectAsync<T, TResult>(this HttpClient client, T obj, string requestUri, CancellationToken cancellationToken = default)
  {
    try
    {
      if (obj == null)
        return default;

      var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
      request.Content = new StringContent(
                          JsonConvert.SerializeObject(obj),
                          Encoding.UTF8,
                          MediaTypeNames.Application.Json);

      var response = await client.SendAsync(request, cancellationToken);
      if (response == null || response.Content == null)
        return default;

      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadAsStringAsync();
      if (string.IsNullOrWhiteSpace(json))
        return default;

      return JsonConvert.DeserializeObject<TResult>(json);
    }
    catch (OperationCanceledException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    return default;
  }

  /// <summary>
  /// Envoie une requête HTTP en tant qu'opération asynchrone.
  /// </summary>
  /// <param name="client"></param>
  /// <param name="requestUri"></param>
  /// <param name="content"></param>
  /// <returns></returns>
  public async static Task<HttpResponseMessage?> SendObjectAsync(this HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
  {
    try
    {
      var response = await client.SendAsync(request, cancellationToken);
      if (response == null)
        return default;

      response.EnsureSuccessStatusCode();
      return response;
    }
    catch (OperationCanceledException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    return default;
  }

  /// <summary>
  /// Envoie une requête DELETE avec un jeton d'annulation sous forme d'opération asynchrone.
  /// Si la requete ne renvoie pas un StatusCode = Succes alors exe ResqponseException (ou ApiLegacyException selon les cas) est déclenchée 
  /// </summary>
  /// <param name="client"></param>
  /// <param name="requestUri"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async static Task<HttpResponseMessage?> DeleteObjectAsync(this HttpClient client, string requestUri, CancellationToken cancellationToken = default(CancellationToken))
  {
    try
    {
      var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
      var response = await client.DeleteAsync(requestUri, cancellationToken);
      if (response != null && response.StatusCode != HttpStatusCode.NotFound)
        response.EnsureSuccessStatusCode();
      return response;
    }
    catch (OperationCanceledException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    return default(HttpResponseMessage);
  }

  /// <summary>
  /// récupére un Stream de façon asynchrone
  /// </summary>
  /// <param name="client"></param>
  /// <param name="requestUri"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async static Task<Stream> GetStreamAsync(this HttpClient client, string requestUri, CancellationToken cancellationToken)
  {
    var response = await client.GetAsync(requestUri, cancellationToken);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStreamAsync();
  }
}
