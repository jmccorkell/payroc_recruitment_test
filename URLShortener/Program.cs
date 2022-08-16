using System;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using LiteDB;
using System.Linq;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // HTTP routing
            services.AddRouting();

            // LiteDB
            services.AddSingleton<ILiteDatabase, LiteDatabase>(_ => new LiteDatabase("short-links.db"));
        })
        .Configure(app =>
        {
            // pipeline routing
            app.UseRouting();

            // endpoint routing mod maps URLs to functions.
            app.UseEndpoints((endpoints) =>
            {
                // root at index.html
                endpoints.MapGet("/", (ctx) =>
                {
                    return ctx.Response.SendFileAsync("index.html");
                });

                endpoints.MapPost("/shorten", HandleShortenUrl);
                endpoints.MapFallback(HandleRedirect);
            });
        });
    })
    .Build();

await host.RunAsync();

static Task HandleShortenUrl(HttpContext context)
{
    // form validation
    if (!context.Request.HasFormContentType || !context.Request.Form.ContainsKey("url"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Cannot process request");
    }

    context.Request.Form.TryGetValue("url", out var formData);
    var requestedUrl = formData.ToString();

    // Test the link
    if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri result))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Could not understand URL.");
    }

    var url = result.ToString();
    // LiteDB functionality
    var liteDB = context.RequestServices.GetService<ILiteDatabase>();
    var links = liteDB.GetCollection<ShortenURL>(BsonAutoId.Int32);

    //temp shortlink to pass
    var entry = new ShortenURL
    {
        Url = url
    };

    // Insert our short-link
    links.Insert(entry);

    var urlChunk = entry.GetUrlChunk();
    var responseUri = $"{context.Request.Scheme}://{context.Request.Host}/{urlChunk}";
    context.Response.Redirect($"/#{responseUri}");
    return Task.CompletedTask;
}

static Task HandleRedirect(HttpContext context)
{
    var db = context.RequestServices.GetService<ILiteDatabase>();
    var collection = db.GetCollection<ShortenURL>();

    var path = context.Request.Path.ToUriComponent().Trim('/');
    var id = ShortenURL.GetId(path);
    var entry = collection.Find(p => p.Id == id).FirstOrDefault();

    if (entry != null)
        context.Response.Redirect(entry.Url);
    else
        context.Response.Redirect("/");

    return Task.CompletedTask;
}