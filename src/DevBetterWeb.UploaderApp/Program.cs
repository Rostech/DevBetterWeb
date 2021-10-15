﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.ApiCaller;
using DevBetterWeb.Vimeo.Constants;
using DevBetterWeb.Vimeo.Services.VideoServices;
using Microsoft.Extensions.DependencyInjection;

namespace DevBetterWeb.UploaderApp
{
  class Program
  {
    private static IServiceProvider _serviceProvider;
    static async Task Main(string[] args)
    {      
      var argsList = args.ToList();
      if (argsList.Count == 0 || argsList.All( x => x.ToLower() != "-d") || argsList.All(x => x.ToLower() != "-t") || argsList.All(x => x.ToLower() != "-a"))
      {
        Console.WriteLine("Please use -d [destination folder] -t [Vimeo token] -a [api link]");
        return;
      }

      var folderToUpload = GetArgument(argsList, "-d");
      if (string.IsNullOrEmpty(folderToUpload))
      {
        Console.WriteLine("Please use -d [destination folder]");
        return;
      }

      var token = GetArgument(argsList, "-t");
      if (string.IsNullOrEmpty(token))
      {
        Console.WriteLine("Please use -t [Vimeo token]");
        return;
      }

      var apiLink = GetArgument(argsList, "-a");
      if (string.IsNullOrEmpty(apiLink))
      {
        Console.WriteLine("Please use -a [api link]");
        return;
      }

      _serviceProvider = SetupDi(token, apiLink);

      var uploaderService = GetUploaderService();
      await uploaderService.SyncAsync(folderToUpload);

      Console.WriteLine("Done, press any key to close");
      Console.ReadKey();
    }       
    
    private static string GetArgument(List<string> argsList, string argValue)
    {
      var index  = argsList.FindIndex(x => x.ToLower() == argValue) + 1;
      if (index <= 0)
      {
        return null;
      }

      return argsList[index];
    }

    private static ServiceProvider SetupDi(string token, string apiLink)
    {
      var services = new ServiceCollection()
            .AddLogging()
            .AddScoped(sp => HttpClientBuilder())
            .AddScoped<HttpService>()
            .AddScoped<GetAllVideosService>()
            .AddScoped<GetAnimatedThumbnailService>()
            .AddScoped<GetStatusAnimatedThumbnailService>()
            .AddScoped<AddAnimatedThumbnailsToVideoService>()
            .AddScoped<AddDomainToVideoService>()
            .AddScoped<CompleteUploadByCompleteUriService>()
            .AddScoped<GetStreamingTicketService>()
            .AddScoped<UpdateVideoDetailsService>()
            .AddScoped<UploadVideoService>()
            .AddScoped<GetVideoService>()
            .AddScoped(sp => new UploaderService(
              token,  
              apiLink, 
              sp.GetRequiredService<HttpService>(), 
              sp.GetRequiredService<UploadVideoService>(), 
              sp.GetRequiredService<GetAllVideosService>(),
              sp.GetRequiredService<GetStatusAnimatedThumbnailService>(),
              sp.GetRequiredService<GetAnimatedThumbnailService>(),
              sp.GetRequiredService<AddAnimatedThumbnailsToVideoService>(),
              sp.GetRequiredService<GetVideoService>()));

      return services.BuildServiceProvider();
    }

    private static UploaderService GetUploaderService()
    {
      return _serviceProvider
        .GetService<UploaderService>();
    }

    private static HttpClient HttpClientBuilder()
    {
      var httpClient = new HttpClient
      {
        BaseAddress = new Uri(ServiceConstants.VIMEO_URI),
        Timeout = TimeSpan.FromHours(2)
      };
      httpClient.DefaultRequestHeaders.Add("Accept", ServiceConstants.VIMEO_HTTP_ACCEPT);

      return httpClient;
    }
  }
}
