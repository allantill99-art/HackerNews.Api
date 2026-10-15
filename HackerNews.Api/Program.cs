using HackerNews.Api.Clients;
using HackerNews.Api.Services;
using HackerNews.Api.Caching;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Register typed client with dependency injection
builder.Services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(
    client =>
    {
        client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/");

        client.Timeout = TimeSpan.FromSeconds(30);
    });

//Register the story retrieval service
builder.Services.AddScoped<IBestStoriesService, BestStoriesService>();

// register a singleton snapshot provider
builder.Services.AddSingleton<IBestStoriesSnapshotProvider, BestStoriesSnapshotProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
