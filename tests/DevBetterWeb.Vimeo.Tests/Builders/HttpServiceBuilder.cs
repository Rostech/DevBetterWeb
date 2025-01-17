﻿using System;
using System.Net.Http;
using Ardalis.ApiClient;
using DevBetterWeb.Vimeo.Constants;
using DevBetterWeb.Vimeo.Tests.Constants;

namespace DevBetterWeb.Vimeo.Tests.Builders;

public class HttpServiceBuilder
{
  public static HttpService Build()
  {
    var httpClient = new HttpClient { BaseAddress = new Uri(ServiceConstants.VIMEO_URI) };
    httpClient.DefaultRequestHeaders.Remove("Accept");
    httpClient.DefaultRequestHeaders.Add("Accept", ServiceConstants.VIMEO_HTTP_ACCEPT);
    httpClient.Timeout = TimeSpan.FromMinutes(60);
    var httpService = new HttpService(httpClient);

    var vimeoToken = Environment.GetEnvironmentVariable(ConfigurationConstants.VIMEO_TOKEN);
    httpService.SetAuthorization(vimeoToken);

    return httpService;
  }
}

